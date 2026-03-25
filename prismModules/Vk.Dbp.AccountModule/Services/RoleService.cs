using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Extensions;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 角色服务实现
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly List<Role> _roles = new();
        private readonly IAuditLogService _auditLogService;
        private int _nextRoleId = 1;

        public RoleService(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            InitializeSampleData();
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await Task.FromResult(_roles.ToList());
        }

        public async Task<Role> GetRoleByIdAsync(int id)
        {
            return await Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));
        }

        public async Task<bool> CreateRoleAsync(Role role)
        {
            try
            {
                role.Id = _nextRoleId++;
                role.CreatedTime = DateTime.Now;
                _roles.Add(role);

                await _auditLogService.LogCreateAsync(
                    1, "admin", "Account", "Role", role.Id, role,
                    $"创建角色: {role.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建角色失败", ex.Message, "Role", role.Id);
                return false;
            }
        }

        public async Task<bool> UpdateRoleAsync(Role role)
        {
            try
            {
                var existingRole = _roles.FirstOrDefault(r => r.Id == role.Id);
                if (existingRole == null)
                    return false;

                var oldData = new { existingRole.Name, existingRole.Description };

                existingRole.Name = role.Name;
                existingRole.Description = role.Description;
                existingRole.LastModifiedTime = DateTime.Now;
                existingRole.Remarks = role.Remarks;

                var newData = new { existingRole.Name, existingRole.Description };

                await _auditLogService.LogUpdateAsync(
                    1, "admin", "Account", "Role", role.Id, oldData, newData,
                    $"更新角色: {role.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"更新角色失败", ex.Message, "Role", role.Id);
                return false;
            }
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            try
            {
                var role = _roles.FirstOrDefault(r => r.Id == id);
                if (role == null)
                    return false;

                _roles.Remove(role);

                await _auditLogService.LogDeleteAsync(
                    1, "admin", "Account", "Role", id, role,
                    $"删除角色: {role.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"删除角色失败", ex.Message, "Role", id);
                return false;
            }
        }

        public async Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds)
        {
            var role = _roles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
                return false;

            role.PermissionIds = permissionIds ?? new List<int>();
            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.Update, "Account",
                $"修改角色权限: {role.Name}", "Role", roleId);

            return true;
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
        {
            return await Task.FromResult(new List<Permission>());
        }

        public async Task<bool> EnableRoleAsync(int id, bool isEnabled)
        {
            var role = _roles.FirstOrDefault(r => r.Id == id);
            if (role == null)
                return false;

            role.IsEnabled = isEnabled;
            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.Update, "Account",
                $"{(isEnabled ? "启用" : "禁用")}角色: {role.Name}", "Role", id);

            return true;
        }

        private void InitializeSampleData()
        {
            _roles.AddRange(new[]
            {
                new Role
                {
                    Id = _nextRoleId++,
                    Name = "系统管理员",
                    Description = "拥有系统所有权限",
                    IsEnabled = true,
                    PermissionIds = new List<int> { 1, 2, 3, 4, 5 }
                },
                new Role
                {
                    Id = _nextRoleId++,
                    Name = "普通用户",
                    Description = "只读权限",
                    IsEnabled = true,
                    PermissionIds = new List<int> { 4 }
                }
            });
        }
    }
}