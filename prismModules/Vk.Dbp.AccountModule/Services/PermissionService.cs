using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Extensions;
using Vk.Dbp.Core.Audit.Interfaces;
using Dabp.Infrastructure.Entities;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;
using PermissionEntity = Dabp.Infrastructure.Entities.Permission;

namespace Vk.Dbp.AccountModule.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ISqlSugarClient _db;
        private readonly IAuditLogService _auditLogService;

        public PermissionService(ISqlSugarClient db, IAuditLogService auditLogService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        public async Task<List<PermissionModel>> GetAllPermissionsAsync()
        {
            var entities = await _db.Queryable<PermissionEntity>().ToListAsync();
            return entities.Select(MapToModel).ToList();
        }

        public async Task<List<PermissionModel>> GetPermissionTreeAsync(PermissionType? type = null)
        {
            var entities = await _db.Queryable<PermissionEntity>().ToListAsync();

            if (type.HasValue)
            {
                entities = entities.Where(e => e.ProviderId == (int)type.Value).ToList();
            }

            var models = entities.Select(MapToModel).ToList();
            var rootPermissions = models.Where(p => string.IsNullOrEmpty(GetParentName(entities.First(e => e.Id == p.Id)))).ToList();

            foreach (var root in rootPermissions)
            {
                BuildPermissionTree(root, models, entities);
            }

            return rootPermissions;
        }

        public async Task<PermissionModel> GetPermissionByIdAsync(int id)
        {
            var entity = await _db.Queryable<PermissionEntity>().InSingleAsync(id);
            return entity == null ? null : MapToModel(entity);
        }

        public async Task<bool> CreatePermissionAsync(PermissionModel permission)
        {
            try
            {
                var entity = MapToEntity(permission);
                
                var result = await _db.Insertable(entity).ExecuteCommandAsync();
                if (result > 0)
                {
                    permission.Id = entity.Id;
                    await _auditLogService.LogCreateAsync(
                        1, "admin", "Account", "Permission", permission.Id, permission,
                        $"创建权限: {permission.Name}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建权限失败", ex.Message, "Permission");
                return false;
            }
        }

        public async Task<bool> UpdatePermissionAsync(PermissionModel permission)
        {
            try
            {
                var existingEntity = await _db.Queryable<PermissionEntity>().InSingleAsync(permission.Id);
                if (existingEntity == null)
                    return false;

                var oldData = new { existingEntity.DisplyName, existingEntity.ProviderKey };

                existingEntity.DisplyName = permission.Name;
                existingEntity.ProviderKey = permission.Code;
                existingEntity.IsEnabled = permission.IsEnabled;

                var result = await _db.Updateable(existingEntity).ExecuteCommandAsync();
                if (result > 0)
                {
                    var newData = new { existingEntity.DisplyName, existingEntity.ProviderKey };
                    await _auditLogService.LogUpdateAsync(
                        1, "admin", "Account", "Permission", permission.Id, oldData, newData,
                        $"更新权限: {permission.Name}");
                    return true;
                }
                return false;
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
                var entity = await _db.Queryable<PermissionEntity>().InSingleAsync(id);
                if (entity == null)
                    return false;

                await _db.Deleteable<RolePermission>().Where(rp => rp.PermissionId == id).ExecuteCommandAsync();

                var result = await _db.Deleteable<PermissionEntity>().In(id).ExecuteCommandAsync();
                if (result > 0)
                {
                    await _auditLogService.LogDeleteAsync(
                        1, "admin", "Account", "Permission", id, entity,
                        $"删除权限: {entity.DisplyName}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"删除权限失败", ex.Message, "Permission", id);
                return false;
            }
        }

        public async Task<List<PermissionModel>> GetUserPermissionsAsync(int userId)
        {
            var roleIds = await _db.Queryable<UserRole>()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!roleIds.Any())
                return new List<PermissionModel>();

            var permissionIds = await _db.Queryable<RolePermission>()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            if (!permissionIds.Any())
                return new List<PermissionModel>();

            var permissionEntities = await _db.Queryable<PermissionEntity>()
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync();

            return permissionEntities.Select(MapToModel).ToList();
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Any(p => p.Code == permissionCode);
        }

        public async Task<bool> EnablePermissionAsync(int id, bool isEnabled)
        {
            var permission = await _db.Queryable<PermissionEntity>().InSingleAsync(id);
            if (permission == null)
                return false;

            var result = await _db.Updateable<PermissionEntity>()
                .SetColumns(p => new PermissionEntity { IsEnabled = isEnabled })
                .Where(p => p.Id == id)
                .ExecuteCommandAsync();

            if (result > 0)
            {
                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"{(isEnabled ? "启用" : "禁用")}权限: {permission.DisplyName}", "Permission", id);
                return true;
            }
            return false;
        }

        private static PermissionModel MapToModel(PermissionEntity entity)
        {
            return new PermissionModel
            {
                Id = entity.Id,
                Name = entity.DisplyName,
                Code = entity.ProviderKey,
                Type = (PermissionType)entity.ProviderId,
                IsEnabled = entity.IsEnabled,
                CreatedTime = DateTime.Now
            };
        }

        private static PermissionEntity MapToEntity(PermissionModel model)
        {
            return new PermissionEntity
            {
                Id = model.Id,
                DisplyName = model.Name,
                ProviderKey = model.Code,
                ProviderId = (int)model.Type,
                IsEnabled = model.IsEnabled,
                ParentName = model.ParentId.HasValue ? model.ParentId.ToString() : null
            };
        }

        private static string GetParentName(PermissionEntity entity)
        {
            return entity.ParentName;
        }

        private void BuildPermissionTree(PermissionModel parent, List<PermissionModel> allPermissions, List<PermissionEntity> entities)
        {
            var parentEntity = entities.FirstOrDefault(e => e.Id == parent.Id);
            if (parentEntity == null) return;

            var childEntities = entities.Where(e => e.ParentName == parentEntity.DisplyName).ToList();
            
            parent.Children = allPermissions
                .Where(p => childEntities.Any(ce => ce.Id == p.Id))
                .ToList();

            foreach (var child in parent.Children)
            {
                BuildPermissionTree(child, allPermissions, entities);
            }
        }
    }
}
