using System;
using Prism.Events;

namespace Vk.Dbp.Contracts.Events
{
    /// <summary>
    /// 通知类型枚举
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// 系统通知
        /// </summary>
        System = 0,

        /// <summary>
        /// 信息通知
        /// </summary>
        Info = 1,

        /// <summary>
        /// 警告通知
        /// </summary>
        Warning = 2,

        /// <summary>
        /// 成功通知
        /// </summary>
        Success = 3,

        /// <summary>
        /// 错误通知
        /// </summary>
        Error = 4
    }

    /// <summary>
    /// 通知优先级枚举
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// 低优先级
        /// </summary>
        Low = 0,

        /// <summary>
        /// 普通优先级
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 高优先级
        /// </summary>
        High = 2,

        /// <summary>
        /// 关键优先级
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// 全局通知事件Payload
    /// </summary>
    public class GlobalNotificationPayload
    {
        /// <summary>
        /// 通知唯一标识（持久化后由服务生成）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 通知标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 通知内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 通知类型
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.Info;

        /// <summary>
        /// 目标用户ID（null表示广播给所有用户）
        /// </summary>
        public int? TargetUserId { get; set; }

        /// <summary>
        /// 来源模块标识
        /// </summary>
        public string? SourceModule { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 通知优先级
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>
        /// 可选的点击跳转链接（用于导航到具体页面）
        /// </summary>
        public string? ActionUrl { get; set; }

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead { get; set; } = false;
    }

    /// <summary>
    /// 全局通知事件 - 用于跨模块/后台线程发送通知
    /// </summary>
    public class GlobalNotificationEvent : PubSubEvent<GlobalNotificationPayload>
    {
    }

    /// <summary>
    /// 通知数量变更事件Payload
    /// </summary>
    public class NotificationCountChangedPayload
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 未读通知数量
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 通知数量变更事件 - 用于更新通知Badge计数
    /// </summary>
    public class NotificationCountChangedEvent : PubSubEvent<NotificationCountChangedPayload>
    {
    }
}