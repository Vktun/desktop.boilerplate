using Dabp.Infrastructure;
using Dabp.Infrastructure.Entities;
using Prism.Ioc;
using Serilog;
using Dabp.Utils.Exceptions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.Contracts.Services;

namespace Dabp.WpfWindow.Services;

public sealed class AppStartupService(
    IDatabaseInitializer databaseInitializer,
    ISessionTimeoutService sessionTimeoutService,
    IContainerProvider containerProvider) : IAppStartupService
{
    public async Task InitializeDatabaseAsync()
    {
        await databaseInitializer.InitializeAsync();
    }

    public async Task StartSessionTimeoutMonitoringAsync()
    {
        try
        {
            var systemConfigService = containerProvider.Resolve<ISystemConfigService>();
            bool enabled = await systemConfigService.GetSessionTimeoutEnabledAsync();
            int minutes = await systemConfigService.GetSessionTimeoutMinutesAsync();

            sessionTimeoutService.TimeoutMinutes = minutes;

            if (enabled)
            {
                sessionTimeoutService.StartMonitoring();
            }

            Log.Information(
                "Session timeout monitoring initialized from database: Enabled={Enabled}, Timeout={Minutes} minutes",
                enabled,
                minutes);
        }
        catch (Exception ex) when (
            ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex) ||
            ex is InvalidOperationException)
        {
            Log.Warning(ex, "Failed to load session config from database, using defaults");
            sessionTimeoutService.TimeoutMinutes = 15;
            sessionTimeoutService.StartMonitoring();
        }
    }

    public async Task StartIndustrialEngineAsync()
    {
        try
        {
            var systemConfigService = containerProvider.Resolve<ISystemConfigService>();
            bool engineEnabled = await systemConfigService.GetBoolConfigAsync(
                SystemConfigKeys.IndustrialEngineEnabled, true);
            if (!engineEnabled)
            {
                Log.Information("Industrial runtime engine disabled by config");
                return;
            }

            // IDeviceRuntimeService 由 DeviceModule 注册；本方法在 OnInitialized 之后调用，模块必然已加载
            var engine = containerProvider.Resolve<IDeviceRuntimeService>();
            await engine.StartAsync();
        }
        catch (Exception ex)
        {
            // 捕获面宽于 ExpectedOperationExceptionFilter：Unity 容器解析失败的异常类型不在其覆盖内。
            // 引擎启动失败只降级（无实时数据），绝不阻断主程序。
            Log.Warning(ex, "Failed to start industrial runtime engine");
        }
    }
}
