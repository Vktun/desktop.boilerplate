using System;
using System.Collections.Generic;

namespace Vk.Dbp.Contracts.Events
{
    /// <summary>
    /// 权限变更事件 - 当用户的权限发生变化时发布
    /// </summary>
    public class PermissionChangedEvent
    {
        /// <summary>
        /// 受影响的用户ID
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// 变更类型
        /// </summary>
        public PermissionChangeType ChangeType { get; set; }
        
        /// <summary>
        /// 新增的权限代码列表
        /// </summary>
        public List<string> AddedPermissions { get; set; } = new();
        
        /// <summary>
        /// 移除的权限代码列表
        /// </summary>
        public List<string> RemovedPermissions { get; set; } = new();
        
        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedTime { get; set; }
    }
    
    /// <summary>
    /// 权限变更类型
    /// </summary>
    public enum PermissionChangeType
    {
        /// <summary>
        /// 权限授予
        /// </summary>
        Granted,
        
        /// <summary>
        /// 权限撤销
        /// </summary>
        Revoked,
        
        /// <summary>
        /// 权限更新
        /// </summary>
        Updated
    }
}