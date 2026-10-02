using FluentAssertions;
using Prism.Events;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class SnapshotEventPublisherTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Start);
    private readonly EventAggregator _eventAggregator = new();

    private SnapshotEventPublisher CreatePublisher(int intervalMs = 500)
    {
        return new SnapshotEventPublisher(_eventAggregator, TimeSpan.FromMilliseconds(intervalMs), _timeProvider);
    }

    private static PointValueSnapshot Sample(string code, double value)
    {
        return new PointValueSnapshot
        {
            PointId = 1,
            PointCode = code,
            DeviceCode = "DEV",
            Value = value,
            Quality = DataQuality.Good,
            Timestamp = DateTime.Now
        };
    }

    [Fact]
    public void TryFlushDue_WithinInterval_AggregatesLatestPerPoint()
    {
        var publisher = CreatePublisher(intervalMs: 500);
        var batches = new List<PointValuesBatchPayload>();
        _eventAggregator.GetEvent<PointValuesChangedEvent>().Subscribe(batches.Add);

        publisher.Add("DEV", Sample("P1", 1));
        publisher.Add("DEV", Sample("P1", 2));
        publisher.Add("DEV", Sample("P2", 3));
        publisher.TryFlushDue();

        batches.Should().BeEmpty("间隔未到不应发布");

        _timeProvider.Advance(TimeSpan.FromMilliseconds(600));
        publisher.TryFlushDue();

        batches.Should().HaveCount(1, "到点后应发布一批");
        batches[0].Samples.Should().HaveCount(2, "同点多次更新应聚合为一条");
        batches[0].Samples.Should().ContainSingle(sample => sample.PointCode == "P1" && sample.Value == 2, "同点应取窗口内最新值");
        batches[0].Samples.Should().ContainSingle(sample => sample.PointCode == "P2" && sample.Value == 3, "不同点应各自保留");
    }

    [Fact]
    public void TryFlushDue_AfterPublish_ResetsThrottleWindow()
    {
        var publisher = CreatePublisher(intervalMs: 500);
        var count = 0;
        _eventAggregator.GetEvent<PointValuesChangedEvent>().Subscribe(_ => count++);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(600));
        publisher.Add("DEV", Sample("P1", 1));
        publisher.TryFlushDue();
        publisher.Add("DEV", Sample("P1", 2));
        publisher.TryFlushDue();

        count.Should().Be(1, "发布后间隔窗口应重置，窗口内再刷不发");

        _timeProvider.Advance(TimeSpan.FromMilliseconds(600));
        publisher.TryFlushDue();

        count.Should().Be(2, "窗口过后应再次发布");
    }

    [Fact]
    public void Flush_ForcesImmediatePublishIgnoringInterval()
    {
        var publisher = CreatePublisher(intervalMs: 5000);
        var batches = new List<PointValuesBatchPayload>();
        _eventAggregator.GetEvent<PointValuesChangedEvent>().Subscribe(batches.Add);

        publisher.Add("DEV", Sample("P1", 1));
        publisher.Flush();

        batches.Should().HaveCount(1, "Flush 应无视间隔立即发布");
    }

    [Fact]
    public void TryFlushDue_MultipleDevices_PublishesSeparateBatches()
    {
        var publisher = CreatePublisher(intervalMs: 100);
        var batches = new List<PointValuesBatchPayload>();
        _eventAggregator.GetEvent<PointValuesChangedEvent>().Subscribe(batches.Add);

        publisher.Add("DEV-A", Sample("P1", 1));
        publisher.Add("DEV-B", Sample("P2", 2));
        _timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        publisher.TryFlushDue();

        batches.Should().HaveCount(2, "不同设备应各自成批发布");
        batches.Select(batch => batch.DeviceCode).Should().BeEquivalentTo(new[] { "DEV-A", "DEV-B" }, "批次应按设备区分");
    }

    [Fact]
    public void TryFlushDue_EmptyPending_IsNoOp()
    {
        var publisher = CreatePublisher(intervalMs: 10);
        var count = 0;
        _eventAggregator.GetEvent<PointValuesChangedEvent>().Subscribe(_ => count++);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        publisher.TryFlushDue();

        count.Should().Be(0, "无待发布样本时不发布空批次");
    }

    [Fact]
    public void Configure_InvalidInterval_Throws()
    {
        var act = () => CreatePublisher().Configure(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>("发布间隔必须为正数");
    }
}
