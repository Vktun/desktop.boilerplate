using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using PermissionEntity = Dabp.Infrastructure.Entities.Permission;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using RoleModel = Vk.Dbp.AccountModule.Models.Role;

namespace Vk.Dbp.AccountModule.Services;

public class RoleService : IRoleService
{
    private readonly ISqlSugarClient _db;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;

    public RoleService(ISqlSugarClient db, IAuditLogService auditLogService, IUserSession userSession)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public async Task<List<RoleModel>> GetAllRolesAsync()
    {
        List<RoleEntity> entities = await _db.Queryable<RoleEntity>().ToListAsync();
        return entities.Select(MapToModel).ToList();
    }

    public async Task<RoleModel?> GetRoleByIdAsync(int id)
    {
        RoleEntity? entity = await _db.Queryable<RoleEntity>()
            .FirstAsync(role => role.Id == id);
        return entity is null ? null : MapToModel(entity);
    }

    public async Task<bool> CreateRoleAsync(RoleModel role)
    {
        try
        {
            RoleEntity entity = MapToEntity(role);

            int result = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
            if (result <= 0)
            {
                return false;
            }

            role.Id = result;
            await _auditLogService.LogCreateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "Role",
                role.Id,
                role,
                $"鍒涘缓瑙掕壊: {role.Name}");

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Create,
                "Account",
                "鍒涘缓瑙掕壊澶辫触",
                ex.Message,
                "Role",
                role.Id);
            return false;
        }
    }

    public async Task<bool> UpdateRoleAsync(RoleModel role)
    {
        try
        {
            RoleEntity? existingEntity = await _db.Queryable<RoleEntity>()
                .FirstAsync(entity => entity.Id == role.Id);
            if (existingEntity is null)
            {
                return false;
            }

            var oldData = new { existingEntity.Name };
            existingEntity.Name = role.Name;
            existingEntity.RoleLevel = role.IsEnabled ? 1 : 0;

            int result = await _db.Updateable(existingEntity)
                .Where(entity => entity.Id == role.Id)
                .ExecuteCommandAsync();
            if (result <= 0)
            {
                return false;
            }

            var newData = new { existingEntity.Name };
            await _auditLogService.LogUpdateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "Role",
                role.Id,
                oldData,
                newData,
                $"鏇存柊瑙掕壊: {role.Name}");

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                "鏇存柊瑙掕壊澶辫触",
                ex.Message,
                "Role",
                role.Id);
            return false;
        }
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        try
        {
            RoleEntity? entity = await _db.Queryable<RoleEntity>()
                .FirstAsync(role => role.Id == id);
            if (entity is null)
            {
                return false;
            }

            await _db.Deleteable<RolePermission>()
                .Where(rp => rp.RoleId == id)
                .ExecuteCommandAsync();

            int result = await _db.Deleteable<RoleEntity>()
                .Where(role => role.Id == id)
                .ExecuteCommandAsync();
            if (result <= 0)
            {
                return false;
            }

            await _auditLogService.LogDeleteAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "Role",
                id,
                entity,
                $"鍒犻櫎瑙掕壊: {entity.Name}");

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                "鍒犻櫎瑙掕壊澶辫触",
                ex.Message,
                "Role",
                id);
            return false;
        }
    }

    public async Task<bool> AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds)
    {
        RoleEntity? role = await _db.Queryable<RoleEntity>()
            .FirstAsync(entity => entity.Id == roleId);
        if (role is null)
        {
            return false;
        }

        try
        {
            await _db.Deleteable<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ExecuteCommandAsync();

            if (permissionIds is { Count: > 0 })
            {
                List<RolePermission> rolePermissions = permissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreationTime = DateTime.Now,
                    CreatorId = _userSession.GetAuditUserId()
                }).ToList();

                await _db.Insertable(rolePermissions).ExecuteCommandAsync();
            }

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                $"淇敼瑙掕壊鏉冮檺: {role.Name}",
                "Role",
                roleId);

            return true;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                "鍒嗛厤鏉冮檺澶辫触",
                ex.Message,
                "Role",
                roleId);
            return false;
        }
    }

    public async Task<List<PermissionModel>> GetRolePermissionsAsync(int roleId)
    {
        List<int> permissionIds = await _db.Queryable<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        if (permissionIds.Count == 0)
        {
            return new List<PermissionModel>();
        }

        List<PermissionEntity> permissionEntities = await _db.Queryable<PermissionEntity>()
            .Where(p => permissionIds.Contains(p.Id) && p.IsEnabled)
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
        RoleEntity? role = await _db.Queryable<RoleEntity>()
            .FirstAsync(entity => entity.Id == id);
        if (role is null)
        {
            return false;
        }

        int result = await _db.Updateable<RoleEntity>()
            .SetColumns(r => new RoleEntity { RoleLevel = isEnabled ? 1 : 0 })
            .Where(r => r.Id == id)
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
            $"{(isEnabled ? "鍚敤" : "绂佺敤")}瑙掕壊: {role.Name}",
            "Role",
            id);

        return true;
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
