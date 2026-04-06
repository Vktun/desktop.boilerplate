using System;

namespace Vk.Dbp.Contracts.Events
{
    /// <summary>
    /// 用户登录事件 - 当用户成功登录时发布
    /// </summary>
    public class UserLoggedInEvent
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime { get; set; }
        
        /// <summary>
        /// 登录IP地址（可选）
        /// </summary>
        public string? IpAddress { get; set; }
    }
}