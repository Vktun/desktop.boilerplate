using System;
using System.Collections.Generic;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    public interface IUserSession
    {
        int UserId { get; }
        string Username { get; }
        string RealName { get; }
        string Email { get; }
        string Phone { get; }
        bool IsLoggedIn { get; }
        DateTime LoginTime { get; }
        string Token { get; }
        List<string> Permissions { get; }

        void Login(User user, string token = "");
        void Logout();
        void SetPermissions(List<string> permissions);
        bool HasPermission(string permissionCode);
    }
}
