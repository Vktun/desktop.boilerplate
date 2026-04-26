using System.Threading.Tasks;

namespace Dabp.WpfWindow.Services;

public interface IAppStartupService
{
    Task InitializeDatabaseAsync();

    Task StartSessionTimeoutMonitoringAsync();
}
