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
    /// 权限服务实现
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly List<Permission> _permissions = new();
        private readonly IAuditLogService _auditLogService;
        private int _nextPermissionId = 1;

        public PermissionService(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            InitializeSampleData();
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await Task.FromResult(_permissions.ToList());
        }

        public async Task<List<Permission>> GetPermissionTreeAsync(PermissionType? type = null)
        {
            var permissions = _permissions.Where(p => type == null || p.Type == type).ToList();
            var rootPermissions = permissions.Where(p => p.ParentId == null).ToList();

            foreach (var root in rootPermissions)
            {
                BuildPermissionTree(root, permissions);
            }

            return await Task.FromResult(rootPermissions);
        }

        public async Task<Permission> GetPermissionByIdAsync(int id)
        {
            return await Task.FromResult(_permissions.FirstOrDefault(p => p.Id == id));
        }

        public async Task<bool> CreatePermissionAsync(Permission permission)
        {
            try
            {
                permission.Id = _nextPermissionId++;
                permission.CreatedTime = DateTime.Now;
                _permissions.Add(permission);

                await _auditLogService.LogCreateAsync(
                    1, "admin", "Account", "Permission", permission.Id, permission,
                    $"创建权限: {permission.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建权限失败", ex.Message, "Permission");
                return false;
            }
        }

        public async Task<bool> UpdatePermissionAsync(Permission permission)
        {
            try
            {
                var existingPermission = _permissions.FirstOrDefault(p => p.Id == permission.Id);
                if (existingPermission == null)
                    return false;

                var oldData = new
                {
                    existingPermission.Name,
                    existingPermission.Code,
                    existingPermission.Description,
                    existingPermission.Icon
                };

                existingPermission.Name = permission.Name;
                existingPermission.Code = permission.Code;
                existingPermission.Description = permission.Description;
                existingPermission.Icon = permission.Icon;
                existingPermission.SortOrder = permission.SortOrder;

                var newData = new
                {
                    existingPermission.Name,
                    existingPermission.Code,
                    existingPermission.Description,
                    existingPermission.Icon
                };

                await _auditLogService.LogUpdateAsync(
                    1, "admin", "Account", "Permission", permission.Id, oldData, newData,
                    $"更新权限: {permission.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"更新权限失败", ex.Message, "Permission", permission.Id);
                return false;
            }
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            try
            {
                var permission = _permissions.FirstOrDefault(p => p.Id == id);
                if (permission == null)
                    return false;

                _permissions.Remove(permission);

                await _auditLogService.LogDeleteAsync(
                    1, "admin", "Account", "Permission", id, permission,
                    $"删除权限: {permission.Name}");

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"删除权限失败", ex.Message, "Permission", id);
                return false;
            }
        }

        public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
        {
            return await Task.FromResult(new List<Permission>());
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            return await Task.FromResult(true);
        }

        public async Task<bool> EnablePermissionAsync(int id, bool isEnabled)
        {
            var permission = _permissions.FirstOrDefault(p => p.Id == id);
            if (permission == null)
                return false;

            permission.IsEnabled = isEnabled;
            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.Update, "Account",
                $"{(isEnabled ? "启用" : "禁用")}权限: {permission.Name}", "Permission", id);

            return true;
        }

        private void BuildPermissionTree(Permission parent, List<Permission> allPermissions)
        {
            parent.Children = allPermissions
                .Where(p => p.ParentId == parent.Id)
                .ToList();

            foreach (var child in parent.Children)
            {
                BuildPermissionTree(child, allPermissions);
            }
        }

        private void InitializeSampleData()
        {
            _permissions.AddRange(new[]
            {
                new Permission
                {
                    Id = _nextPermissionId++,
                    Name = "系统管理",
                    Code = "system:manage",
                    Type = PermissionType.Menu,
                    Description = "系统管理模块",
                    Module = "Account",
                    SortOrder = 1,
                    ParentId = null,
                    IsEnabled = true
                },
                new Permission
                {
                    Id = _nextPermissionId++,
                    Name = "用户管理",
                    Code = "user:manage",
                    Type = PermissionType.Menu,
                    Description = "用户管理菜单",
                    Module = "Account",
                    SortOrder = 1,
                    ParentId = 1,
                    IsEnabled = true
                },
                new Permission
                {
                    Id = _nextPermissionId++,
                    Name = "新增用户",
                    Code = "user:create",
                    Type = PermissionType.Button,
                    Description = "新增用户按钮",
                    Module = "Account",
                    SortOrder = 1,
                    ParentId = 2,
                    IsEnabled = true
                },
                new Permission
                {
                    Id = _nextPermissionId++,
                    Name = "角色管理",
                    Code = "role:manage",
                    Type = PermissionType.Menu,
                    Description = "角色管理菜单",
                    Module = "Account",
                    SortOrder = 2,
                    ParentId = 1,
                    IsEnabled = true
                },
                new Permission
                {
                    Id = _nextPermissionId++,
                    Name = "权限管理",
                    Code = "permission:manage",
                    Type = PermissionType.Menu,
                    Description = "权限管理菜单",
                    Module = "Account",
                    SortOrder = 3,
                    ParentId = 1,
                    IsEnabled = true
                }
            });
        }
    }
}