using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dabp.Infrastructure;
using Dabp.Infrastructure.OrmSetting;
using Dabp.Services.Settings;
using Dabp.Utils.Security;
using Dabp.WpfWindow.Layout;
using Dabp.WpfWindow.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
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

        protected override void InitializeShell(DependencyObject shell)
        {
            // 异步初始化数据库，避免阻塞UI线程
            var splashScreen = new Window
            {
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Content = new TextBlock
                {
                    Text = "正在初始化数据库...",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = System.Windows.Media.Brushes.White
                }
            };
            splashScreen.Show();

            Task.Run(() => InitializeDatabaseAsync())
                .ContinueWith(t =>
                {
                    splashScreen.Dispatcher.Invoke(() => splashScreen.Close());

                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception?.InnerException ?? t.Exception!;
                        Log.Error(ex, "Database initialization failed");
                        MessageBox.Show(
                            $"Database initialization failed: {ex.Message}",
                            "Startup Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Application.Current?.Shutdown();
                        return;
                    }

                    base.InitializeShell(shell);

                    var regionManager = Container.Resolve<IRegionManager>();
                    var userSession = Container.Resolve<IUserSession>();

                    string initialView = userSession.IsLoggedIn
                        ? ViewNames.Dashboard
                        : ViewNames.LoginView;

                    regionManager.RequestNavigate(RegionNames.ContentRegion, initialView);
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task InitializeDatabaseAsync()
        {
            var initializer = Container.Resolve<IDatabaseInitializer>();
            await initializer.InitializeAsync();
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
            containerRegistry.RegisterSingleton<IMenuPermissionFilter, MenuPermissionFilter>();
            containerRegistry.RegisterSingleton<IUserSession, UserSession>();
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
                    });

                return sqlSugar;
            });
        }

        private static string GetLocalAppDataDirectory()
        {
            string appName = AppDomain.CurrentDomain.FriendlyName;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
        }
    }
}
