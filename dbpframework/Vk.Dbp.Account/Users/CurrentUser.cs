using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual string? UserName => GetPrincipalValue(nameof(UserName));

        public virtual string? Name => GetPrincipalValue(nameof(Name));

        public virtual string? SurName => GetPrincipalValue(nameof(SurName));

        public virtual string? PhoneNumber => GetPrincipalValue(nameof(PhoneNumber));

        /// <summary>
        /// 角色列表
        /// </summary>
        public virtual List<RoleDto> Roles => _principalAccessor.TryGetValue("Roles", out var roles) && roles is List<RoleDto> roleList
            ? roleList
            : new List<RoleDto>();

        /// <summary>
        /// 权限列表
        /// </summary>
        public virtual List<PermissionDto> permissions => _principalAccessor.TryGetValue("permissions", out var permissionValues) && permissionValues is List<PermissionDto> permissionList
            ? permissionList
            : new List<PermissionDto>();

        public virtual bool IsInRole(string roleName)
        {
            return _principalAccessor.TryGetValue("Roles", out var roles) &&
                   roles is List<RoleDto> roleList &&
                   roleList.Any(r => r.Name == roleName);
        }

        private Guid? FindGuid(string name)
        {
            return _principalAccessor.TryGetValue(name, out var value) && Guid.TryParse(value?.ToString(), out Guid id)
                ? id
                : null;
        }

        private string? GetPrincipalValue(string name)
        {
            return _principalAccessor.TryGetValue(name, out var value) ? value?.ToString() : string.Empty;
        }
    }
}
