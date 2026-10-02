using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Services;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 报表中心页视图模型（组合根）：持有告警/班报/审计三个子 VM，装载时刷新设备清单。
/// </summary>
public sealed class ReportCenterViewModel : BindableBase, INavigationAware
{
    private readonly ShiftReportViewModel _shiftReport;

    /// <summary>
    /// 构造报表中心视图模型
    /// </summary>
    public ReportCenterViewModel(
        AlarmSummaryReportGenerator alarmGenerator,
        ShiftReportGenerator shiftGenerator,
        AuditLogReportGenerator auditGenerator,
        IDeviceCatalogService catalogService,
        IExportService exportService)
    {
        AlarmReport = new AlarmReportViewModel(alarmGenerator, exportService);
        _shiftReport = new ShiftReportViewModel(shiftGenerator, catalogService, exportService);
        ShiftReport = _shiftReport;
        AuditReport = new AuditReportViewModel(auditGenerator, exportService);
    }

    /// <summary>
    /// 告警报表子页
    /// </summary>
    public AlarmReportViewModel AlarmReport { get; }

    /// <summary>
    /// 点位班报子页
    /// </summary>
    public ShiftReportViewModel ShiftReport { get; }

    /// <summary>
    /// 审计报表子页
    /// </summary>
    public AuditReportViewModel AuditReport { get; }

    /// <inheritdoc />
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await _shiftReport.LoadDevicesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }
}
