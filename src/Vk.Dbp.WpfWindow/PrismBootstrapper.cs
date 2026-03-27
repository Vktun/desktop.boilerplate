using Dabp.Infrastructure;
using Dabp.Infrastructure.OrmSetting;
using Dabp.Utils.Algorithm;
using Dabp.Utils.Security;
using Dabp.WpfWindow.Layout;
using Dabp.WpfWindow.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            base.InitializeShell(shell);

            InitializeDatabaseAsync().GetAwaiter().GetResult();

            var regionManager = Container.Resolve<IRegionManager>();
            var userSession = Vk.Dbp.AccountModule.Models.UserSession.Instance;

            if (userSession.IsLoggedIn)
            {
                regionManager.RequestNavigate("ContentRegion", "Dashboard");
            }
            else
            {
                regionManager.RequestNavigate("ContentRegion", "LoginView");
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var initializer = Container.Resolve<IDatabaseInitializer>();
                await initializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库初始化失败");
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var config = ConfigConfiguration();
            ConfigureLogging();
            ConfigureSqlSugarDb(containerRegistry, config);

            containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
            containerRegistry.RegisterSingleton<IPasswordHasher, PasswordHasher>();
            containerRegistry.RegisterSingleton<IDatabaseInitializer, DatabaseInitializer>();
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

        #region 读取配置文件
        private IConfigurationRoot ConfigConfiguration()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            return config;
        }
        #endregion

        #region 日志配置
        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
        .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.File("Logs/logs.txt",
            outputTemplate: "[{Timestamp:MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}", // 输出日期格式
            rollingInterval: RollingInterval.Day, // 日志按日保存
            rollOnFileSizeLimit: true, // 限制单个文件的最大长度
            encoding: Encoding.UTF8, // 文件字符编码
            retainedFileCountLimit: 10, // 最大保存文件数
            fileSizeLimitBytes: 100 * 1024) // 最大单个文件长度
        .CreateLogger();
        }
        #endregion
        #region 配置数据库
        void ConfigureSqlSugarDb(IContainerRegistry containerRegistry, IConfiguration configuration)
        {
            string connectionString = string.Empty;
            if (configuration["ConnectionStrings:Default"] != null)
            {
                var connectValue = configuration["ConnectionStrings:Default"].ToString();
                connectionString = SM4.Decrypt(connectValue);
            }
            containerRegistry.RegisterScoped<ISqlSugarClient>(s =>
            {
                //Scoped用SqlSugarClient 
                SqlSugarClient sqlSugar = new SqlSugarClient(new ConnectionConfig()
                {
                    DbType = SqlSugar.DbType.SqlServer,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,
                    ConfigureExternalServices= SqlSugarFluentService.GetConfigureExternalServices()
                },
               db =>
               {
                   //每次上下文都会执行
                   db.Aop.OnLogExecuting = (sql, pars) =>
                   {

                   };
               });
                return sqlSugar;
            });
        }
        #endregion
    }
}
