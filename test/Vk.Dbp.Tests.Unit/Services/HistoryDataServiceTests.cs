using Dabp.Infrastructure.Entities;
using FluentAssertions;
using SqlSugar;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class HistoryDataServiceTests : IClassFixture<TestDatabaseFixture>
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly ISqlSugarClient _db;
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);

    public HistoryDataServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();
    }

    private HistoryDataService CreateService(int batchSize = 500, TimeSpan? flushInterval = null)
    {
        return new HistoryDataService(_db, _timeProvider, batchSize, flushInterval);
    }

    private void ResetDatabase()
    {
        _db.Deleteable<PointHistory>().Where(_ => true).ExecuteCommand();
    }

    private static PointValueSnapshot Sample(double value, DateTime timestamp, string pointCode = "HIST-01", int pointId = 1, DataQuality quality = DataQuality.Good)
    {
        return new PointValueSnapshot
        {
            PointId = pointId,
            PointCode = pointCode,
            DeviceCode = "SIM-TEST",
            Value = value,
            ValueText = value.ToString("F2"),
            Quality = quality,
            Timestamp = timestamp
        };
    }

    [Fact]
    public async Task FlushAsync_PersistsOnlyGoodQualitySamplesWithFields()
    {
        var baseTime = FixedNow.LocalDateTime;
        var service = CreateService();
        service.Enqueue(new[]
        {
            Sample(1.5, baseTime),
            Sample(2.5, baseTime.AddSeconds(1)),
            Sample(double.NaN, baseTime.AddSeconds(2), quality: DataQuality.Bad)
        });

        var written = await service.FlushAsync();

        written.Should().Be(2, "只有 Good 质量样本应落库");
        var rows = await _db.Queryable<PointHistory>().OrderBy(row => row.Timestamp).ToListAsync();
        rows.Should().HaveCount(2, "Bad 样本不应写入历史表");
        rows[0].PointCode.Should().Be("HIST-01", "历史行应冗余点位编码");
        rows[0].Value.Should().Be(1.5, "历史行应保存数值采样值");
        rows[0].Quality.Should().Be(DataQuality.Good, "历史行质量应为 Good");
    }

    [Fact]
    public async Task FlushIfDueAsync_ReachingBatchSize_TriggersImmediately()
    {
        var baseTime = FixedNow.LocalDateTime;
        var service = CreateService(batchSize: 3);
        service.Enqueue(new[]
        {
            Sample(1, baseTime),
            Sample(2, baseTime.AddSeconds(1)),
            Sample(3, baseTime.AddSeconds(2))
        });

        var written = await service.FlushIfDueAsync();

        written.Should().Be(3, "缓冲达到批量阈值应立即触发刷盘");
        (await _db.Queryable<PointHistory>().CountAsync()).Should().Be(3, "刷盘后历史表应有 3 行");
    }

    [Fact]
    public async Task FlushIfDueAsync_BelowBatchSizeAndInterval_Waits()
    {
        var service = CreateService(batchSize: 500, flushInterval: TimeSpan.FromSeconds(5));
        service.Enqueue(new[] { Sample(1, FixedNow.LocalDateTime) });
        _timeProvider.Advance(TimeSpan.FromSeconds(1));

        var written = await service.FlushIfDueAsync();

        written.Should().Be(0, "未达批量且未到间隔不应刷盘");
        (await _db.Queryable<PointHistory>().CountAsync()).Should().Be(0, "未刷盘时历史表不应有数据");
    }

    [Fact]
    public async Task FlushIfDueAsync_ElapsedInterval_Triggers()
    {
        var service = CreateService(batchSize: 500, flushInterval: TimeSpan.FromSeconds(5));
        service.Enqueue(new[] { Sample(1, FixedNow.LocalDateTime) });
        _timeProvider.Advance(TimeSpan.FromSeconds(6));

        var written = await service.FlushIfDueAsync();

        written.Should().Be(1, "超过刷盘间隔应触发落库");
    }

    [Fact]
    public async Task QueryAsync_FiltersRangeAndDownsamplesKeepingEndpoints()
    {
        var baseTime = FixedNow.LocalDateTime;
        var rows = Enumerable.Range(0, 10)
            .Select(index => new PointHistory
            {
                PointId = 1,
                PointCode = "HIST-01",
                Value = index,
                Quality = DataQuality.Good,
                Timestamp = baseTime.AddMinutes(index)
            })
            .ToList();
        rows.Add(new PointHistory
        {
            PointId = 1,
            PointCode = "HIST-01",
            Value = 99,
            Quality = DataQuality.Good,
            Timestamp = baseTime.AddHours(1)
        });
        await _db.Insertable(rows).ExecuteCommandAsync();

        var result = await CreateService().QueryAsync(
            "HIST-01",
            baseTime,
            baseTime.AddMinutes(10),
            maxPoints: 4);

        result.Should().HaveCount(4, "超过 maxPoints 应等距降采样");
        result.First().Value.Should().Be(0, "降采样应保留首条样本");
        result.Last().Value.Should().Be(9, "降采样应保留末条样本");
        result.Should().OnlyContain(point => point.Value <= 9, "范围外的样本应被过滤");
    }

    [Fact]
    public async Task PurgeExpiredAsync_DeletesOnlyRowsOlderThanRetention()
    {
        var now = FixedNow.LocalDateTime;
        await _db.Insertable(new[]
        {
            new PointHistory { PointId = 1, PointCode = "HIST-01", Value = 1, Quality = DataQuality.Good, Timestamp = now.AddDays(-100) },
            new PointHistory { PointId = 1, PointCode = "HIST-01", Value = 2, Quality = DataQuality.Good, Timestamp = now.AddDays(-1) }
        }).ExecuteCommandAsync();

        var deleted = await CreateService().PurgeExpiredAsync(90);

        deleted.Should().Be(1, "只应删除超过保留期的数据");
        var remaining = await _db.Queryable<PointHistory>().ToListAsync();
        remaining.Should().ContainSingle("保留期内的数据不应被删除");
        remaining[0].Value.Should().Be(2, "剩余数据应为未过期样本");
    }

    [Fact]
    public async Task FlushAsync_EmptyQueue_ReturnsZeroWithoutWrite()
    {
        var written = await CreateService().FlushAsync();

        written.Should().Be(0, "空缓冲刷盘应返回 0");
    }
}
