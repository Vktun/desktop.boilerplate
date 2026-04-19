using SqlSugar;
using System;
using Vk.Dbp.Contracts.Events;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 告警记录表
    /// </summary>
    [SugarTable("AlarmRecords")]
    public class AlarmRecord
    {
        /// <summary>
        /// 告警记录ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 告警代码（唯一标识）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string AlarmCode { get; set; } = string.Empty;

        /// <summary>
        /// 告警标题（简短描述）
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = false)]
        public string AlarmTitle { get; set; } = string.Empty;

        /// <summary>
        /// 告警详细内容
        /// </summary>
        [SugarColumn(Length = 2000, IsNullable = true)]
        public string? AlarmContent { get; set; }

        /// <summary>
        /// 告警来源（设备ID、模块名等）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string? AlarmSource { get; set; }

        /// <summary>
        /// 告警等级（0=Info, 1=Warning, 2=Critical）
        /// </summary>
        public AlarmLevel AlarmLevel { get; set; } = AlarmLevel.Info;

        /// <summary>
        /// 告警状态（0=Active, 1=Acknowledged, 2=Resolved, 3=Ignored）
        /// </summary>
        public AlarmStatus AlarmStatus { get; set; } = AlarmStatus.Active;

        /// <summary>
        /// 告警类型（0=Threshold, 1=Device, 2=Process, 3=System, 4=Safety）
        /// </summary>
        public AlarmType AlarmType { get; set; } = AlarmType.System;

        /// <summary>
        /// 触发时间（告警发生时刻）
        /// </summary>
        public DateTime TriggeredTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 确认时间（用户确认时刻）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? AcknowledgedTime { get; set; }

        /// <summary>
        /// 解决时间（告警消除时刻）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ResolvedTime { get; set; }

        /// <summary>
        /// 阈值设定值（用于阈值告警）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public decimal? ThresholdValue { get; set; }

        /// <summary>
        /// 实际测量值（触发告警时的实际值）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public decimal? ActualValue { get; set; }

        /// <summary>
        /// 数值单位
        /// </summary>
        [SugarColumn(Length = 20, IsNullable = true)]
        public string? Unit { get; set; }

        /// <summary>
        /// 确认人ID
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? AcknowledgedBy { get; set; }

        /// <summary>
        /// 解决人ID
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? ResolvedBy { get; set; }

        /// <summary>
        /// 关联用户ID（用于权限过滤）
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}