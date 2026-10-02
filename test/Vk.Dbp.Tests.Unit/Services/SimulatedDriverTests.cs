using FluentAssertions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class SimulatedDriverTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Start);
    private readonly SimulatedDriver _driver;

    public SimulatedDriverTests()
    {
        _driver = new SimulatedDriver(_timeProvider, "SIM-TEST");
    }

    private async Task<PointValueSnapshot> ReadAsync(string address, PointDataType dataType = PointDataType.Double, int pointId = 1)
    {
        var snapshots = await _driver.ReadPointsAsync(
            new[] { new PointReadRequest { PointId = pointId, PointCode = $"P{pointId}", DataType = dataType, Address = address } },
            CancellationToken.None);
        return snapshots[0];
    }

    [Fact]
    public async Task ReadPointsAsync_SineAddress_ValueStaysWithinClosedRange()
    {
        var values = new List<double>();
        for (var i = 0; i < 12; i++)
        {
            var snapshot = await ReadAsync("sine(20,80,60)", pointId: 1);
            values.Add(snapshot.Value!.Value);
            _timeProvider.Advance(TimeSpan.FromSeconds(5));
        }

        values.Should().OnlyContain(value => value >= 20 && value <= 80, "sine 波形值域应为闭区间 [min,max]");
    }

    [Fact]
    public async Task ReadPointsAsync_SameTimestamp_ReturnsIdenticalValue()
    {
        var first = await ReadAsync("sine(20,80,60)");
        var second = await ReadAsync("sine(20,80,60)");

        second.Value.Should().Be(first.Value, "同一时刻两次读取应得到相同值（确定性求值）");
    }

    [Fact]
    public async Task ReadPointsAsync_RandAddress_StableWithinSecondAndInsideRange()
    {
        var first = await ReadAsync("rand(0.4,1.2)", pointId: 7);
        var second = await ReadAsync("rand(0.4,1.2)", pointId: 7);

        second.Value.Should().Be(first.Value, "同一整秒内 rand 应保持稳定");

        var values = new List<double> { first.Value!.Value };
        _timeProvider.Advance(TimeSpan.FromSeconds(2));
        values.Add((await ReadAsync("rand(0.4,1.2)", pointId: 7)).Value!.Value);
        _timeProvider.Advance(TimeSpan.FromSeconds(2));
        values.Add((await ReadAsync("rand(0.4,1.2)", pointId: 7)).Value!.Value);

        values.Should().OnlyContain(value => value >= 0.4 && value < 1.2, "rand 值域应为 [min,max)");
    }

    [Fact]
    public async Task ReadPointsAsync_RampAddress_PeriodicAndInsideRange()
    {
        var first = await ReadAsync("ramp(0,1500,120)");
        _timeProvider.Advance(TimeSpan.FromSeconds(120));
        var second = await ReadAsync("ramp(0,1500,120)");

        second.Value.Should().Be(first.Value, "ramp 相差整周期时值应折返相同");
        first.Value.Should().BeInRange(0, 1500, "ramp 值应在 [min,max] 内");
    }

    [Fact]
    public async Task ReadPointsAsync_PulseAddress_SquareWaveToggles()
    {
        var observed = new HashSet<double>();
        for (var i = 0; i < 24; i++)
        {
            var snapshot = await ReadAsync("pulse(30)");
            observed.Add(snapshot.Value!.Value);
            _timeProvider.Advance(TimeSpan.FromSeconds(2));
        }

        observed.Should().BeEquivalentTo(new[] { 0.0, 1.0 }, "脉冲波形应在完整周期内同时取到 0 和 1");
    }

    [Fact]
    public async Task ReadPointsAsync_ConstAddress_ValueNeverChanges()
    {
        var first = await ReadAsync("const(62.5)");
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        var second = await ReadAsync("const(62.5)");

        second.Value.Should().Be(62.5, "const 波形应恒定返回设定值");
        first.Value.Should().Be(62.5, "const 波形应恒定返回设定值");
    }

    [Theory]
    [InlineData("SINE( 20 , 80 , 60 )")]
    [InlineData(" const(42) ")]
    public async Task ReadPointsAsync_MixedCaseAndWhitespace_ParsesSuccessfully(string address)
    {
        var snapshot = await ReadAsync(address);

        snapshot.Quality.Should().Be(DataQuality.Good, "地址语法应容忍大小写与空白");
    }

    [Theory]
    [InlineData("sine(20,80)", "参数个数")]
    [InlineData("foo(1)", "未知波形")]
    [InlineData("not-a-grammar", "格式")]
    public async Task ReadPointsAsync_InvalidAddress_ReturnsBadQualityWithMessage(string address, string expectedFragment)
    {
        var snapshot = await ReadAsync(address);

        snapshot.Quality.Should().Be(DataQuality.Bad, "非法地址应返回 Bad 质量而不是抛异常");
        snapshot.ValueText.Should().Contain(expectedFragment, "错误描述应包含解析失败原因");
        snapshot.Value.Should().BeNull("Bad 质量样本不应携带数值");
    }

    [Fact]
    public async Task ReadPointsAsync_BooleanPoint_TextShowsOnOff()
    {
        var on = await ReadAsync("const(1)", PointDataType.Boolean);
        var off = await ReadAsync("const(0)", PointDataType.Boolean);

        on.ValueText.Should().Be("开", "布尔点非零应显示为开");
        on.Value.Should().Be(1, "布尔点非零应归一为 1");
        off.ValueText.Should().Be("关", "布尔点零应显示为关");
    }

    [Fact]
    public async Task ReadPointsAsync_Int32Point_RoundsToInteger()
    {
        var snapshot = await ReadAsync("const(62.6)", PointDataType.Int32);

        snapshot.Value.Should().Be(63, "整数点应四舍五入取整");
    }

    [Fact]
    public async Task WritePointAsync_ThenRead_ReturnsOverrideValue()
    {
        var writeRequest = new PointWriteRequest
        {
            PointCode = "P1",
            Address = "pulse(45)",
            DataType = PointDataType.Boolean,
            Value = "1"
        };
        await _driver.WritePointAsync(writeRequest, CancellationToken.None);

        var snapshot = await ReadAsync("pulse(45)", PointDataType.Boolean);
        snapshot.Value.Should().Be(1, "写入覆盖值后读取应返回覆盖值");
        snapshot.ValueText.Should().Be("开", "覆盖值 1 应按布尔语义显示为开");
        snapshot.Quality.Should().Be(DataQuality.Good, "写入成功应返回 Good 质量");
    }

    [Fact]
    public async Task WritePointAsync_InvalidValue_ReturnsBadQuality()
    {
        var writeRequest = new PointWriteRequest
        {
            PointCode = "P1",
            Address = "pulse(45)",
            DataType = PointDataType.Boolean,
            Value = "abc"
        };

        var snapshot = await _driver.WritePointAsync(writeRequest, CancellationToken.None);

        snapshot.Quality.Should().Be(DataQuality.Bad, "非法写入值应返回 Bad 质量");
        snapshot.ValueText.Should().Contain("无法解析", "错误描述应说明写入值解析失败");
    }
}
