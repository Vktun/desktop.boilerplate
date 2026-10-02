using System.Collections.Concurrent;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Exceptions;
using Serilog;
using SqlSugar;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 点位历史数据服务：内存缓冲 + 定时/定量批量落盘 + 降采样查询 + 保留策略清理。
/// 落库失败时样本回队一次并进入指数退避（DB 故障期间以限流频率重试，不丢已采数据）。
/// </summary>
public sealed class HistoryDataService : IHistoryDataService
{
    // 缓冲硬顶：DB 长时间不可用时防止内存膨胀，超限丢最旧
    private const int MaxQueueCapacity = 50000;

    // 单次查询行数硬顶：防御异常大的时间范围拖垮内存
    private const int MaxQueryRows = 100000;

    // 连续失败退避上限：5s × 2^3 = 8 个维护周期
    private const int MaxBackoffShift = 3;

    private readonly ISqlSugarClient _db;
    private readonly TimeProvider _timeProvider;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private readonly ConcurrentQueue<PointValueSnapshot> _pending = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    private int _pendingCount;
    private int _consecutiveFlushFailures;
    private DateTime _lastFlushAt = DateTime.MinValue;

    /// <summary>
    /// 构造历史数据服务
    /// </summary>
    public HistoryDataService(ISqlSugarClient db, TimeProvider timeProvider, int batchSize = 500, TimeSpan? flushInterval = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _batchSize = batchSize > 0 ? batchSize : throw new ArgumentOutOfRangeException(nameof(batchSize));
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(5);
        _lastFlushAt = _timeProvider.GetLocalNow().LocalDateTime;
    }

    /// <inheritdoc />
    public void Enqueue(IReadOnlyList<PointValueSnapshot> samples)
    {
        if (samples.Count == 0)
        {
            return;
        }

        var enqueued = 0;
        foreach (var sample in samples)
        {
            // 历史库只存 Good 质量样本：Bad/Uncertain 数据不进趋势
            if (sample.Quality != DataQuality.Good)
            {
                continue;
            }

            _pending.Enqueue(sample);
            enqueued++;
            Interlocked.Increment(ref _pendingCount);
        }

        var dropped = 0;
        while (Volatile.Read(ref _pendingCount) > MaxQueueCapacity)
        {
            if (!_pending.TryDequeue(out _))
            {
                break;
            }

            Interlocked.Decrement(ref _pendingCount);
            dropped++;
        }

        if (dropped > 0)
        {
            Log.Warning("历史缓冲超过硬顶 {Capacity}，丢弃最旧样本 {Dropped} 条", MaxQueueCapacity, dropped);
        }
    }

    /// <inheritdoc />
    public async Task<int> FlushAsync()
    {
        await _flushLock.WaitAsync();
        try
        {
            var batch = new List<PointValueSnapshot>();
            while (_pending.TryDequeue(out var sample))
            {
                batch.Add(sample);
                Interlocked.Decrement(ref _pendingCount);
            }

            if (batch.Count == 0)
            {
                _lastFlushAt = _timeProvider.GetLocalNow().LocalDateTime;
                return 0;
            }

            try
            {
                var rows = batch
                    .Select(sample => new PointHistory
                    {
                        PointId = sample.PointId,
                        PointCode = sample.PointCode,
                        Value = sample.Value,
                        ValueText = sample.ValueText,
                        Quality = sample.Quality,
                        Timestamp = sample.Timestamp
                    })
                    .ToList();

                await _db.Insertable(rows).ExecuteCommandAsync();
                _consecutiveFlushFailures = 0;
                _lastFlushAt = _timeProvider.GetLocalNow().LocalDateTime;
                return rows.Count;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                // 落库失败：样本按原序回队一次，进入退避（见 FlushIfDueAsync）；
                // 连接类错误同时会触发 shell 的锁屏 AOP，此处只负责限流重试，不吞不绕过
                foreach (var sample in batch)
                {
                    _pending.Enqueue(sample);
                    Interlocked.Increment(ref _pendingCount);
                }

                _consecutiveFlushFailures++;
                _lastFlushAt = _timeProvider.GetLocalNow().LocalDateTime;
                Log.Warning(ex, "历史数据刷盘失败，{Count} 条样本回队，连续失败 {Failures} 次", batch.Count, _consecutiveFlushFailures);
                return 0;
            }
        }
        finally
        {
            _flushLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> FlushIfDueAsync()
    {
        var now = _timeProvider.GetLocalNow().LocalDateTime;
        var pendingCount = Volatile.Read(ref _pendingCount);

        if (pendingCount == 0)
        {
            return 0;
        }

        // 连续失败后的退避间隔：5s × 2^min(失败次数,3)，把 DB 故障期的重试压到数秒一次
        var backoffMultiplier = 1 << Math.Min(_consecutiveFlushFailures, MaxBackoffShift);
        var requiredElapsed = TimeSpan.FromTicks(_flushInterval.Ticks * backoffMultiplier);
        var sinceLastFlush = now - _lastFlushAt;

        var sizeTriggered = pendingCount >= _batchSize && _consecutiveFlushFailures == 0;
        if (!sizeTriggered && sinceLastFlush < requiredElapsed)
        {
            return 0;
        }

        return await FlushAsync();
    }

    /// <inheritdoc />
    public async Task<List<HistoryPoint>> QueryAsync(string pointCode, DateTime startTime, DateTime endTime, int maxPoints)
    {
        if (maxPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPoints), "maxPoints 必须为正数");
        }

        var rows = await _db.Queryable<PointHistory>()
            .Where(history => history.PointCode == pointCode
                              && history.Timestamp >= startTime
                              && history.Timestamp <= endTime)
            .OrderBy(history => history.Timestamp)
            .Take(MaxQueryRows)
            .ToListAsync();

        if (rows.Count > MaxQueryRows - 1)
        {
            Log.Warning("点位 {PointCode} 查询结果达到行数硬顶 {Limit}，结果可能被截断", pointCode, MaxQueryRows);
        }

        return Downsample(rows, maxPoints);
    }

    /// <inheritdoc />
    public async Task<int> PurgeExpiredAsync(int retentionDays)
    {
        if (retentionDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "保留天数必须为正数");
        }

        var cutoff = _timeProvider.GetLocalNow().LocalDateTime - TimeSpan.FromDays(retentionDays);
        return await _db.Deleteable<PointHistory>()
            .Where(history => history.Timestamp < cutoff)
            .ExecuteCommandAsync();
    }

    // 等距降采样并保留首尾样本
    private static List<HistoryPoint> Downsample(List<PointHistory> rows, int maxPoints)
    {
        if (rows.Count <= maxPoints)
        {
            return rows
                .Select(row => new HistoryPoint { Timestamp = row.Timestamp, Value = row.Value })
                .ToList();
        }

        var stride = (rows.Count - 1) / (double)(maxPoints - 1);
        var result = new List<HistoryPoint>(maxPoints);
        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)Math.Round(i * stride);
            if (i == maxPoints - 1)
            {
                index = rows.Count - 1;
            }

            result.Add(new HistoryPoint { Timestamp = rows[index].Timestamp, Value = rows[index].Value });
        }

        return result;
    }
}
