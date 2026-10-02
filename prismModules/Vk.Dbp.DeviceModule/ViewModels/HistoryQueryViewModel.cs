using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Growl = HandyControl.Controls.Growl;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 趋势历史查询页视图模型：设备/点位选择 + 时间范围（预设/自定义）→ 历史服务降采样查询 →
/// ScottPlot 趋势（View 侧桥接）+ 统计摘要 + 明细表 + Excel 导出。
/// </summary>
public class HistoryQueryViewModel : BindableBase, INavigationAware
{
    /// <summary>
    /// 图表/明细的最大点位数（历史服务等距降采样保首尾）
    /// </summary>
    public const int MaxChartPoints = 2000;

    private const string CustomPreset = "自定义";

    private readonly IHistoryDataService _historyService;
    private readonly IDeviceCatalogService _catalogService;
    private readonly IExportService _exportService;

    private DeviceSummary? _selectedDevice;
    private PointDefinition? _selectedPoint;
    private string _selectedRangePreset = "近1小时";
    private DateTime _customStart = DateTime.Now.AddHours(-8);
    private DateTime _customEnd = DateTime.Now;
    private bool _isQuerying;
    private bool _hasResult;
    private string _statusText = "选择设备与点位后查询";

    // —— 可测性旁路（DeviceManagementViewModel 同款）：通知在测试子类中替换为记录器 ——

    /// <summary>
    /// 成功通知（默认 Growl）
    /// </summary>
    protected virtual void NotifySuccess(string message)
    {
        Growl.Success(message);
    }

    /// <summary>
    /// 失败通知（默认 Growl）
    /// </summary>
    protected virtual void NotifyError(string message)
    {
        Growl.Error(message);
    }

    /// <summary>
    /// 警告通知（默认 Growl）
    /// </summary>
    protected virtual void NotifyWarning(string message)
    {
        Growl.Warning(message);
    }

    /// <summary>
    /// 构造趋势查询视图模型
    /// </summary>
    public HistoryQueryViewModel(
        IHistoryDataService historyService,
        IDeviceCatalogService catalogService,
        IExportService exportService)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

        QueryCommand = new DelegateCommand(async () => await QueryAsync(), () => CanQuery)
            .ObservesProperty(() => SelectedDevice)
            .ObservesProperty(() => SelectedPoint)
            .ObservesProperty(() => SelectedRangePreset);
        ExportCommand = new DelegateCommand(async () => await ExportAsync(), () => HasResult)
            .ObservesProperty(() => HasResult);
    }

    /// <summary>
    /// 图表数据变化（View 侧 code-behind 订阅后重绘）
    /// </summary>
    public event EventHandler? SamplesChanged;

    /// <summary>
    /// 当前查询结果（不可变；时间升序）
    /// </summary>
    public IReadOnlyList<HistoryPoint>? CurrentSamples { get; private set; }

    /// <summary>
    /// 设备清单
    /// </summary>
    public ObservableCollection<DeviceSummary> Devices { get; } = [];

    /// <summary>
    /// 选中设备（变更时加载点位）
    /// </summary>
    public DeviceSummary? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                _ = LoadPointsAsync();
            }
        }
    }

    /// <summary>
    /// 选中设备的点位清单
    /// </summary>
    public ObservableCollection<PointDefinition> Points { get; } = [];

    /// <summary>
    /// 选中点位
    /// </summary>
    public PointDefinition? SelectedPoint
    {
        get => _selectedPoint;
        set => SetProperty(ref _selectedPoint, value);
    }

    /// <summary>
    /// 时间范围预设项
    /// </summary>
    public ObservableCollection<string> RangePresets { get; } = new(new[]
    {
        "近1小时", "近8小时", "今日", "近24小时", "近7天", CustomPreset
    });

    /// <summary>
    /// 选中的时间范围预设
    /// </summary>
    public string SelectedRangePreset
    {
        get => _selectedRangePreset;
        set
        {
            if (SetProperty(ref _selectedRangePreset, value))
            {
                RaisePropertyChanged(nameof(IsCustomRange));
            }
        }
    }

    /// <summary>
    /// 是否自定义范围（控制起止时间选择器可用性）
    /// </summary>
    public bool IsCustomRange => SelectedRangePreset == CustomPreset;

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
    /// 是否正在查询
    /// </summary>
    public bool IsQuerying
    {
        get => _isQuerying;
        private set => SetProperty(ref _isQuerying, value);
    }

    /// <summary>
    /// 是否已有查询结果（导出按钮开关）
    /// </summary>
    public bool HasResult
    {
        get => _hasResult;
        private set => SetProperty(ref _hasResult, value);
    }

    /// <summary>
    /// 状态文本（查询结果/提示）
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// 最大值
    /// </summary>
    public string MaxText { get; private set; } = "--";

    /// <summary>
    /// 最小值
    /// </summary>
    public string MinText { get; private set; } = "--";

    /// <summary>
    /// 平均值
    /// </summary>
    public string AvgText { get; private set; } = "--";

    /// <summary>
    /// 样本数
    /// </summary>
    public string CountText { get; private set; } = "--";

    /// <summary>
    /// 查询命令
    /// </summary>
    public DelegateCommand QueryCommand { get; }

    /// <summary>
    /// 导出 Excel 命令
    /// </summary>
    public DelegateCommand ExportCommand { get; }

    private bool CanQuery => SelectedDevice is not null && SelectedPoint is not null && TryGetRange(out _, out _);

    /// <inheritdoc />
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await LoadDevicesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    /// <summary>
    /// 按预设/自定义解析查询时间范围（internal 供测试直接验证映射）
    /// </summary>
    internal bool TryGetRange(out DateTime start, out DateTime end)
    {
        var now = DateTime.Now;
        switch (SelectedRangePreset)
        {
            case "近1小时":
                start = now.AddHours(-1);
                end = now;
                return true;
            case "近8小时":
                start = now.AddHours(-8);
                end = now;
                return true;
            case "今日":
                start = now.Date;
                end = now;
                return true;
            case "近24小时":
                start = now.AddDays(-1);
                end = now;
                return true;
            case "近7天":
                start = now.AddDays(-7);
                end = now;
                return true;
            case CustomPreset:
                start = CustomStart;
                end = CustomEnd;
                return start < end;
            default:
                start = default;
                end = default;
                return false;
        }
    }

    // internal 供测试直调（OnNavigatedTo 的装载入口）
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

    private async Task LoadPointsAsync()
    {
        var device = SelectedDevice;
        Points.Clear();
        SelectedPoint = null;

        if (device is null)
        {
            return;
        }

        var points = await _catalogService.GetPointsAsync(device.Id);
        if (SelectedDevice?.Id != device.Id)
        {
            return;
        }

        foreach (var point in points.Where(point => point.IsEnabled))
        {
            Points.Add(point);
        }

        SelectedPoint = Points.FirstOrDefault();
    }

    private async Task QueryAsync()
    {
        if (SelectedPoint is null || !TryGetRange(out var start, out var end))
        {
            if (IsCustomRange && CustomStart >= CustomEnd)
            {
                NotifyWarning("自定义范围的开始时间必须早于结束时间");
            }

            return;
        }

        IsQuerying = true;
        try
        {
            var samples = await _historyService.QueryAsync(SelectedPoint.Code, start, end, MaxChartPoints);

            lock (this)
            {
                CurrentSamples = samples;
                HasResult = samples.Count > 0;
                UpdateStatistics(samples);
            }

            StatusText = HasResult
                ? $"[{SelectedPoint.Code}] {start:MM-dd HH:mm} ~ {end:MM-dd HH:mm} · {samples.Count} 个样本（超出 {MaxChartPoints} 自动降采样）"
                : $"[{SelectedPoint.Code}] 该时间范围内无历史数据";
            SamplesChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            NotifyError($"历史查询失败: {ex.Message}");
            StatusText = "查询失败，请稍后重试";
        }
        finally
        {
            IsQuerying = false;
        }
    }

    private async Task ExportAsync()
    {
        if (CurrentSamples is not { Count: > 0 } samples || SelectedPoint is null)
        {
            return;
        }

        var rows = samples.Select(sample => new TrendExportRow
        {
            Timestamp = sample.Timestamp,
            Value = sample.Value
        }).ToList();

        var options = new ExcelExportOptions
        {
            Title = $"{SelectedPoint.Name}（{SelectedPoint.Code}）历史趋势",
            ColumnDisplayNames = new Dictionary<string, string>
            {
                [nameof(TrendExportRow.Timestamp)] = "时间",
                [nameof(TrendExportRow.Value)] = "数值"
            },
            ColumnFormats = new Dictionary<string, string>
            {
                [nameof(TrendExportRow.Timestamp)] = "yyyy-MM-dd HH:mm:ss"
            }
        };

        try
        {
            var fileName = $"{SelectedPoint.Code}-历史趋势-{DateTime.Now:yyyyMMdd-HHmmss}";
            var filePath = await _exportService.ExportToExcelAsync(rows, fileName, options);
            NotifySuccess("历史数据已导出");
            await _exportService.OpenExportedFileAsync(filePath);
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex)
                                    || Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex))
        {
            NotifyError($"导出失败: {ex.Message}");
        }
    }

    private void UpdateStatistics(IReadOnlyList<HistoryPoint> samples)
    {
        var numeric = samples.Select(sample => sample.Value).Where(value => value is not null).Select(value => value!.Value).ToList();

        if (numeric.Count == 0)
        {
            MaxText = MinText = AvgText = CountText = "--";
        }
        else
        {
            MaxText = numeric.Max().ToString("0.##");
            MinText = numeric.Min().ToString("0.##");
            AvgText = (numeric.Sum() / numeric.Count).ToString("0.##");
            CountText = numeric.Count.ToString();
        }

        RaisePropertyChanged(nameof(MaxText));
        RaisePropertyChanged(nameof(MinText));
        RaisePropertyChanged(nameof(AvgText));
        RaisePropertyChanged(nameof(CountText));
    }

    /// <summary>
    /// 导出行模型（列名经 ExcelExportOptions 映射为中文）
    /// </summary>
    public sealed class TrendExportRow
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 数值
        /// </summary>
        public double? Value { get; set; }
    }
}
