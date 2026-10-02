using Prism.Events;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 实时快照节流发布器：按设备聚合窗口内样本（同点取最新），距上次发布不足最小间隔时挂起，
/// 到点后按设备批量发布 <see cref="PointValuesChangedEvent"/>（PublisherThread；UI 订阅方自行调度回 UI 线程）。
/// </summary>
public sealed class SnapshotEventPublisher
{
    private readonly IEventAggregator _eventAggregator;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();

    // deviceCode → (pointCode → 最新快照)
    private readonly Dictionary<string, Dictionary<string, PointValueSnapshot>> _pending = new(StringComparer.OrdinalIgnoreCase);

    private TimeSpan _minInterval;
    private DateTimeOffset _lastPublishAt;

    /// <summary>
    /// 构造节流发布器（DI 入口，默认 500ms；引擎读取 SystemConfig 后经 <see cref="Configure"/> 调整）
    /// </summary>
    public SnapshotEventPublisher(IEventAggregator eventAggregator, TimeProvider timeProvider)
        : this(eventAggregator, TimeSpan.FromMilliseconds(500), timeProvider)
    {
    }

    /// <summary>
    /// 构造节流发布器（测试可注入任意间隔）
    /// </summary>
    public SnapshotEventPublisher(IEventAggregator eventAggregator, TimeSpan minInterval, TimeProvider timeProvider)
    {
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _minInterval = minInterval;
        _lastPublishAt = _timeProvider.GetLocalNow();
    }

    /// <summary>
    /// 调整最小发布间隔（引擎按 SystemConfig 工业配置调用）
    /// </summary>
    public void Configure(TimeSpan minInterval)
    {
        if (minInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minInterval), "发布间隔必须为正");
        }

        lock (_lock)
        {
            _minInterval = minInterval;
        }
    }

    /// <summary>
    /// 追加一条待发布样本（窗口内同点覆盖，只保留最新值）
    /// </summary>
    public void Add(string deviceCode, PointValueSnapshot sample)
    {
        lock (_lock)
        {
            if (!_pending.TryGetValue(deviceCode, out var samples))
            {
                samples = new Dictionary<string, PointValueSnapshot>(StringComparer.OrdinalIgnoreCase);
                _pending[deviceCode] = samples;
            }

            samples[sample.PointCode] = sample;
        }
    }

    /// <summary>
    /// 到期才发布（引擎每轮询周期调用）；未到期为轻量空操作
    /// </summary>
    public void TryFlushDue()
    {
        List<KeyValuePair<string, IReadOnlyList<PointValueSnapshot>>>? batches = null;

        lock (_lock)
        {
            if (_pending.Count == 0)
            {
                return;
            }

            if (_timeProvider.GetLocalNow() - _lastPublishAt < _minInterval)
            {
                return;
            }

            batches = TakePendingLocked();
            _lastPublishAt = _timeProvider.GetLocalNow();
        }

        Publish(batches);
    }

    /// <summary>
    /// 无视间隔立即发布剩余样本（引擎停止时终发最后一帧）
    /// </summary>
    public void Flush()
    {
        List<KeyValuePair<string, IReadOnlyList<PointValueSnapshot>>>? batches = null;

        lock (_lock)
        {
            if (_pending.Count == 0)
            {
                return;
            }

            batches = TakePendingLocked();
            _lastPublishAt = _timeProvider.GetLocalNow();
        }

        Publish(batches);
    }

    private List<KeyValuePair<string, IReadOnlyList<PointValueSnapshot>>> TakePendingLocked()
    {
        var batches = _pending
            .Select(pair => new KeyValuePair<string, IReadOnlyList<PointValueSnapshot>>(pair.Key, pair.Value.Values.ToList()))
            .ToList();
        _pending.Clear();
        return batches;
    }

    private void Publish(List<KeyValuePair<string, IReadOnlyList<PointValueSnapshot>>> batches)
    {
        var @event = _eventAggregator.GetEvent<PointValuesChangedEvent>();
        var now = _timeProvider.GetLocalNow().LocalDateTime;

        foreach (var batch in batches)
        {
            @event.Publish(new PointValuesBatchPayload
            {
                DeviceCode = batch.Key,
                Timestamp = now,
                Samples = batch.Value
            });
        }
    }
}
