using FluentAssertions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class AlarmEdgeDetectorTests
{
    private readonly AlarmEdgeDetector _detector = new();

    private static PointDefinition Point(decimal? high = null, decimal? low = null, int id = 1)
    {
        return new PointDefinition
        {
            Id = id,
            DeviceId = 1,
            Code = $"P{id}",
            Name = $"点位{id}",
            DataType = PointDataType.Double,
            Address = "const(0)",
            AlarmHigh = high,
            AlarmLow = low,
            IsEnabled = true
        };
    }

    private static PointValueSnapshot Sample(double value, DataQuality quality = DataQuality.Good)
    {
        return new PointValueSnapshot
        {
            PointId = 1,
            PointCode = "P1",
            DeviceCode = "DEV",
            Value = value,
            Quality = quality,
            Timestamp = DateTime.Now
        };
    }

    [Fact]
    public void Evaluate_CrossingHighLimit_TriggersOnce()
    {
        var point = Point(high: 75m);

        var first = _detector.Evaluate(point, Sample(80));
        var second = _detector.Evaluate(point, Sample(85));

        first.Should().NotBeNull("首次越过高限应触发告警");
        first!.IsHigh.Should().BeTrue("高限触发应标记为 IsHigh");
        first.Threshold.Should().Be(75m, "阈值应为高限配置值");
        second.Should().BeNull("持续超限不应重复触发");
    }

    [Fact]
    public void Evaluate_RecoveryThenReCross_TriggersAgain()
    {
        var point = Point(high: 75m);

        _detector.Evaluate(point, Sample(80));
        var recovered = _detector.Evaluate(point, Sample(60));
        var reCrossed = _detector.Evaluate(point, Sample(90));

        recovered.Should().BeNull("回落到阈值内只复位状态，不产生转换");
        reCrossed.Should().NotBeNull("回落后再越限应再次触发");
    }

    [Fact]
    public void Evaluate_LowLimit_MirrorsHighBehavior()
    {
        var point = Point(low: 0.5m);

        var triggered = _detector.Evaluate(point, Sample(0.3));
        var sustained = _detector.Evaluate(point, Sample(0.2));
        var recovered = _detector.Evaluate(point, Sample(0.6));
        var reTriggered = _detector.Evaluate(point, Sample(0.4));

        triggered.Should().NotBeNull("跌破低限应触发告警");
        triggered!.IsHigh.Should().BeFalse("低限触发应标记为非高限");
        triggered.Threshold.Should().Be(0.5m, "阈值应为低限配置值");
        sustained.Should().BeNull("持续低于低限不应重复触发");
        recovered.Should().BeNull("回升只复位状态");
        reTriggered.Should().NotBeNull("回升后再跌破应再次触发");
    }

    [Fact]
    public void Evaluate_NoThresholds_NeverTriggers()
    {
        var point = Point();

        var result = _detector.Evaluate(point, Sample(1000));

        result.Should().BeNull("未配置阈值的点位不参与评估");
    }

    [Theory]
    [InlineData(DataQuality.Bad)]
    [InlineData(DataQuality.Uncertain)]
    public void Evaluate_NonGoodQuality_SkipsEvaluation(DataQuality quality)
    {
        var point = Point(high: 75m);

        var result = _detector.Evaluate(point, Sample(1000, quality));

        result.Should().BeNull("非 Good 质量样本不参与阈值评估");
    }

    [Fact]
    public void Evaluate_ValueExactlyAtThreshold_DoesNotTrigger()
    {
        var point = Point(high: 75m);

        var result = _detector.Evaluate(point, Sample(75));

        result.Should().BeNull("恰好等于高限不应触发（严格大于才触发）");
    }

    [Fact]
    public void CalculateRetryBackoff_DoublesAndCapsAtSixtySeconds()
    {
        AlarmEdgeDetector.CalculateRetryBackoff(0).Should().Be(TimeSpan.FromSeconds(5), "首次失败退避 5 秒");
        AlarmEdgeDetector.CalculateRetryBackoff(1).Should().Be(TimeSpan.FromSeconds(5), "首次失败退避 5 秒");
        AlarmEdgeDetector.CalculateRetryBackoff(2).Should().Be(TimeSpan.FromSeconds(10), "第二次失败翻倍");
        AlarmEdgeDetector.CalculateRetryBackoff(3).Should().Be(TimeSpan.FromSeconds(20), "第三次失败再翻倍");
        AlarmEdgeDetector.CalculateRetryBackoff(4).Should().Be(TimeSpan.FromSeconds(40), "第四次失败再翻倍");
        AlarmEdgeDetector.CalculateRetryBackoff(5).Should().Be(TimeSpan.FromSeconds(60), "超过封顶后保持 60 秒");
        AlarmEdgeDetector.CalculateRetryBackoff(100).Should().Be(TimeSpan.FromSeconds(60), "极端失败次数仍封顶 60 秒");
    }
}
