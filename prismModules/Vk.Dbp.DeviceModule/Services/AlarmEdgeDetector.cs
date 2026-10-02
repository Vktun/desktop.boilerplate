using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 阈值告警边沿转换（检出即触发一次，内部值对象）
/// </summary>
/// <param name="PointId">点位ID</param>
/// <param name="PointCode">点位编码</param>
/// <param name="IsHigh">是否高限（false=低限）</param>
/// <param name="Threshold">触发阈值</param>
/// <param name="ActualValue">触发时实测值</param>
/// <param name="Timestamp">触发时间</param>
internal sealed record AlarmTransition(
    int PointId,
    string PointCode,
    bool IsHigh,
    decimal Threshold,
    decimal ActualValue,
    DateTime Timestamp);

/// <summary>
/// 阈值告警边沿检测状态机（每设备 worker 私有实例，无并发问题）。
/// 仅上沿触发（None→High / None→Low 建告警）；回落只复位状态，不建新告警也不自动解决——
/// 告警生命周期（确认/解决）保留人工操作语义。同点配双阈时高限优先。
/// 无滞环是阶段一取舍：阈值附近可能反复触发，配置时把阈值设在波动带外缓解。
/// </summary>
internal sealed class AlarmEdgeDetector
{
    private enum EdgeState
    {
        None,
        HighActive,
        LowActive
    }

    private readonly Dictionary<int, EdgeState> _states = new();

    /// <summary>
    /// 评估一次采样；命中上沿时返回转换描述，否则返回 null
    /// </summary>
    public AlarmTransition? Evaluate(PointDefinition point, PointValueSnapshot snapshot)
    {
        if (snapshot.Quality != DataQuality.Good || snapshot.Value is not { } value)
        {
            return null;
        }

        var state = _states.TryGetValue(point.Id, out var current) ? current : EdgeState.None;
        var decimalValue = (decimal)value;

        if (point.AlarmHigh is { } high)
        {
            if (state == EdgeState.None && decimalValue > high)
            {
                _states[point.Id] = EdgeState.HighActive;
                return new AlarmTransition(point.Id, point.Code, IsHigh: true, high, decimalValue, snapshot.Timestamp);
            }

            if (state == EdgeState.HighActive && decimalValue <= high)
            {
                // 恢复：只复位状态，不自动解决既有告警（人工生命周期）
                _states[point.Id] = EdgeState.None;
                return null;
            }
        }

        if (point.AlarmLow is { } low)
        {
            if (state == EdgeState.None && decimalValue < low)
            {
                _states[point.Id] = EdgeState.LowActive;
                return new AlarmTransition(point.Id, point.Code, IsHigh: false, low, decimalValue, snapshot.Timestamp);
            }

            if (state == EdgeState.LowActive && decimalValue >= low)
            {
                _states[point.Id] = EdgeState.None;
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 设备轮询失败的重连退避：5s 起步指数翻倍（5/10/20/40/80…），60s 封顶
    /// </summary>
    public static TimeSpan CalculateRetryBackoff(int consecutiveFailures)
    {
        if (consecutiveFailures <= 1)
        {
            return TimeSpan.FromSeconds(5);
        }

        var exponent = Math.Min(consecutiveFailures - 1, 4);
        var seconds = Math.Min(5.0 * Math.Pow(2, exponent), 60);
        return TimeSpan.FromSeconds(seconds);
    }
}
