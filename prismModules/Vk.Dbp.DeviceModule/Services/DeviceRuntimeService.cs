using System.Collections.Concurrent;
using System.IO;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Exceptions;
using Prism.Events;
using Prism.Ioc;
using Serilog;
using SqlSugar;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.Services.Alarm;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 设备运行时引擎：加载启用设备 → 每设备一个轮询循环（连接 → 周期采点 → 实时仓/历史缓冲/告警联动/节流事件），
/// 另有一条维护循环负责历史刷盘与每日保留清理。故障设备指数退避重连，不影响其余设备。
/// 本服务运行于后台线程，不触碰任何 UI 类型（Services/模块程序集不被 ConfigureAwait.Fody 织入）。
/// </summary>
public sealed class DeviceRuntimeService : IDeviceRuntimeService
{
    private const int DefaultPollIntervalMs = 1000;
    private const int MinPollIntervalMs = 100;
    private const int MaxPollIntervalMs = 60000;
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromSeconds(1);

    private readonly ISqlSugarClient _db;
    private readonly IProtocolDriverFactory _driverFactory;
    private readonly RealtimeDataStore _store;
    private readonly IHistoryDataService _historyService;
    private readonly SnapshotEventPublisher _publisher;
    private readonly IEventAggregator _eventAggregator;
    private readonly IContainerProvider _containerProvider;
    private readonly TimeProvider _timeProvider;

    private readonly ConcurrentDictionary<int, DeviceWorker> _workers = new();
    private readonly ConcurrentDictionary<int, DeviceRuntimeStatus> _statuses = new();
    private readonly SemaphoreSlim _startStopGate = new(1, 1);

    private CancellationTokenSource? _cts;
    private List<Task> _workerTasks = [];
    private Task? _maintenanceTask;
    private volatile bool _running;
    private int _pollIntervalMs = DefaultPollIntervalMs;
    private int _retentionDays = 90;
    private bool _historyEnabled = true;
    private DateTime _lastPurgeDate = DateTime.MinValue;

    // 懒解析：IAlarmService/ISystemConfigService 由 AccountModule 注册，引擎首次解析发生在启动完成后，
    // 模块必然已加载（HeaderViewModel 同款模式）
    private IAlarmService? _alarmService;
    private IAlarmService AlarmService => _alarmService ??= _containerProvider.Resolve<IAlarmService>();
    private ISystemConfigService? _systemConfigService;
    private ISystemConfigService SystemConfigService => _systemConfigService ??= _containerProvider.Resolve<ISystemConfigService>();

    /// <summary>
    /// 构造设备运行时引擎
    /// </summary>
    public DeviceRuntimeService(
        ISqlSugarClient db,
        IProtocolDriverFactory driverFactory,
        RealtimeDataStore store,
        IHistoryDataService historyService,
        SnapshotEventPublisher publisher,
        IEventAggregator eventAggregator,
        IContainerProvider containerProvider,
        TimeProvider timeProvider)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _driverFactory = driverFactory ?? throw new ArgumentNullException(nameof(driverFactory));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        await _startStopGate.WaitAsync();
        try
        {
            if (_running)
            {
                return;
            }

            await LoadEngineSettingsAsync();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var devices = await _db.Queryable<Device>()
                .Where(device => device.IsEnabled)
                .OrderBy(device => device.Id)
                .ToListAsync();

            var workerTasks = new List<Task>();
            foreach (var device in devices)
            {
                var worker = await CreateWorkerAsync(device, token);
                if (worker is null)
                {
                    continue;
                }

                _workers[device.Id] = worker;
                workerTasks.Add(StartWorker(worker));
            }

            _workerTasks = workerTasks;
            _maintenanceTask = Task.Run(() => RunMaintenanceLoopAsync(token), CancellationToken.None);
            _running = true;

            Log.Information("设备运行时引擎已启动，加载设备 {DeviceCount} 台", _workers.Count);
        }
        finally
        {
            _startStopGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        await _startStopGate.WaitAsync();
        try
        {
            if (!_running)
            {
                return;
            }

            _running = false;

            _cts?.Cancel();

            var tasks = new List<Task>(_workerTasks);
            if (_maintenanceTask is not null)
            {
                tasks.Add(_maintenanceTask);
            }

            try
            {
                await Task.WhenAll(tasks).WaitAsync(StopTimeout);
            }
            catch (TimeoutException)
            {
                Log.Warning("引擎停止等待任务超时，放弃等待剩余任务");
            }
            catch (Exception ex)
            {
                // 个别任务异常退出不影响整体停止流程（停止路径只记录不上抛）
                Log.Warning(ex, "引擎停止时部分任务异常退出");
            }

            _publisher.Flush();

            try
            {
                var flushed = await _historyService.FlushAsync();
                if (flushed > 0)
                {
                    Log.Information("引擎停止终刷历史样本 {Flushed} 条", flushed);
                }
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                Log.Warning(ex, "引擎停止终刷历史缓冲失败");
            }

            _workers.Clear();
            _store.Clear();
            _cts?.Dispose();
            _cts = null;
        }
        finally
        {
            _startStopGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RestartDeviceAsync(int deviceId)
    {
        if (!_workers.TryGetValue(deviceId, out var worker) || worker.Task is null)
        {
            return;
        }

        worker.Cancel();

        try
        {
            await worker.Task.WaitAsync(StopTimeout);
        }
        catch (TimeoutException)
        {
            Log.Warning("设备 {DeviceId} 重启等待旧任务超时", deviceId);
        }
        catch (Exception ex)
        {
            // 旧任务异常退出不阻断重启
            Log.Warning(ex, "设备 {DeviceId} 重启时旧任务异常退出", deviceId);
        }

        _workers.TryRemove(deviceId, out _);

        if (!_running || _cts is null)
        {
            return;
        }

        var device = await _db.Queryable<Device>().FirstAsync(entity => entity.Id == deviceId);
        if (device is not { IsEnabled: true })
        {
            SetStatus(deviceId, worker.Device.Code, DeviceRuntimeStatus.Disabled, "设备已停用");
            return;
        }

        var newWorker = await CreateWorkerAsync(device, _cts.Token);
        if (newWorker is not null)
        {
            _workers[deviceId] = newWorker;
            _workerTasks.Add(StartWorker(newWorker));
        }
    }

    /// <inheritdoc />
    public DeviceRuntimeStatus GetDeviceStatus(int deviceId)
    {
        return _workers.TryGetValue(deviceId, out var worker) ? worker.Status : DeviceRuntimeStatus.Stopped;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, DeviceRuntimeStatus> GetAllDeviceStatuses()
    {
        return new Dictionary<int, DeviceRuntimeStatus>(_statuses);
    }

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteCommandAsync(int commandId)
    {
        var command = await _db.Queryable<DeviceCommand>().FirstAsync(entity => entity.Id == commandId);
        if (command is not { IsEnabled: true })
        {
            return new CommandResult(false, $"命令不存在或已禁用: {commandId}");
        }

        if (!_workers.TryGetValue(command.DeviceId, out var worker))
        {
            return new CommandResult(false, "设备未在运行，无法执行命令");
        }

        if (!worker.RequestsByCode.TryGetValue(command.TargetPointCode, out var request)
            || !worker.DefinitionsById.TryGetValue(request.PointId, out var definition))
        {
            return new CommandResult(false, $"目标点位不存在: {command.TargetPointCode}");
        }

        var writeRequest = new PointWriteRequest
        {
            PointCode = command.TargetPointCode,
            Address = definition.Address,
            DataType = definition.DataType,
            Value = command.WriteValue ?? string.Empty
        };

        var snapshot = await worker.Driver.WritePointAsync(writeRequest, worker.CancellationToken);
        _store.Update(snapshot);

        var success = snapshot.Quality == DataQuality.Good;
        return new CommandResult(success, success ? $"命令 {command.Name} 执行成功" : snapshot.ValueText ?? "执行失败");
    }

    private async Task LoadEngineSettingsAsync()
    {
        try
        {
            _pollIntervalMs = Math.Clamp(
                await SystemConfigService.GetIntConfigAsync(SystemConfigKeys.IndustrialPollIntervalMs, DefaultPollIntervalMs),
                MinPollIntervalMs,
                MaxPollIntervalMs);
            _historyEnabled = await SystemConfigService.GetBoolConfigAsync(SystemConfigKeys.IndustrialHistoryEnabled, true);
            _retentionDays = Math.Max(
                1,
                await SystemConfigService.GetIntConfigAsync(SystemConfigKeys.IndustrialHistoryRetentionDays, 90));

            var throttleMs = Math.Clamp(
                await SystemConfigService.GetIntConfigAsync(SystemConfigKeys.IndustrialEventThrottleMs, 500),
                50,
                10000);
            _publisher.Configure(TimeSpan.FromMilliseconds(throttleMs));
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex) || ex is InvalidOperationException)
        {
            Log.Warning(ex, "读取工业引擎配置失败，使用默认值继续");
        }
    }

    private async Task<DeviceWorker?> CreateWorkerAsync(Device device, CancellationToken globalToken)
    {
        if (!_driverFactory.CanCreate(device.ProtocolType))
        {
            Log.Warning("设备 {DeviceCode} 的协议 {ProtocolType} 暂无可用驱动，已跳过", device.Code, device.ProtocolType);
            SetStatus(device.Id, device.Code, DeviceRuntimeStatus.Disabled, $"协议 {device.ProtocolType} 无可用驱动");
            return null;
        }

        var points = await _db.Queryable<DevicePoint>()
            .Where(point => point.DeviceId == device.Id && point.IsEnabled)
            .OrderBy(point => point.Id)
            .ToListAsync();

        var definitions = points
            .Select(point => new PointDefinition
            {
                Id = point.Id,
                DeviceId = point.DeviceId,
                Code = point.Code,
                Name = point.Name,
                DataType = point.DataType,
                Unit = point.Unit,
                Address = point.Address,
                AlarmHigh = point.AlarmHigh,
                AlarmLow = point.AlarmLow,
                IsEnabled = point.IsEnabled
            })
            .ToList();

        var requests = definitions
            .Select(definition => new PointReadRequest
            {
                PointId = definition.Id,
                PointCode = definition.Code,
                DataType = definition.DataType,
                Address = definition.Address
            })
            .ToList();

        var worker = new DeviceWorker(
            this,
            device,
            definitions,
            requests,
            ResolvePollInterval(device.ConnectionConfig));

        _store.RegisterPoints(device.Id, requests.Select(request => request.PointCode));
        SetStatus(device.Id, device.Code, DeviceRuntimeStatus.Stopped);
        return worker;
    }

    private Task StartWorker(DeviceWorker worker)
    {
        worker.Start(_cts?.Token ?? CancellationToken.None);
        return worker.Task!;
    }

    private int ResolvePollInterval(string? connectionConfig)
    {
        // 设备级覆盖只认 {"pollIntervalMs":N} 一个键（JSON 解析失败回落全局间隔）
        if (string.IsNullOrWhiteSpace(connectionConfig))
        {
            return _pollIntervalMs;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(connectionConfig);
            if (document.RootElement.TryGetProperty("pollIntervalMs", out var value)
                && value.TryGetInt32(out var interval))
            {
                return Math.Clamp(interval, MinPollIntervalMs, MaxPollIntervalMs);
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // 配置非法时回落全局间隔
        }

        return _pollIntervalMs;
    }

    private async Task RunMaintenanceLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(MaintenanceInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    if (_historyEnabled)
                    {
                        await _historyService.FlushIfDueAsync();
                    }

                    var today = _timeProvider.GetLocalNow().Date;
                    if (today != _lastPurgeDate)
                    {
                        // 先记日期再清理：清理失败也等次日重试，避免 DB 故障期每日风暴
                        _lastPurgeDate = today;
                        var deleted = await _historyService.PurgeExpiredAsync(_retentionDays);
                        if (deleted > 0)
                        {
                            Log.Information("历史数据保留清理完成，删除 {Deleted} 条", deleted);
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
                {
                    Log.Warning(ex, "引擎维护周期任务失败，下个周期重试");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
    }

    private async Task HandleSamplesAsync(DeviceWorker worker, IReadOnlyList<PointValueSnapshot> samples)
    {
        foreach (var sample in samples)
        {
            _store.Update(sample);
        }

        if (_historyEnabled)
        {
            // Enqueue 自行过滤仅 Good 质量样本
            _historyService.Enqueue(samples);
        }

        foreach (var sample in samples)
        {
            if (!worker.DefinitionsById.TryGetValue(sample.PointId, out var definition))
            {
                continue;
            }

            var transition = worker.Detector.Evaluate(definition, sample);
            if (transition is null)
            {
                continue;
            }

            try
            {
                await RaiseThresholdAlarmAsync(worker.Device, definition, transition);
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                // 告警创建失败不能中断采集
                Log.Warning(ex, "点位 {PointCode} 告警创建失败", definition.Code);
            }
        }

        foreach (var sample in samples)
        {
            _publisher.Add(worker.Device.Code, sample);
        }
    }

    private async Task RaiseThresholdAlarmAsync(Device device, PointDefinition point, AlarmTransition transition)
    {
        var direction = transition.IsHigh ? "高限" : "低限";

        // AlarmCode 必须带秒级时间戳：AlarmService.CreateAlarmAsync 按 AlarmCode 等值回查 Id，重复编码会拿到旧记录
        var record = new AlarmRecord
        {
            AlarmCode = $"{device.Code}.{point.Code}.{(transition.IsHigh ? "HIGH" : "LOW")}.{_timeProvider.GetLocalNow():yyyyMMddHHmmss}",
            AlarmTitle = $"{device.Name} {point.Name} {direction}告警",
            AlarmContent = $"实测值 {transition.ActualValue:F2}{point.Unit ?? string.Empty}，超限阈值 {transition.Threshold}{point.Unit ?? string.Empty}",
            AlarmSource = device.Code,
            AlarmLevel = transition.IsHigh ? AlarmLevel.Critical : AlarmLevel.Warning,
            AlarmType = AlarmType.Threshold,
            ThresholdValue = transition.Threshold,
            ActualValue = transition.ActualValue,
            Unit = point.Unit,
            UserId = 0
        };

        var created = await AlarmService.CreateAlarmAsync(record);
        if (!created)
        {
            Log.Warning("阈值告警写入失败: {AlarmCode}", record.AlarmCode);
            return;
        }

        // AlarmService 只写库不发事件；引擎补发 Triggered（Header/AppAlarm 已订阅，勿双发 CountChanged）
        _eventAggregator.GetEvent<AlarmTriggeredEvent>().Publish(new AlarmTriggeredPayload
        {
            AlarmRecordId = record.Id,
            AlarmCode = record.AlarmCode,
            Level = record.AlarmLevel,
            Title = record.AlarmTitle,
            Content = record.AlarmContent,
            TriggeredTime = record.TriggeredTime,
            Source = record.AlarmSource,
            UserId = record.UserId
        });

        Log.Information("阈值告警已创建: {AlarmCode}", record.AlarmCode);
    }

    private async Task RaiseDeviceFaultAlarmAsync(Device device, string reason)
    {
        try
        {
            var now = _timeProvider.GetLocalNow();
            var record = new AlarmRecord
            {
                AlarmCode = $"{device.Code}.FAULT.{now:yyyyMMddHHmmss}",
                AlarmTitle = $"{device.Name} 通讯故障",
                AlarmContent = reason.Length > 500 ? reason[..500] : reason,
                AlarmSource = device.Code,
                AlarmLevel = AlarmLevel.Critical,
                AlarmType = AlarmType.Device,
                UserId = 0
            };

            if (await AlarmService.CreateAlarmAsync(record))
            {
                _eventAggregator.GetEvent<AlarmTriggeredEvent>().Publish(new AlarmTriggeredPayload
                {
                    AlarmRecordId = record.Id,
                    AlarmCode = record.AlarmCode,
                    Level = record.AlarmLevel,
                    Title = record.AlarmTitle,
                    Content = record.AlarmContent,
                    TriggeredTime = record.TriggeredTime,
                    Source = record.AlarmSource,
                    UserId = record.UserId
                });
            }
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            Log.Warning(ex, "设备 {DeviceCode} 故障告警创建失败", device.Code);
        }
    }

    private void SetStatus(int deviceId, string deviceCode, DeviceRuntimeStatus newStatus, string? message = null)
    {
        var oldStatus = _statuses.GetOrAdd(deviceId, DeviceRuntimeStatus.Stopped);
        if (oldStatus == newStatus)
        {
            return;
        }

        _statuses[deviceId] = newStatus;
        Log.Debug("设备 {DeviceCode} 状态 {OldStatus} → {NewStatus}", deviceCode, oldStatus, newStatus);

        // PublisherThread 发布；UI 订阅方（UIThread 订阅）自动调度回界面线程
        _eventAggregator.GetEvent<DeviceStatusChangedEvent>().Publish(new DeviceStatusPayload
        {
            DeviceId = deviceId,
            DeviceCode = deviceCode,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Message = message,
            ChangedAt = _timeProvider.GetLocalNow().LocalDateTime
        });
    }

    private static bool IsRecoverable(Exception ex)
    {
        return ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex)
               || ex is TimeoutException
               || ex is IOException
               || ex is NotSupportedException;
    }

    /// <summary>
    /// 单设备轮询工作单元（引擎私有协作对象）
    /// </summary>
    private sealed class DeviceWorker
    {
        private readonly DeviceRuntimeService _engine;

        private CancellationTokenSource? _linkedCts;
        private int _consecutiveFailures;
        private bool _faultAlarmRaised;

        public DeviceWorker(
            DeviceRuntimeService engine,
            Device device,
            List<PointDefinition> definitions,
            List<PointReadRequest> requests,
            int pollIntervalMs)
        {
            _engine = engine;
            Device = device;
            DefinitionsById = definitions.ToDictionary(definition => definition.Id);
            RequestsByCode = requests.ToDictionary(request => request.PointCode, StringComparer.OrdinalIgnoreCase);
            Requests = requests;
            PollIntervalMs = pollIntervalMs;
            Driver = engine._driverFactory.Create(new DeviceConnectionInfo
            {
                DeviceId = device.Id,
                Code = device.Code,
                ProtocolType = device.ProtocolType,
                ConnectionConfig = device.ConnectionConfig
            });
        }

        public Device Device { get; }

        public IProtocolDriver Driver { get; private set; }

        public IReadOnlyList<PointReadRequest> Requests { get; }

        public IReadOnlyDictionary<int, PointDefinition> DefinitionsById { get; }

        public IReadOnlyDictionary<string, PointReadRequest> RequestsByCode { get; }

        public AlarmEdgeDetector Detector { get; } = new();

        public DeviceRuntimeStatus Status { get; private set; } = DeviceRuntimeStatus.Stopped;

        public Task? Task { get; private set; }

        private int PollIntervalMs { get; }

        public CancellationToken CancellationToken => _linkedCts?.Token ?? CancellationToken.None;

        public void Start(CancellationToken globalToken)
        {
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
            Task = Task.Run(() => RunAsync(_linkedCts.Token), CancellationToken.None);
        }

        public void Cancel()
        {
            _linkedCts?.Cancel();
        }

        private async Task RunAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await RunPollingSessionAsync(token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex) when (IsRecoverable(ex))
                    {
                        _consecutiveFailures++;
                        SetWorkerStatus(DeviceRuntimeStatus.Faulted, ex.Message);
                        Log.Warning(ex, "设备 {DeviceCode} 轮询失败（连续 {Failures} 次），退避后重连",
                            Device.Code, _consecutiveFailures);

                        // 一次连续故障期只建一条设备故障告警，恢复连接后复位
                        if (!_faultAlarmRaised)
                        {
                            _faultAlarmRaised = true;
                            await _engine.RaiseDeviceFaultAlarmAsync(Device, ex.Message);
                        }

                        try
                        {
                            await Task.Delay(AlarmEdgeDetector.CalculateRetryBackoff(_consecutiveFailures), token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        await DisposeDriverSafely();
                        Driver = _engine._driverFactory.Create(new DeviceConnectionInfo
                        {
                            DeviceId = Device.Id,
                            Code = Device.Code,
                            ProtocolType = Device.ProtocolType,
                            ConnectionConfig = Device.ConnectionConfig
                        });
                    }
                }
            }
            finally
            {
                await DisposeDriverSafely();
                SetWorkerStatus(DeviceRuntimeStatus.Stopped);
                _linkedCts?.Dispose();
                _linkedCts = null;
            }
        }

        private async Task RunPollingSessionAsync(CancellationToken token)
        {
            SetWorkerStatus(DeviceRuntimeStatus.Connecting);
            await Driver.ConnectAsync(token);
            SetWorkerStatus(DeviceRuntimeStatus.Running);
            _consecutiveFailures = 0;
            _faultAlarmRaised = false;

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollIntervalMs));
            while (await timer.WaitForNextTickAsync(token))
            {
                var samples = await Driver.ReadPointsAsync(Requests, token);
                await _engine.HandleSamplesAsync(this, samples);
                _engine._publisher.TryFlushDue();
            }
        }

        private async Task DisposeDriverSafely()
        {
            try
            {
                await Driver.DisposeAsync();
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                Log.Warning(ex, "设备 {DeviceCode} 驱动释放失败", Device.Code);
            }
        }

        private void SetWorkerStatus(DeviceRuntimeStatus status, string? message = null)
        {
            Status = status;
            _engine.SetStatus(Device.Id, Device.Code, status, message);
        }
    }
}
