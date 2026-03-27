using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Extensions;
using Vk.Dbp.Core.Audit.Interfaces;
using Dabp.Infrastructure.Entities;
using RoleModel = Vk.Dbp.AccountModule.Models.Role;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using PermissionEntity = Dabp.Infrastructure.Entities.Permission;

namespace Vk.Dbp.AccountModule.Services
{
    public class RoleService : IRoleService
    {
        private readonly ISqlSugarClient _db;
        private readonly IAuditLogService _auditLogService;

        public RoleService(ISqlSugarClient db, IAuditLogService auditLogService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        public async Task<List<RoleModel>> GetAllRolesAsync()
        {
            var entities = await _db.Queryable<RoleEntity>().ToListAsync();
            return entities.Select(MapToModel).ToList();
        }

        public async Task<RoleModel> GetRoleByIdAsync(int id)
        {
            var entity = await _db.Queryable<RoleEntity>().InSingleAsync(id);
            return entity == null ? null : MapToModel(entity);
        }

        public async Task<bool> CreateRoleAsync(RoleModel role)
        {
            try
            {
                var entity = MapToEntity(role);
                entity.RoleLevel = 0;
                
                var result = await _db.Insertable(entity).ExecuteCommandAsync();
                if (result > 0)
                {
                    role.Id = entity.Id;
                    await _auditLogService.LogCreateAsync(
                        1, "admin", "Account", "Role", role.Id, role,
                        $"创建角色: {role.Name}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建角色失败", ex.Message, "Role", role.Id);
                return false;
            }
        }

        public async Task<bool> UpdateRoleAsync(RoleModel role)
        {
            try
            {
                var existingEntity = await _db.Queryable<RoleEntity>().InSingleAsync(role.Id);
                if (existingEntity == null)
                    return false;

                var oldData = new { existingEntity.Name };

                existingEntity.Name = role.Name;
                
                var result = await _db.Updateable(existingEntity).ExecuteCommandAsync();
                if (result > 0)
                {
                    var newData = new { existingEntity.Name };
                    await _auditLogService.LogUpdateAsync(
                        1, "admin", "Account", "Role", role.Id, oldData, newData,
                        $"更新角色: {role.Name}");
                    return true;
                }
                return false;
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
                var entity = await _db.Queryable<RoleEntity>().InSingleAsync(id);
                if (entity == null)
                    return false;

                await _db.Deleteable<RolePermission>().Where(rp => rp.RoleId == id).ExecuteCommandAsync();

                var result = await _db.Deleteable<RoleEntity>().In(id).ExecuteCommandAsync();
                if (result > 0)
                {
                    await _auditLogService.LogDeleteAsync(
                        1, "admin", "Account", "Role", id, entity,
                        $"删除角色: {entity.Name}");
                    return true;
                }
                return false;
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
            var role = await _db.Queryable<RoleEntity>().InSingleAsync(roleId);
            if (role == null)
                return false;

            try
            {
                await _db.Deleteable<RolePermission>().Where(rp => rp.RoleId == roleId).ExecuteCommandAsync();

                if (permissionIds != null && permissionIds.Any())
                {
                    var rolePermissions = permissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        CreationTime = DateTime.Now,
                        CreatorId = 1
                    }).ToList();

                    await _db.Insertable(rolePermissions).ExecuteCommandAsync();
                }

                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"修改角色权限: {role.Name}", "Role", roleId);

                return true;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"分配权限失败", ex.Message, "Role", roleId);
                return false;
            }
        }

        public async Task<List<PermissionModel>> GetRolePermissionsAsync(int roleId)
        {
            var permissionIds = await _db.Queryable<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            if (!permissionIds.Any())
                return new List<PermissionModel>();

            var permissionEntities = await _db.Queryable<PermissionEntity>()
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync();

            return permissionEntities.Select(p => new PermissionModel
            {
                Id = p.Id,
                Name = p.DisplyName,
                Code = p.ProviderKey,
                IsEnabled = p.IsEnabled
            }).ToList();
        }

        public async Task<bool> EnableRoleAsync(int id, bool isEnabled)
        {
            var role = await _db.Queryable<RoleEntity>().InSingleAsync(id);
            if (role == null)
                return false;

            var result = await _db.Updateable<RoleEntity>()
                .SetColumns(r => new RoleEntity { RoleLevel = isEnabled ? 1 : 0 })
                .Where(r => r.Id == id)
                .ExecuteCommandAsync();

            if (result > 0)
            {
                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"{(isEnabled ? "启用" : "禁用")}角色: {role.Name}", "Role", id);
                return true;
            }
            return false;
        }

        private static RoleModel MapToModel(RoleEntity entity)
        {
            return new RoleModel
            {
                Id = entity.Id,
                Name = entity.Name,
                IsEnabled = entity.RoleLevel > 0,
                CreatedTime = DateTime.Now,
                PermissionIds = new List<int>()
            };
        }

        private static RoleEntity MapToEntity(RoleModel model)
        {
            return new RoleEntity
            {
                Id = model.Id,
                Name = model.Name,
                IsDefault = model.IsEnabled,
                RoleLevel = model.IsEnabled ? 1 : 0
            };
        }
    }
}
