using System.Collections.ObjectModel;
using Prism.Commands;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 点位班报子页：设备 + 班次预设（早/中/夜）或自定义时段 → 各点位统计 → Excel 导出。
/// </summary>
public class ShiftReportViewModel : ReportTabViewModelBase
{
    private const string CustomShift = "自定义";

    private readonly ShiftReportGenerator _generator;
    private readonly IDeviceCatalogService _catalogService;

    private DeviceSummary? _selectedDevice;
    private string _selectedShift = "早班";
    private DateTime? _referenceDate = DateTime.Today;
    private DateTime _customStart = DateTime.Today.AddHours(8);
    private DateTime _customEnd = DateTime.Today.AddHours(16);
    private bool _isBusy;
    private bool _hasResult;

    /// <summary>
    /// 构造班报子页 VM
    /// </summary>
    public ShiftReportViewModel(ShiftReportGenerator generator, IDeviceCatalogService catalogService, IExportService exportService)
        : base(exportService)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));

        QueryCommand = new DelegateCommand(async () => await QueryAsync(), () => SelectedDevice is not null && !IsBusy)
            .ObservesProperty(() => SelectedDevice)
            .ObservesProperty(() => IsBusy);
        ExportExcelCommand = new DelegateCommand(async () => await ExportAsync(), () => HasResult && !IsBusy)
            .ObservesProperty(() => HasResult)
            .ObservesProperty(() => IsBusy);
    }

    /// <summary>
    /// 设备清单
    /// </summary>
    public ObservableCollection<DeviceSummary> Devices { get; } = [];

    /// <summary>
    /// 选中设备
    /// </summary>
    public DeviceSummary? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    /// <summary>
    /// 班次预设
    /// </summary>
    public ObservableCollection<string> ShiftPresets { get; } = new(["早班", "中班", "夜班", CustomShift]);

    /// <summary>
    /// 选中的班次
    /// </summary>
    public string SelectedShift
    {
        get => _selectedShift;
        set
        {
            if (SetProperty(ref _selectedShift, value))
            {
                RaisePropertyChanged(nameof(IsCustomShift));
                RaisePropertyChanged(nameof(IsPresetShift));
            }
        }
    }

    /// <summary>
    /// 是否自定义班次
    /// </summary>
    public bool IsCustomShift => SelectedShift == CustomShift;

    /// <summary>
    /// 是否预设班次（参考日期选择器启用开关，与 IsCustomShift 互补）
    /// </summary>
    public bool IsPresetShift => !IsCustomShift;

    /// <summary>
    /// 参考日期（预设班次以该日的 8/16/24 点切分）
    /// </summary>
    public DateTime? ReferenceDate
    {
        get => _referenceDate;
        set => SetProperty(ref _referenceDate, value);
    }

    /// <summary>
    /// 自定义开始时间
    /// </summary>
    public DateTime CustomStart
    {
        get => _customStart;
        set => SetProperty(ref _customStart, value);
    }

    /// <summary>
    /// 自定义结束时间
    /// </summary>
    public DateTime CustomEnd
    {
        get => _customEnd;
        set => SetProperty(ref _customEnd, value);
    }

    /// <summary>
    /// 是否正在生成
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// 是否已有结果
    /// </summary>
    public bool HasResult
    {
        get => _hasResult;
        private set => SetProperty(ref _hasResult, value);
    }

    /// <summary>
    /// 班报预览行
    /// </summary>
    public ObservableCollection<ShiftReportRow> Rows { get; } = [];

    /// <summary>
    /// 查询命令
    /// </summary>
    public DelegateCommand QueryCommand { get; }

    /// <summary>
    /// 导出 Excel
    /// </summary>
    public DelegateCommand ExportExcelCommand { get; }

    /// <summary>
    /// 装载设备清单（internal 供测试）
    /// </summary>
    internal async Task LoadDevicesAsync()
    {
        var devices = await _catalogService.GetDevicesAsync(includeDisabled: false);
        Devices.Clear();
        foreach (var device in devices)
        {
            Devices.Add(device);
        }

        SelectedDevice ??= Devices.FirstOrDefault();
    }

    /// <summary>
    /// 解析班次时间窗（internal 供测试）
    /// </summary>
    internal bool TryGetWindow(out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        if (SelectedShift == CustomShift)
        {
            start = CustomStart;
            end = CustomEnd;
            return start < end;
        }

        if (ReferenceDate is not { } date)
        {
            return false;
        }

        var day = date.Date;
        switch (SelectedShift)
        {
            case "早班":
                start = day.AddHours(8);
                end = day.AddHours(16).AddTicks(-1);
                return true;
            case "中班":
                start = day.AddHours(16);
                end = day.AddDays(1).AddTicks(-1);
                return true;
            case "夜班":
                start = day;
                end = day.AddHours(8).AddTicks(-1);
                return true;
            default:
                return false;
        }
    }

    private async Task QueryAsync()
    {
        if (SelectedDevice is null || !TryGetWindow(out var start, out var end))
        {
            if (IsCustomShift && CustomStart >= CustomEnd)
            {
                NotifyWarning("自定义时段的开始时间必须早于结束时间");
            }

            return;
        }

        IsBusy = true;
        try
        {
            var rows = await _generator.BuildRowsAsync(SelectedDevice.Code, start, end);
            Rows.Clear();
            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            HasResult = rows.Count > 0;
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            NotifyError($"班报生成失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        if (SelectedDevice is null || !TryGetWindow(out var start, out var end))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var parameters = new Vk.Dbp.Contracts.Extensions.ReportParameters
            {
                StartDate = start,
                EndDate = end,
                CustomParameters = { ["deviceCode"] = SelectedDevice.Code }
            };

            var bytes = await _generator.GenerateReportAsync(parameters);
            var defaultName = $"点位班报-{SelectedDevice.Code}-{start:yyyyMMdd-HHmm}";
            await ExportBytesAsync(bytes, defaultName, ".xlsx");
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex)
                                    || Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex))
        {
            NotifyError($"导出失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 警告通知（测试子类替换）
    /// </summary>
    protected virtual void NotifyWarning(string message)
    {
        HandyControl.Controls.Growl.Warning(message);
    }
}
