using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Dabp.Infrastructure;
using Dabp.Infrastructure.OrmSetting;
using Dabp.Infrastructure.Repositories;
using Dabp.Services.Caching;
using Dabp.Services.Export;
using Dabp.Services.Settings;
using Dabp.Utils.Exceptions;
using Dabp.Utils.Security;
using Dabp.WpfWindow.Layout;
using Dabp.WpfWindow.Services;
using Dabp.WpfWindow.ViewModels;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SqlSugar;
using AppCacheService = Vk.Dbp.Contracts.Caching.ICacheService;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Contracts.Constants;
using Vk.Dbp.WpfWindow.ViewModels;

namespace Dabp.WpfWindow
{
    internal class Bootstrapper : PrismBootstrapper
    {
        private DependencyObject? _shell;
        private Window? _splashScreen;
        private ShutdownMode _originalShutdownMode = ShutdownMode.OnLastWindowClose;

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell(DependencyObject shell)
        {
            // Prism 不会 await InitializeShell：若在此直接初始化数据库，InitializeModules
            // （注册 LoginView 等导航目标）会与它并发执行，首次导航存在竞态。
            // 因此这里只做同步准备，数据库初始化与首次导航推迟到 OnInitialized（模块就绪后）。
            _shell = shell;

            if (Application.Current != null)
            {
                _originalShutdownMode = Application.Current.ShutdownMode;
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            _splashScreen = CreateSplashScreen();
            _splashScreen.Show();

            base.InitializeShell(shell);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // OnInitialized 在 InitializeModules 之后同步调用，导航目标已注册，
            // 可以安全地初始化数据库并执行首次导航。
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            _ = CompleteStartupAsync(dispatcher);
        }

        private async Task CompleteStartupAsync(Dispatcher dispatcher)
        {
            Window? splashScreen = _splashScreen;

            try
            {
                var startupService = Container.Resolve<IAppStartupService>();
                await startupService.InitializeDatabaseAsync();

                // 本方法的 await 已被 ConfigureAwait.Fody 织入，续体可能在线程池线程；
                // Shell 显示、关闭模式与首次导航都是 UI 线程亲和操作，显式调度回 UI 线程。
                dispatcher.Invoke(() =>
                {
                    ShowShellWindow(_shell ?? throw new InvalidOperationException("Shell was not created during startup."));

                    if (Application.Current != null)
                    {
                        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    }

                    var navigationService = Container.Resolve<INavigationService>();
                    var userSession = Container.Resolve<IUserSession>();

                    string initialView = userSession.IsLoggedIn
                        ? ViewNames.Dashboard
                        : ViewNames.LoginView;

                    navigationService.NavigateTo(initialView);
                });

                _ = startupService.StartSessionTimeoutMonitoringAsync();
            }
            catch (InvalidOperationException ex)
            {
                HandleStartupFailure(dispatcher, ex);
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                HandleStartupFailure(dispatcher, ex);
            }
            finally
            {
                dispatcher.Invoke(() =>
                {
                    splashScreen?.Close();

                    if (Application.Current?.MainWindow == null && Application.Current != null)
                    {
                        Application.Current.ShutdownMode = _originalShutdownMode;
                    }
                });
            }
        }

        private static void HandleStartupFailure(Dispatcher dispatcher, Exception ex)
        {
            Log.Error(ex, "Application startup failed");

            dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"Application startup failed: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current?.Shutdown();
            });
        }

        private static void ShowShellWindow(DependencyObject shell)
        {
            if (shell is not Window mainWindow)
            {
                return;
            }

            Application.Current.MainWindow = mainWindow;

            if (!mainWindow.IsVisible)
            {
                mainWindow.Show();
            }

            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            mainWindow.Activate();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            IConfigurationRoot configuration = BuildConfiguration();

            ConfigureLogging();
            ConfigurationValidator.Validate(configuration);
            ConfigureSqlSugarDb(containerRegistry, configuration);

            containerRegistry.RegisterSingleton<IAppSettingsService, AppSettingsService>();
            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
            containerRegistry.RegisterSingleton<IPasswordHasher, PasswordHasher>();
            containerRegistry.RegisterSingleton<IDatabaseInitializer, DatabaseInitializer>();
            containerRegistry.Register(typeof(IRepository<>), typeof(SqlSugarRepository<>));
            containerRegistry.RegisterSingleton<IMenuPermissionFilter, MenuPermissionFilter>();
            containerRegistry.RegisterSingleton<IUserSession, UserSession>();
            containerRegistry.RegisterSingleton<IExportService, ExportService>();
            containerRegistry.RegisterSingleton<IUiDialogService, UiDialogService>();
            containerRegistry.RegisterSingleton<IAppStartupService, AppStartupService>();
            containerRegistry.RegisterSingleton<ILockScreenService, LockScreenService>();
            containerRegistry.RegisterSingleton<ISessionTimeoutService, SessionTimeoutService>();
            RegisterCacheService(containerRegistry, configuration);
            containerRegistry.RegisterSingleton<IViewModelFactory, ViewModelFactory>();
            containerRegistry.RegisterSingleton<INavigationService, PrismNavigationService>();
            containerRegistry.RegisterSingleton<LockScreenViewModel>();
            containerRegistry.RegisterSingleton<AppAlarmViewModel>();
            // 通知中心 VM 必须单例：HeaderViewModel 每次铃铛点击 new 一个 View，
            // 瞬态 VM 以 keepSubscriberReferenceAlive:true 订阅全局事件且 Dispose 无人调用，会持续泄漏。
            containerRegistry.RegisterSingleton<AppNotificationViewModel>();
        }

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();
            ViewModelLocationProvider.Register<HeaderView, HeaderViewModel>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<Vk.Dbp.WorkshopModule.DbpWorkshopModule>();
            moduleCatalog.AddModule<Vk.Dbp.AccountModule.DbpAccountModule>();
        }

        private IConfigurationRoot BuildConfiguration()
        {
            return AppConfigurationBuilder.Build();
        }

        private void ConfigureLogging()
        {
            string logDirectory = Path.Join(GetLocalAppDataDirectory(), "Logs");
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Join(logDirectory, "logs.txt"),
                    outputTemplate: "[{Timestamp:MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    encoding: Encoding.UTF8,
                    retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 100 * 1024)
                .CreateLogger();
        }

        private static void RegisterCacheService(IContainerRegistry containerRegistry, IConfiguration configuration)
        {
            RedisCacheOptions redisOptions = configuration.GetSection("Redis").Get<RedisCacheOptions>() ?? new RedisCacheOptions();

            if (string.IsNullOrWhiteSpace(redisOptions.InstanceName))
            {
                redisOptions.InstanceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "DabpDesktopBoilerplate";
            }

            containerRegistry.RegisterSingleton<AppCacheService>(_ =>
            {
                if (!redisOptions.Enabled)
                {
                    return new InMemoryCacheService();
                }

                try
                {
                    return new RedisCacheService(redisOptions);
                }
                catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedExternalServiceException(ex))
                {
                    Log.Warning(ex, "Failed to initialize Redis cache. Falling back to in-memory cache.");
                    return new InMemoryCacheService();
                }
            });
        }

        private void ConfigureSqlSugarDb(IContainerRegistry containerRegistry, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                const string message =
                    "Missing database connection string. Configure appsettings.local.json or ConnectionStrings__Default.";

                Log.Fatal(message);
                MessageBox.Show(message, "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new InvalidOperationException(message);
            }

            containerRegistry.RegisterSingleton<ISqlSugarClient>(_ =>
            {
                SqlSugarScope sqlSugar = new SqlSugarScope(
                    new ConnectionConfig
                    {
                        DbType = SqlSugar.DbType.SqlServer,
                        ConnectionString = connectionString,
                        IsAutoCloseConnection = true,
                        ConfigureExternalServices = SqlSugarFluentService.GetConfigureExternalServices()
                    },
                    db =>
                    {
                        db.Aop.OnLogExecuting = (sql, _) => Log.Debug("SQL: {Sql}", sql);

                        db.Aop.OnError = (exp) =>
                        {
                            Log.Error(exp, "Database operation error");

                            if (IsConnectionError(exp))
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        var lockScreenService = Container.Resolve<ILockScreenService>();
                                        lockScreenService.Lock("数据库连接失败，请检查网络后解锁重试");
                                    }
                                    catch (InvalidOperationException lockEx)
                                    {
                                        Log.Error(lockEx, "Failed to trigger lock screen");
                                    }
                                    catch (ArgumentException lockEx)
                                    {
                                        Log.Error(lockEx, "Failed to trigger lock screen");
                                    }
                                });
                            }
                        };
                    });

                return sqlSugar;
            });
        }

        private static bool IsConnectionError(Exception exp)
        {
            if (exp == null)
            {
                return false;
            }

            string message = exp.ToString().ToLowerInvariant();
            return message.Contains("connection") ||
                   message.Contains("timeout") ||
                   message.Contains("network") ||
                   message.Contains("server was not found") ||
                   message.Contains("error locating server/instance") ||
                   message.Contains("login failed") ||
                   message.Contains("建立连接") ||
                   message.Contains("网络相关") ||
                   message.Contains("超时");
        }

        private static string GetLocalAppDataDirectory()
        {
            string appName = AppDomain.CurrentDomain.FriendlyName;
            return Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
        }

        private static Window CreateSplashScreen()
        {
            return new Window
            {
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = System.Windows.Media.Brushes.White,
                Content = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(24),
                    Child = new TextBlock
                    {
                        Text = "正在初始化数据库，请稍候...",
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = System.Windows.Media.Brushes.Black
                    }
                }
            };
        }
    }
}
