using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;
using Vk.Dbp.Account.Permissions;

namespace Vk.Dbp.Account.Users
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }

        [CanBeNull]
        Guid? Id { get; }

        string? UserName { get; }

        string? Name { get; }

        string? SurName { get; }

        string? PhoneNumber { get; }


        [NotNull]
        List<RoleDto> Roles { get; }

        bool IsInRole(string roleName);

        List<PermissionDto> permissions { get; }
    }
}
