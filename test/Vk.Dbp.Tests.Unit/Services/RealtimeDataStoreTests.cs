using FluentAssertions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class RealtimeDataStoreTests
{
    private readonly RealtimeDataStore _store = new();

    private static PointValueSnapshot Snapshot(int pointId, string code, int deviceId, double value, DateTime timestamp)
    {
        return new PointValueSnapshot
        {
            PointId = pointId,
            PointCode = code,
            DeviceCode = $"DEV-{deviceId}",
            Value = value,
            Quality = DataQuality.Good,
            Timestamp = timestamp
        };
    }

    [Fact]
    public void GetSnapshot_AfterUpdate_ReturnsLatestValue()
    {
        _store.RegisterPoints(1, new[] { "TEMP-01" });

        _store.Update(Snapshot(1, "TEMP-01", 1, 42.5, DateTime.Now));

        _store.GetSnapshot("TEMP-01").Should().NotBeNull("更新后应能取到快照");
        _store.GetSnapshot("TEMP-01")!.Value.Should().Be(42.5, "快照应为最后一次更新的值");
    }

    [Fact]
    public void GetSnapshot_UnknownCode_ReturnsNull()
    {
        _store.GetSnapshot("NOPE").Should().BeNull("未注册点位应返回 null");
    }

    [Fact]
    public void Update_UnregisteredPoint_IsIgnored()
    {
        var act = () => _store.Update(Snapshot(9, "GHOST", 1, 1, DateTime.Now));

        act.Should().NotThrow("未注册点位的更新应被忽略而不是抛异常");
        _store.GetSnapshot("GHOST").Should().BeNull("被忽略的更新不应产生数据");
    }

    [Fact]
    public void GetTrendWindow_OverCapacity_KeepsNewestSamples()
    {
        var store = new RealtimeDataStore(trendCapacity: 3);
        store.RegisterPoints(1, new[] { "TEMP-01" });

        var baseTime = DateTime.Now;
        for (var i = 1; i <= 5; i++)
        {
            store.Update(Snapshot(1, "TEMP-01", 1, i, baseTime.AddSeconds(i)));
        }

        var window = store.GetTrendWindow("TEMP-01", 10);
        window.Should().HaveCount(3, "滚动窗口应截断到容量");
        window.Select(sample => sample.Value).Should().BeEquivalentTo(
            new[] { 3d, 4d, 5d },
            options => options.WithStrictOrdering(),
            "窗口应保留最新的样本");
        window[0].Timestamp.Should().Be(baseTime.AddSeconds(3), "窗口首条应为容量内最旧样本");
    }

    [Fact]
    public void GetTrendWindow_MaxCountBelowWindowSize_TrimsToRequestedCount()
    {
        var store = new RealtimeDataStore(trendCapacity: 10);
        store.RegisterPoints(1, new[] { "TEMP-01" });

        for (var i = 1; i <= 10; i++)
        {
            store.Update(Snapshot(1, "TEMP-01", 1, i, DateTime.Now.AddSeconds(i)));
        }

        var window = store.GetTrendWindow("TEMP-01", 4);
        window.Should().HaveCount(4, "请求条数小于窗口时应只返回最新 N 条");
        window.Select(sample => sample.Value).Should().BeEquivalentTo(
            new[] { 7d, 8d, 9d, 10d },
            options => options.WithStrictOrdering(),
            "应返回窗口尾部样本");
    }

    [Fact]
    public void GetDeviceSnapshots_FiltersByDevice()
    {
        _store.RegisterPoints(1, new[] { "A-01", "A-02" });
        _store.RegisterPoints(2, new[] { "B-01" });
        _store.Update(Snapshot(1, "A-01", 1, 1, DateTime.Now));
        _store.Update(Snapshot(2, "A-02", 1, 2, DateTime.Now));
        _store.Update(Snapshot(3, "B-01", 2, 3, DateTime.Now));

        _store.GetDeviceSnapshots(1).Should().HaveCount(2, "设备 1 应只包含自己的点位");
        _store.GetDeviceSnapshots(2).Should().ContainSingle("设备 2 应只包含自己的点位");
        _store.GetDeviceSnapshots(3).Should().BeEmpty("未注册设备应返回空集合");
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        _store.RegisterPoints(1, new[] { "A-01" });
        _store.Update(Snapshot(1, "A-01", 1, 1, DateTime.Now));

        _store.Clear();

        _store.GetAllSnapshots().Should().BeEmpty("清空后不应残留任何快照");
        _store.GetSnapshot("A-01").Should().BeNull("清空后按码查询应为 null");
    }
}
