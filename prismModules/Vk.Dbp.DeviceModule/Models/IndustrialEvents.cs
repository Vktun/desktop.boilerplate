using Prism.Events;
using Vk.Dbp.Contracts.Industrial;

namespace Vk.Dbp.DeviceModule.Models;

/// <summary>
/// 点位值批量变化事件载荷（引擎节流后按设备聚合发布，控制 UI 刷新频率）
/// </summary>
public sealed class PointValuesBatchPayload
{
    /// <summary>
    /// 设备编码
    /// </summary>
    public required string DeviceCode { get; init; }

    /// <summary>
    /// 批次时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// 本批次的最新快照（每点位一条，取窗口内最后一次值）
    /// </summary>
    public required IReadOnlyList<PointValueSnapshot> Samples { get; init; }
}

/// <summary>
/// 点位值批量变化事件（监控页等 UI 消费；发布频率由引擎节流器控制）
/// </summary>
public sealed class PointValuesChangedEvent : PubSubEvent<PointValuesBatchPayload>
{
}

/// <summary>
/// 设备状态变化事件载荷
/// </summary>
public sealed class DeviceStatusPayload
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public int DeviceId { get; init; }

    /// <summary>
    /// 设备编码
    /// </summary>
    public required string DeviceCode { get; init; }

    /// <summary>
    /// 变化前状态
    /// </summary>
    public DeviceRuntimeStatus OldStatus { get; init; }

    /// <summary>
    /// 变化后状态
    /// </summary>
    public DeviceRuntimeStatus NewStatus { get; init; }

    /// <summary>
    /// 附加信息（如故障原因）
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 变化时间
    /// </summary>
    public DateTime ChangedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// 设备状态变化事件（低频，状态转换时发布）
/// </summary>
public sealed class DeviceStatusChangedEvent : PubSubEvent<DeviceStatusPayload>
{
}
