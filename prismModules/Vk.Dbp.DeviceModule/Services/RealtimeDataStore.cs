using System.Collections.Concurrent;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 实时数据仓：线程安全的"当前值 + 滚动窗口"存储。
/// 引擎写侧直接依赖本具体类（RegisterPoints/Update/Clear），
/// 消费者通过 <see cref="IRealtimeDataService"/> 只读视图访问同一实例。
/// </summary>
public sealed class RealtimeDataStore : IRealtimeDataService
{
    private readonly int _trendCapacity;
    private readonly ConcurrentDictionary<string, PointEntry> _points = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, HashSet<string>> _devicePoints = new();
    private readonly object _registrationLock = new();

    /// <summary>
    /// 构造实时数据仓
    /// </summary>
    /// <param name="trendCapacity">每点位滚动窗口容量（趋势展示时长 = 容量 × 轮询周期）</param>
    public RealtimeDataStore(int trendCapacity = 600)
    {
        if (trendCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trendCapacity), "滚动窗口容量必须为正数");
        }

        _trendCapacity = trendCapacity;
    }

    /// <summary>
    /// 注册点位（引擎启动/设备重启时调用；重复注册幂等）
    /// </summary>
    public void RegisterPoints(int deviceId, IEnumerable<string> pointCodes)
    {
        lock (_registrationLock)
        {
            var codes = _devicePoints.GetOrAdd(deviceId, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            foreach (var pointCode in pointCodes)
            {
                _points.TryAdd(pointCode, new PointEntry { DeviceId = deviceId });
                codes.Add(pointCode);
            }
        }
    }

    /// <summary>
    /// 更新点位快照（未注册的点位被忽略）；同步维护滚动窗口
    /// </summary>
    public void Update(PointValueSnapshot snapshot)
    {
        if (!_points.TryGetValue(snapshot.PointCode, out var entry))
        {
            return;
        }

        lock (entry.Lock)
        {
            entry.Latest = snapshot;
            entry.Window.Enqueue(snapshot);
            while (entry.Window.Count > _trendCapacity)
            {
                entry.Window.Dequeue();
            }
        }
    }

    /// <summary>
    /// 清空全部实时数据（引擎停止时调用）
    /// </summary>
    public void Clear()
    {
        lock (_registrationLock)
        {
            _points.Clear();
            _devicePoints.Clear();
        }
    }

    /// <inheritdoc />
    public PointValueSnapshot? GetSnapshot(string pointCode)
    {
        return _points.TryGetValue(pointCode, out var entry) ? entry.Latest : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<PointValueSnapshot> GetDeviceSnapshots(int deviceId)
    {
        if (!_devicePoints.TryGetValue(deviceId, out var codes))
        {
            return Array.Empty<PointValueSnapshot>();
        }

        List<PointValueSnapshot> snapshots;
        lock (_registrationLock)
        {
            snapshots = codes
                .Select(code => _points.TryGetValue(code, out var entry) ? entry.Latest : null)
                .Where(snapshot => snapshot is not null)
                .Select(snapshot => snapshot!)
                .ToList();
        }

        return snapshots;
    }

    /// <inheritdoc />
    public IReadOnlyList<PointValueSnapshot> GetAllSnapshots()
    {
        return _points.Values
            .Select(entry => entry.Latest)
            .Where(snapshot => snapshot is not null)
            .Select(snapshot => snapshot!)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<PointValueSnapshot> GetTrendWindow(string pointCode, int maxCount)
    {
        if (!_points.TryGetValue(pointCode, out var entry))
        {
            return Array.Empty<PointValueSnapshot>();
        }

        lock (entry.Lock)
        {
            if (entry.Window.Count <= maxCount)
            {
                return entry.Window.ToArray();
            }

            return entry.Window.Skip(entry.Window.Count - maxCount).ToArray();
        }
    }

    private sealed class PointEntry
    {
        public int DeviceId { get; init; }

        public PointValueSnapshot? Latest { get; set; }

        public Queue<PointValueSnapshot> Window { get; } = new();

        public object Lock { get; } = new();
    }
}
