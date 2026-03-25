using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vk.Dbp.Account.Permissions;

namespace Vk.Dbp.Account.Users
{
    /// <summary>
    /// 单例用户，CS客户端只允许同时有一个用户
    /// T:用户类
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IDictionary<string, object> _principalAccessor;
        public CurrentUser(IDictionary<string, object> principalAccessor)
        {
            _principalAccessor = principalAccessor;
        }
        public virtual bool IsAuthenticated => Id.HasValue;

        public virtual Guid? Id => FindGuid("Id");

        public virtual string? UserName => (_principalAccessor[nameof(UserName)] ?? string.Empty).ToString();

        public virtual string? Name => (_principalAccessor[nameof(Name)] ?? string.Empty).ToString();

        public virtual string? SurName => (_principalAccessor[nameof(SurName)] ?? string.Empty).ToString();

        public virtual string? PhoneNumber => (_principalAccessor[nameof(PhoneNumber)] ?? string.Empty).ToString();
        /// <summary>
        /// 角色列表
        /// </summary>
        public virtual List<RoleDto> Roles => _principalAccessor["Roles"] == null ? new List<RoleDto> { } : (List<RoleDto>)_principalAccessor["Roles"];
        /// <summary>
        /// 权限列表
        /// </summary>
        public virtual List<PermissionDto> permissions => _principalAccessor["permissions"] == null ? new List<PermissionDto>() { } : (List<PermissionDto>)_principalAccessor["permissions"];
        public virtual bool IsInRole(string roleName)
        {
            var roles = _principalAccessor["Roles"];
            if (roles == null)
            {
                return false;
            }
            else
            {
                var rs = (List<RoleDto>)roles;
                return rs.Any(r => r.Name == roleName);
            }
        }
        Guid? FindGuid(string name)
        {
            Guid _id;
            var res = Guid.TryParse((_principalAccessor[name]).ToString(), out _id);
            if (res)
            {
                return _id;
            }
            else
            {
                return null;
            }

        }
    }
}
