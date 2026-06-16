using SqlSugar;
using System;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 告警配置表
    /// </summary>
    [SugarTable("AlarmConfigs")]
    public class AlarmConfig
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 告警代码（关联 AlarmRecord.AlarmCode）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string AlarmCode { get; set; } = string.Empty;

        /// <summary>
        /// 配置名称
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = false)]
        public string AlarmName { get; set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 最小阈值
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public decimal? ThresholdMin { get; set; }

        /// <summary>
        /// 最大阈值
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public decimal? ThresholdMax { get; set; }

        /// <summary>
        /// 阈值单位
        /// </summary>
        [SugarColumn(Length = 20, IsNullable = true)]
        public string? ThresholdUnit { get; set; }

        /// <summary>
        /// 比较类型（GreaterThan, LessThan, Range, Equal等）
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string? ComparisonType { get; set; }

        /// <summary>
        /// 是否启用弹窗通知
        /// </summary>
        public bool EnablePopup { get; set; } = true;

        /// <summary>
        /// 是否启用声音提示
        /// </summary>
        public bool EnableSound { get; set; } = false;

        /// <summary>
        /// 声音文件路径
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? SoundFilePath { get; set; }

        /// <summary>
        /// 是否自动确认
        /// </summary>
        public bool AutoAcknowledge { get; set; } = false;

        /// <summary>
        /// 确认超时时间（分钟）
        /// </summary>
        public int AcknowledgeTimeout { get; set; } = 30;

        /// <summary>
        /// 显示颜色（对应 AlarmLevel）
        /// </summary>
        [SugarColumn(Length = 20, IsNullable = true)]
        public string? DisplayColor { get; set; }

        /// <summary>
        /// 优先级（用于排序）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 是否启用此配置
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 比较类型常量
    /// </summary>
    public static class ComparisonTypes
    {
        /// <summary>
        /// 大于
        /// </summary>
        public const string GreaterThan = "GreaterThan";

        /// <summary>
        /// 小于
        /// </summary>
        public const string LessThan = "LessThan";

        /// <summary>
        /// 范围内（Min <= Value <= Max）
        /// </summary>
        public const string InRange = "InRange";

        /// <summary>
        /// 范围外（Value < Min 或 Value > Max）
        /// </summary>
        public const string OutOfRange = "OutOfRange";

        /// <summary>
        /// 等于
        /// </summary>
        public const string Equal = "Equal";

        /// <summary>
        /// 不等于
        /// </summary>
        public const string NotEqual = "NotEqual";
    }
}
