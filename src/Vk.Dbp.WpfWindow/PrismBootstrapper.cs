using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Dabp.Infrastructure;
using Dabp.Infrastructure.OrmSetting;
using Dabp.Infrastructure.Repositories;
using Dabp.Services.Export;
using Dabp.Services.Settings;
using Dabp.Utils.Security;
using Dabp.WpfWindow.Layout;
using Dabp.WpfWindow.Services;
using Dabp.WpfWindow.ViewModels;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SqlSugar;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;
using Vk.Dbp.WpfWindow.Constants;
using Vk.Dbp.WpfWindow.ViewModels;

namespace Dabp.WpfWindow
{
    internal class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override async void InitializeShell(DependencyObject shell)
        {
            Window? splashScreen = null;
            ShutdownMode originalShutdownMode = Application.Current?.ShutdownMode ?? ShutdownMode.OnLastWindowClose;

            try
            {
                if (Application.Current != null)
                {
                    Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                }

                splashScreen = CreateSplashScreen();
                splashScreen.Show();

                await InitializeDatabaseAsync();

                base.InitializeShell(shell);
                ShowShellWindow(shell);

                if (Application.Current != null)
                {
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }

                var regionManager = Container.Resolve<IRegionManager>();
                var userSession = Container.Resolve<IUserSession>();

                string initialView = userSession.IsLoggedIn
                    ? ViewNames.Dashboard
                    : ViewNames.LoginView;

                regionManager.RequestNavigate(RegionNames.ContentRegion, initialView);

                _ = StartSessionTimeoutMonitoringAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Application startup failed");
                MessageBox.Show(
                    $"Application startup failed: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current?.Shutdown();
            }
            finally
            {
                splashScreen?.Close();

                if (Application.Current?.MainWindow == null && Application.Current != null)
                {
                    Application.Current.ShutdownMode = originalShutdownMode;
                }
            }
        }

        private async Task StartSessionTimeoutMonitoringAsync()
        {
            try
            {
                var timeoutService = Container.Resolve<ISessionTimeoutService>();

                try
                {
                    var configService = Container.Resolve<Vk.Dbp.AccountModule.Services.ISystemConfigService>();
                    var enabled = await configService.GetSessionTimeoutEnabledAsync();
                    var minutes = await configService.GetSessionTimeoutMinutesAsync();

                    timeoutService.TimeoutMinutes = minutes;

                    if (enabled)
                    {
                        timeoutService.StartMonitoring();
                    }

                    Log.Information(
                        "Session timeout monitoring initialized from database: Enabled={Enabled}, Timeout={Minutes} minutes",
                        enabled,
                        minutes);
                }
                catch (Exception dbEx)
                {
                    Log.Warning(dbEx, "Failed to load session config from database, using defaults");
                    timeoutService.TimeoutMinutes = 15;
                    timeoutService.StartMonitoring();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start session timeout monitoring");
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            var initializer = Container.Resolve<IDatabaseInitializer>();
            await initializer.InitializeAsync();
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
            ConfigureSqlSugarDb(containerRegistry, configuration);

            containerRegistry.RegisterSingleton<IAppSettingsService, AppSettingsService>();
            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
            containerRegistry.RegisterSingleton<IPasswordHasher, PasswordHasher>();
            containerRegistry.RegisterSingleton<IDatabaseInitializer, DatabaseInitializer>();
            containerRegistry.Register(typeof(IRepository<>), typeof(SqlSugarRepository<>));
            containerRegistry.RegisterSingleton<IMenuPermissionFilter, MenuPermissionFilter>();
            containerRegistry.RegisterSingleton<IUserSession, UserSession>();
            containerRegistry.RegisterSingleton<IExportService, ExportService>();
            containerRegistry.RegisterSingleton<ILockScreenService, LockScreenService>();
            containerRegistry.RegisterSingleton<ISessionTimeoutService, SessionTimeoutService>();
            containerRegistry.RegisterSingleton<LockScreenViewModel>();
            containerRegistry.RegisterSingleton<AppAlarmViewModel>();
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
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private void ConfigureLogging()
        {
            string logDirectory = Path.Combine(GetLocalAppDataDirectory(), "Logs");
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
                    Path.Combine(logDirectory, "logs.txt"),
                    outputTemplate: "[{Timestamp:MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    encoding: Encoding.UTF8,
                    retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 100 * 1024)
                .CreateLogger();
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
                                    catch (Exception lockEx)
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
            return Path.Combine(
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

