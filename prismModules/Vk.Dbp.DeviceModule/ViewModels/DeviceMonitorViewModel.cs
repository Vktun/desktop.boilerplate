using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 实时监控页视图模型：设备列表 → 点位实时表格 → 选中点位滚动趋势。
/// 事件订阅遵循 AppNotificationViewModel 三件套范式（UIThread + keepAlive + token + Dispose）。
/// </summary>
public sealed class DeviceMonitorViewModel : BindableBase, INavigationAware, IDisposable
{
    private const int TrendCapacity = 600;

    private readonly IRealtimeDataService _realtimeService;
    private readonly IDeviceCatalogService _catalogService;
    private readonly IDeviceRuntimeService _runtimeService;
    private readonly IEventAggregator _eventAggregator;

    private readonly Dictionary<string, PointRowViewModel> _rowsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<PointValueSnapshot> _trendBuffer = new();
    private readonly object _trendLock = new();
    private readonly List<DeviceListItemViewModel> _allDevices = [];
    private int _pointLoadSequence;

    private SubscriptionToken? _valuesChangedToken;
    private SubscriptionToken? _statusChangedToken;
    private bool _isDisposed;

    private bool _isLoading;
    private bool _isPaused;
    private string _engineStatusText = "引擎未启动";
    private DeviceListItemViewModel? _selectedDevice;
    private PointRowViewModel? _selectedPoint;

    /// <summary>
    /// 构造实时监控视图模型
    /// </summary>
    public DeviceMonitorViewModel(
        IRealtimeDataService realtimeService,
        IDeviceCatalogService catalogService,
        IDeviceRuntimeService runtimeService,
        IEventAggregator eventAggregator)
    {
        _realtimeService = realtimeService ?? throw new ArgumentNullException(nameof(realtimeService));
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _runtimeService = runtimeService ?? throw new ArgumentNullException(nameof(runtimeService));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        RefreshCommand = new DelegateCommand(async () => await LoadDevicesAsync());
        TogglePauseCommand = new DelegateCommand(() => IsPaused = !IsPaused);

        // ThreadOption.UIThread 订阅要求构造时存在同步上下文（shell 单例 VM 同款约束）
        _valuesChangedToken = _eventAggregator.GetEvent<PointValuesChangedEvent>()
            .Subscribe(OnPointValuesChanged, ThreadOption.UIThread, keepSubscriberReferenceAlive: true);
        _statusChangedToken = _eventAggregator.GetEvent<DeviceStatusChangedEvent>()
            .Subscribe(OnDeviceStatusChanged, ThreadOption.UIThread, keepSubscriberReferenceAlive: true);
    }

    /// <summary>
    /// 趋势数据变化（View 侧 code-behind 订阅后重绘 ScottPlot，保持 VM 无 UI 依赖）
    /// </summary>
    public event EventHandler? TrendDataChanged;

    /// <summary>
    /// 当前趋势快照（不可变；时间升序，最多 600 点）
    /// </summary>
    public IReadOnlyList<PointValueSnapshot>? CurrentTrend { get; private set; }

    /// <summary>
    /// 刷新命令（重载设备目录与实时快照）
    /// </summary>
    public DelegateCommand RefreshCommand { get; }

    /// <summary>
    /// 暂停/继续实时刷新
    /// </summary>
    public DelegateCommand TogglePauseCommand { get; }

    /// <summary>
    /// 引擎状态摘要
    /// </summary>
    public string EngineStatusText
    {
        get => _engineStatusText;
        private set => SetProperty(ref _engineStatusText, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 是否暂停实时刷新
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        private set => SetProperty(ref _isPaused, value);
    }

    /// <summary>
    /// 可见设备列表（仅启用设备）
    /// </summary>
    public ObservableCollection<DeviceListItemViewModel> Devices { get; } = [];

    /// <summary>
    /// 选中设备
    /// </summary>
    public DeviceListItemViewModel? SelectedDevice
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
    /// 当前设备点位行集合
    /// </summary>
    public ObservableCollection<PointRowViewModel> Points { get; } = [];

    /// <summary>
    /// 选中点位（趋势数据源）
    /// </summary>
    public PointRowViewModel? SelectedPoint
    {
        get => _selectedPoint;
        set
        {
            if (SetProperty(ref _selectedPoint, value))
            {
                ResetTrend();
            }
        }
    }

    /// <inheritdoc />
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        // 复用同一实例，导航返回时保留状态
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _valuesChangedToken?.Dispose();
        _statusChangedToken?.Dispose();
        _valuesChangedToken = null;
        _statusChangedToken = null;
    }

    private async Task LoadDevicesAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var devices = await _catalogService.GetDevicesAsync(includeDisabled: false);
            _allDevices.Clear();
            _allDevices.AddRange(devices.Select(device => new DeviceListItemViewModel
            {
                Id = device.Id,
                Code = device.Code,
                Name = device.Name,
                ProtocolTypeText = device.ProtocolType
            }));

            Devices.Clear();
            foreach (var item in _allDevices)
            {
                var status = _runtimeService.GetDeviceStatus(item.Id);
                item.ApplyStatus(status);
                Devices.Add(item);
            }

            UpdateEngineStatusText();
            SelectedDevice ??= Devices.FirstOrDefault();

            if (SelectedDevice is null)
            {
                Points.Clear();
                _rowsByCode.Clear();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPointsAsync()
    {
        if (_isDisposed || SelectedDevice is null)
        {
            return;
        }

        // 请求序号防过期结果：快速切换设备时只应用最后一次请求的结果
        var sequence = ++_pointLoadSequence;
        var deviceId = SelectedDevice.Id;

        var points = await _catalogService.GetPointsAsync(deviceId);
        if (_isDisposed || sequence != _pointLoadSequence || SelectedDevice?.Id != deviceId)
        {
            return;
        }

        var rows = points
            .Where(point => point.IsEnabled)
            .Select(point => new PointRowViewModel
            {
                Id = point.Id,
                Code = point.Code,
                Name = point.Name,
                Unit = point.Unit,
                AlarmHigh = point.AlarmHigh,
                AlarmLow = point.AlarmLow
            })
            .ToList();

        Points.Clear();
        _rowsByCode.Clear();
        foreach (var row in rows)
        {
            _rowsByCode[row.Code] = row;
            Points.Add(row);
        }

        // 用实时仓当前快照填充初值（避免等待下一轮事件才有显示）
        foreach (var snapshot in _realtimeService.GetDeviceSnapshots(deviceId))
        {
            if (_rowsByCode.TryGetValue(snapshot.PointCode, out var row))
            {
                row.ApplySnapshot(snapshot);
            }
        }

        SelectedPoint = Points.FirstOrDefault(row => !row.Code.StartsWith("VALVE", StringComparison.OrdinalIgnoreCase))
                        ?? Points.FirstOrDefault();
    }

    private void OnPointValuesChanged(PointValuesBatchPayload payload)
    {
        if (_isDisposed || IsPaused || SelectedDevice is null)
        {
            return;
        }

        if (!string.Equals(payload.DeviceCode, SelectedDevice.Code, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var trendSample = default(PointValueSnapshot);
        foreach (var sample in payload.Samples)
        {
            if (_rowsByCode.TryGetValue(sample.PointCode, out var row))
            {
                row.ApplySnapshot(sample);
            }

            if (SelectedPoint is not null &&
                string.Equals(sample.PointCode, SelectedPoint.Code, StringComparison.OrdinalIgnoreCase))
            {
                trendSample = sample;
            }
        }

        if (trendSample is not null && trendSample.Quality == DataQuality.Good)
        {
            lock (_trendLock)
            {
                _trendBuffer.Enqueue(trendSample);
                while (_trendBuffer.Count > TrendCapacity)
                {
                    _trendBuffer.Dequeue();
                }

                CurrentTrend = _trendBuffer.ToArray();
            }

            TrendDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDeviceStatusChanged(DeviceStatusPayload payload)
    {
        if (_isDisposed)
        {
            return;
        }

        var item = _allDevices.FirstOrDefault(device => device.Id == payload.DeviceId);
        item?.ApplyStatus(payload.NewStatus);
        UpdateEngineStatusText();
    }

    private void ResetTrend()
    {
        lock (_trendLock)
        {
            _trendBuffer.Clear();
            if (SelectedPoint is not null)
            {
                foreach (var snapshot in _realtimeService.GetTrendWindow(SelectedPoint.Code, TrendCapacity))
                {
                    _trendBuffer.Enqueue(snapshot);
                }
            }

            CurrentTrend = _trendBuffer.ToArray();
        }

        TrendDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateEngineStatusText()
    {
        var statuses = _runtimeService.GetAllDeviceStatuses();
        if (statuses.Count == 0)
        {
            EngineStatusText = "引擎未启动";
            return;
        }

        var running = statuses.Values.Count(status => status == DeviceRuntimeStatus.Running);
        var faulted = statuses.Values.Count(status => status == DeviceRuntimeStatus.Faulted);
        EngineStatusText = faulted > 0
            ? $"运行 {running}/{statuses.Count} 台 · 故障 {faulted} 台"
            : $"运行 {running}/{statuses.Count} 台";
    }
}
