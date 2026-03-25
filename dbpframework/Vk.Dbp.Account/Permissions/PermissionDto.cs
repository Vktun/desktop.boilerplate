using System;
using System.Collections.Generic;
using System.Text;

namespace Vk.Dbp.Account.Permissions
{
    /// <summary>
    /// 当前权限
    /// </summary>
    public class PermissionDto
    {
        /// <summary>
        /// 当前权限Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 当前权限名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 当前权限的子权限
        /// </summary>
        public List<PermissionDto> Children { get; set; }
    }
}
