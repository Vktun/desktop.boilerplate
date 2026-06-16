using Dabp.Infrastructure;
using Prism.Ioc;
using Serilog;
using Dabp.Utils.Exceptions;
using Vk.Dbp.AccountModule.Services;
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
}
