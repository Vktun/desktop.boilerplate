using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Extensions;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 用户服务实现
    /// </summary>
    public class UserService : IUserService
    {
        private readonly List<User> _users = new();
        private readonly IAuditLogService _auditLogService;
        private int _nextUserId = 1;

        public UserService(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            InitializeSampleData();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await Task.FromResult(_users.ToList());
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Username == username));
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                user.Id = _nextUserId++;
                user.CreatedTime = DateTime.Now;
                _users.Add(user);

                // 使用扩展方法记录审计日志
                await _auditLogService.LogCreateAsync(
                    1, "admin", "Account", "User", user.Id, user,
                    $"创建用户: {user.Username}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建用户失败: {user.Username}", ex.Message, "User", user.Id);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
                if (existingUser == null)
                    return false;

                var oldData = new
                {
                    existingUser.RealName,
                    existingUser.Email,
                    existingUser.Phone,
                    existingUser.Remarks
                };

                existingUser.RealName = user.RealName;
                existingUser.Email = user.Email;
                existingUser.Phone = user.Phone;
                existingUser.LastModifiedTime = DateTime.Now;
                existingUser.Remarks = user.Remarks;

                var newData = new
                {
                    existingUser.RealName,
                    existingUser.Email,
                    existingUser.Phone,
                    existingUser.Remarks
                };

                // 使用扩展方法记录审计日志
                await _auditLogService.LogUpdateAsync(
                    1, "admin", "Account", "User", user.Id, oldData, newData,
                    $"更新用户: {user.Username}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"更新用户失败: {user.Username}", ex.Message, "User", user.Id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    return false;

                _users.Remove(user);

                // 使用扩展方法记录审计日志
                await _auditLogService.LogDeleteAsync(
                    1, "admin", "Account", "User", id, user,
                    $"删除用户: {user.Username}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"删除用户失败", ex.Message, "User", id);
                return false;
            }
        }

        public async Task<bool> EnableUserAsync(int id, bool isEnabled)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return false;

            user.IsEnabled = isEnabled;
            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.Update, "Account",
                $"{(isEnabled ? "启用" : "禁用")}用户: {user.Username}", "User", id);

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            user.PasswordHash = newPassword;
            await _auditLogService.LogChangePasswordAsync(userId, user.Username);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            user.PasswordHash = newPassword;
            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.ChangePassword, "Account",
                $"重置用户密码: {user.Username}", "User", userId);

            return true;
        }

        public async Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return false;

            user.RoleIds = roleIds ?? new List<int>();
            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.Update, "Account",
                $"修改用户角色: {user.Username}", "User", userId);

            return true;
        }

        public async Task<List<Role>> GetUserRolesAsync(int userId)
        {
            return await Task.FromResult(new List<Role>());
        }

        private void InitializeSampleData()
        {
            _users.AddRange(new[]
            {
                new User
                {
                    Id = _nextUserId++,
                    Username = "admin",
                    RealName = "系统管理员",
                    Email = "admin@example.com",
                    Phone = "13800138000",
                    PasswordHash = "admin123",
                    IsEnabled = true,
                    RoleIds = new List<int> { 1 }
                },
                new User
                {
                    Id = _nextUserId++,
                    Username = "user1",
                    RealName = "张三",
                    Email = "zhangsan@example.com",
                    Phone = "13800138001",
                    PasswordHash = "user123",
                    IsEnabled = true,
                    RoleIds = new List<int> { 2 }
                },
                new User
                {
                    Id = _nextUserId++,
                    Username = "user2",
                    RealName = "李四",
                    Email = "lisi@example.com",
                    Phone = "13800138002",
                    PasswordHash = "user123",
                    IsEnabled = true,
                    RoleIds = new List<int> { 2 }
                }
            });
        }
    }
}