using System;
using System.Collections.Generic;

namespace Vk.Dbp.AccountModule.Models
{
    /// <summary>
    /// 用户模型
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 电话
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 密码哈希
        /// </summary>
        public string PasswordHash { get; set; }

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
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// 所属角色ID列表
        /// </summary>
        public List<int> RoleIds { get; set; } = new();

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; }
    }
}