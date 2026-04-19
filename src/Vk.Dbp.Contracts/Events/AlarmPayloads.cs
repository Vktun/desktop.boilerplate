using System;

namespace Vk.Dbp.Contracts.Events
{
    /// <summary>
    /// 告警触发事件Payload
    /// </summary>
    public class AlarmTriggeredPayload
    {
        /// <summary>
        /// 告警记录ID
        /// </summary>
        public int AlarmRecordId { get; set; }

        /// <summary>
        /// 告警代码
        /// </summary>
        public string AlarmCode { get; set; } = string.Empty;

        /// <summary>
        /// 告警等级
        /// </summary>
        public AlarmLevel Level { get; set; }

        /// <summary>
        /// 告警标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 告警内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime TriggeredTime { get; set; }

        /// <summary>
        /// 告警来源
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// 关联用户ID
        /// </summary>
        public int UserId { get; set; }
    }

    /// <summary>
    /// 告警状态变更Payload
    /// </summary>
    public class AlarmStatusChangedPayload
    {
        /// <summary>
        /// 告警记录ID
        /// </summary>
        public int AlarmRecordId { get; set; }

        /// <summary>
        /// 旧状态
        /// </summary>
        public AlarmStatus OldStatus { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public AlarmStatus NewStatus { get; set; }

        /// <summary>
        /// 变更用户ID
        /// </summary>
        public int ChangedBy { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedTime { get; set; }
    }

    /// <summary>
    /// 告警数量变更Payload
    /// </summary>
    public class AlarmCountChangedPayload
    {
        /// <summary>
        /// 活跃告警数量
        /// </summary>
        public int ActiveCount { get; set; }

        /// <summary>
        /// 严重告警数量
        /// </summary>
        public int CriticalCount { get; set; }

        /// <summary>
        /// 是否有严重告警
        /// </summary>
        public bool HasCriticalAlarm { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
    }
}