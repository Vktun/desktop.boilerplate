using Vk.Dbp.Contracts.Industrial;

namespace Vk.Dbp.DeviceModule.Models;

/// <summary>
/// 点位实时值快照（不可变；实时仓/事件/历史缓冲的统一载体）
/// </summary>
public sealed record PointValueSnapshot
{
    /// <summary>
    /// 点位ID
    /// </summary>
    public int PointId { get; init; }

    /// <summary>
    /// 点位编码
    /// </summary>
    public required string PointCode { get; init; }

    /// <summary>
    /// 所属设备编码
    /// </summary>
    public required string DeviceCode { get; init; }

    /// <summary>
    /// 数值采样值（非数值点位为 null）
    /// </summary>
    public double? Value { get; init; }

    /// <summary>
    /// 文本采样值（Boolean"开/关"、String 原文、Bad 质量的错误描述）
    /// </summary>
    public string? ValueText { get; init; }

    /// <summary>
    /// 数据质量
    /// </summary>
    public DataQuality Quality { get; init; } = DataQuality.Good;

    /// <summary>
    /// 采样时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// 驱动读点请求
/// </summary>
public sealed record PointReadRequest
{
    /// <summary>
    /// 点位ID
    /// </summary>
    public int PointId { get; init; }

    /// <summary>
    /// 点位编码
    /// </summary>
    public required string PointCode { get; init; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public PointDataType DataType { get; init; } = PointDataType.Double;

    /// <summary>
    /// 驱动地址（协议语义）
    /// </summary>
    public required string Address { get; init; }
}

/// <summary>
/// 驱动写点请求（命令回写）
/// </summary>
public sealed record PointWriteRequest
{
    /// <summary>
    /// 点位编码
    /// </summary>
    public required string PointCode { get; init; }

    /// <summary>
    /// 驱动地址（协议语义）
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public PointDataType DataType { get; init; } = PointDataType.Double;

    /// <summary>
    /// 写入值（字符串形式，由驱动按数据类型解析）
    /// </summary>
    public required string Value { get; init; }
}

/// <summary>
/// 设备连接信息（驱动工厂建驱动的输入）
/// </summary>
public sealed record DeviceConnectionInfo
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public int DeviceId { get; init; }

    /// <summary>
    /// 设备编码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 协议类型（见 ProtocolTypes 常量）
    /// </summary>
    public required string ProtocolType { get; init; }

    /// <summary>
    /// 连接配置 JSON（协议相关）
    /// </summary>
    public string? ConnectionConfig { get; init; }
}

/// <summary>
/// 设备目录摘要（监控页设备列表数据源）
/// </summary>
public sealed record DeviceSummary
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 设备编码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 协议类型
    /// </summary>
    public required string ProtocolType { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }
}

/// <summary>
/// 点位定义（监控页点位表格与引擎采集的数据源）
/// </summary>
public sealed record PointDefinition
{
    /// <summary>
    /// 点位ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 所属设备ID
    /// </summary>
    public int DeviceId { get; init; }

    /// <summary>
    /// 点位编码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 点位名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public PointDataType DataType { get; init; } = PointDataType.Double;

    /// <summary>
    /// 工程单位
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// 驱动地址
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// 高限告警阈值（null=不检测）
    /// </summary>
    public decimal? AlarmHigh { get; init; }

    /// <summary>
    /// 低限告警阈值（null=不检测）
    /// </summary>
    public decimal? AlarmLow { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }
}

/// <summary>
/// 命令定义（预定义的点位回写）
/// </summary>
public sealed record CommandDefinition
{
    /// <summary>
    /// 命令ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 所属设备ID
    /// </summary>
    public int DeviceId { get; init; }

    /// <summary>
    /// 命令编码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 命令名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 目标点位编码
    /// </summary>
    public required string TargetPointCode { get; init; }

    /// <summary>
    /// 写入值（字符串形式）
    /// </summary>
    public string? WriteValue { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; }
}

/// <summary>
/// 历史趋势点（历史查询返回值）
/// </summary>
public sealed record HistoryPoint
{
    /// <summary>
    /// 采样时间戳
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// 数值采样值
    /// </summary>
    public double? Value { get; init; }
}
