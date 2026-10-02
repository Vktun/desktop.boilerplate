namespace Vk.Dbp.DeviceModule.Models;

/// <summary>
/// 告警汇总报表行（按来源×等级聚合；日报/月报共用）
/// </summary>
public sealed record AlarmSummaryRow
{
    /// <summary>
    /// 告警来源（设备编码；null 归为"未知来源"）
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// 等级中文（信息/警告/严重）
    /// </summary>
    public required string LevelText { get; init; }

    /// <summary>
    /// 告警数量
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// 占总数百分比（0-100，一位小数）
    /// </summary>
    public double Percent { get; init; }

    /// <summary>
    /// 仍处活跃状态的数量
    /// </summary>
    public int ActiveCount { get; init; }

    /// <summary>
    /// 已解决数量
    /// </summary>
    public int ResolvedCount { get; init; }
}

/// <summary>
/// 点位历史班报行（时段内各点位统计）
/// </summary>
public sealed record ShiftReportRow
{
    /// <summary>
    /// 设备编码
    /// </summary>
    public required string DeviceCode { get; init; }

    /// <summary>
    /// 点位编码
    /// </summary>
    public required string PointCode { get; init; }

    /// <summary>
    /// 点位名称
    /// </summary>
    public required string PointName { get; init; }

    /// <summary>
    /// 工程单位
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// 最小值（无数值样本为 null）
    /// </summary>
    public double? Min { get; init; }

    /// <summary>
    /// 最大值（无数值样本为 null）
    /// </summary>
    public double? Max { get; init; }

    /// <summary>
    /// 平均值（无数值样本为 null）
    /// </summary>
    public double? Avg { get; init; }

    /// <summary>
    /// 样本数
    /// </summary>
    public int Count { get; init; }
}

/// <summary>
/// 审计日志报表行
/// </summary>
public sealed record AuditReportRow
{
    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; init; }

    /// <summary>
    /// 操作人
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 操作类型中文
    /// </summary>
    public string ActionText { get; init; } = string.Empty;

    /// <summary>
    /// 模块
    /// </summary>
    public string? Module { get; init; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 结果（成功/失败）
    /// </summary>
    public string ResultText { get; init; } = "成功";

    /// <summary>
    /// 失败原因（成功时为 null）
    /// </summary>
    public string? FailureReason { get; init; }
}
