using System;
using System.Collections.Generic;

namespace Vk.Dbp.AccountModule.Models
{
    /// <summary>
    /// 角色模型
    /// </summary>
    public class Role
    {
        public int Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 角色描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModifiedTime { get; set; }

        /// <summary>
        /// 权限ID列表
        /// </summary>
        public List<int> PermissionIds { get; set; } = new List<int>();

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
    }
}