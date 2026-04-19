using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using PermissionEntity = Dabp.Infrastructure.Entities.Permission;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;

namespace Vk.Dbp.AccountModule.Services;

public class PermissionService : IPermissionService
{
    private readonly ISqlSugarClient _db;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;

    public PermissionService(ISqlSugarClient db, IAuditLogService auditLogService, IUserSession userSession)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public async Task<List<PermissionModel>> GetAllPermissionsAsync()
    {
        List<PermissionEntity> entities = await _db.Queryable<PermissionEntity>().ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    public async Task<List<PermissionModel>> GetPermissionTreeAsync(PermissionType? type = null)
    {
        List<PermissionEntity> entities = await _db.Queryable<PermissionEntity>()
            .Where(p => p.IsEnabled)
            .ToListAsync();

        if (type.HasValue)
        {
            entities = entities.Where(e => e.ProviderId == (int)type.Value).ToList();
        }

        List<PermissionModel> models = entities.Select(MapToModel).ToList();
        List<PermissionModel> rootPermissions = models
            .Where(p => string.IsNullOrEmpty(GetParentName(entities.First(e => e.Id == p.Id))))
            .ToList();

        foreach (PermissionModel root in rootPermissions)
        {
            BuildPermissionTree(root, models, entities);
        }

        return rootPermissions;
    }

    public async Task<PermissionModel?> GetPermissionByIdAsync(int id)
    {
        PermissionEntity? entity = await _db.Queryable<PermissionEntity>().InSingleAsync(id);
        return entity is null ? null : MapToModel(entity);
    }

    public async Task<bool> CreatePermissionAsync(PermissionModel permission)
    {
        try
        {
            PermissionEntity entity = MapToEntity(permission);
            int result = await _db.Insertable(entity).ExecuteCommandAsync();
            if (result <= 0)
            {
                return false;
            }

            permission.Id = entity.Id;
            await _auditLogService.LogCreateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "Permission",
                permission.Id,
                permission,
                $"鍒涘缓鏉冮檺: {permission.Name}");

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Create,
                "Account",
                "鍒涘缓鏉冮檺澶辫触",
                ex.Message,
                "Permission");
            return false;
        }
    }

    public async Task<bool> UpdatePermissionAsync(PermissionModel permission)
    {
        try
        {
            PermissionEntity? existingEntity = await _db.Queryable<PermissionEntity>().InSingleAsync(permission.Id);
            if (existingEntity is null)
            {
                return false;
            }

            var oldData = new { existingEntity.DisplyName, existingEntity.ProviderKey };

            existingEntity.DisplyName = permission.Name;
            existingEntity.ProviderKey = permission.Code;
            existingEntity.IsEnabled = permission.IsEnabled;

            int result = await _db.Updateable(existingEntity).ExecuteCommandAsync();
            if (result <= 0)
            {
                return false;
            }

            var newData = new { existingEntity.DisplyName, existingEntity.ProviderKey };
            await _auditLogService.LogUpdateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "Permission",
                permission.Id,
                oldData,
                newData,
                $"鏇存柊鏉冮檺: {permission.Name}");

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                "鏇存柊鏉冮檺澶辫触",
                ex.Message,
                "Permission",
                permission.Id);
            return false;
        }
    }

    public async Task<bool> DeletePermissionAsync(int id)
    {
        try
        {
            PermissionEntity? entity = await _db.Queryable<PermissionEntity>().InSingleAsync(id);
            if (entity is null)
            {
                return false;
            }

            await _db.Deleteable<RolePermission>()
                .Where(rp => rp.PermissionId == id)
                .ExecuteCommandAsync();

            int result = await _db.Deleteable<PermissionEntity>().In(id).ExecuteCommandAsync();
            if (result <= 0)
            {
                return false;
            }

            await _auditLogService.LogDeleteAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "Permission",
                id,
                entity,
                $"鍒犻櫎鏉冮檺: {entity.DisplyName}");

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                "鍒犻櫎鏉冮檺澶辫触",
                ex.Message,
                "Permission",
                id);
            return false;
        }
    }

    public async Task<List<PermissionModel>> GetUserPermissionsAsync(int userId)
    {
        List<int> roleIds = await _db.Queryable<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0)
        {
            return new List<PermissionModel>();
        }

        List<int> permissionIds = await _db.Queryable<RolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync();

        if (permissionIds.Count == 0)
        {
            return new List<PermissionModel>();
        }

        List<PermissionEntity> permissionEntities = await _db.Queryable<PermissionEntity>()
            .Where(p => permissionIds.Contains(p.Id) && p.IsEnabled)
            .ToListAsync();

        return permissionEntities.Select(MapToModel).ToList();
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        List<PermissionModel> permissions = await GetUserPermissionsAsync(userId);
        return permissions.Any(p => p.Code == permissionCode && p.IsEnabled);
    }

    public async Task<bool> EnablePermissionAsync(int id, bool isEnabled)
    {
        PermissionEntity? permission = await _db.Queryable<PermissionEntity>().InSingleAsync(id);
        if (permission is null)
        {
            return false;
        }

        int result = await _db.Updateable<PermissionEntity>()
            .SetColumns(p => new PermissionEntity { IsEnabled = isEnabled })
            .Where(p => p.Id == id)
            .ExecuteCommandAsync();

        if (result <= 0)
        {
            return false;
        }

        await _auditLogService.LogOperationAsync(
            _userSession.GetAuditUserId(),
            _userSession.GetAuditUsername(),
            AuditActionType.Update,
            "Account",
            $"{(isEnabled ? "鍚敤" : "绂佺敤")}鏉冮檺: {permission.DisplyName}",
            "Permission",
            id);

        return true;
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

    private static string? GetParentName(PermissionEntity entity)
    {
        return entity.ParentName;
    }

    private void BuildPermissionTree(
        PermissionModel parent,
        List<PermissionModel> allPermissions,
        List<PermissionEntity> entities)
    {
        PermissionEntity? parentEntity = entities.FirstOrDefault(e => e.Id == parent.Id);
        if (parentEntity is null)
        {
            return;
        }

        List<PermissionEntity> childEntities = entities
            .Where(e => e.ParentName == parentEntity.DisplyName)
            .ToList();

        parent.Children = allPermissions
            .Where(p => childEntities.Any(ce => ce.Id == p.Id))
            .ToList();

        foreach (PermissionModel child in parent.Children)
        {
            BuildPermissionTree(child, allPermissions, entities);
        }
    }
}
