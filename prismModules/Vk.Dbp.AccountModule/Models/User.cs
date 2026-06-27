using System;
using System.Collections.Generic;

namespace Vk.Dbp.AccountModule.Models
{
    public class User
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? RealName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? PasswordHash { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedTime { get; set; } = DateTime.Now;

        public DateTime? LastModifiedTime { get; set; }

        public DateTime? LastLoginTime { get; set; }

        public int FailedLoginCount { get; set; }

        public DateTime? LockoutEndTime { get; set; }

        public bool IsLockedOut => LockoutEndTime is { } lockoutEndTime && lockoutEndTime > DateTime.Now;

        public List<int> RoleIds { get; set; } = new();

        public string? Remarks { get; set; }
    }
}
