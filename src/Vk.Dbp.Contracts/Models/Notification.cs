using System;

namespace Vk.Dbp.Contracts.Models
{
    /// <summary>
    /// 通知 DTO：shell 通知中心与 AccountModule 通知持久化共用的跨模块契约模型
    /// </summary>
    public class Notification
    {
        /// <summary>通知 ID</summary>
        public int Id { get; set; }

        /// <summary>标题</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>通知类型</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>是否已读</summary>
        public bool IsRead { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>目标用户 ID</summary>
        public int UserId { get; set; }
    }
}
