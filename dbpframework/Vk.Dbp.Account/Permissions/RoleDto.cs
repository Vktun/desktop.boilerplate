using System;
using System.Collections.Generic;
using System.Text;

namespace Vk.Dbp.Account.Permissions
{
    public class RoleDto
    {
        public int Id { get; set;}

        public string Name { get; set; }
        public bool IsDefault { get; set; }
        /// <summary>
        /// 角色级别，默认可以不用
        /// </summary>
        public int RoleLevel { get; set; }
    }
}
