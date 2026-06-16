using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using UserEntity = Dabp.Infrastructure.Entities.User;
using UserModel = Vk.Dbp.AccountModule.Models.User;

namespace Vk.Dbp.AccountModule.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ISqlSugarClient _db;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;

    public OrganizationService(ISqlSugarClient db, IAuditLogService auditLogService, IUserSession userSession)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public async Task<List<OrganizationUnitModel>> GetAllOrganizationUnitsAsync()
    {
        List<OrganizationUnit> entities = await _db.Queryable<OrganizationUnit>()
            .OrderBy(o => o.Code)
            .ToListAsync();

        return entities.Select(MapToModel).ToList();
    }

    public async Task<OrganizationUnitModel?> GetOrganizationUnitByIdAsync(int id)
    {
        OrganizationUnit? entity = await _db.Queryable<OrganizationUnit>()
            .FirstAsync(o => o.Id == id);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<bool> CreateOrganizationUnitAsync(OrganizationUnitModel orgUnit)
    {
        try
        {
            OrganizationUnit entity = MapToEntity(orgUnit);
            entity.CreationTime = DateTime.Now;

            int result = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
            orgUnit.Id = result;

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Create,
                "Account",
                $"鍒涘缓缁勭粐鍗曞厓: {orgUnit.DisplayName}",
                "OrganizationUnit",
                orgUnit.Id);

            return result > 0;
        }
        catch (Exception ex) when (AccountOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Create,
                "Account",
                $"鍒涘缓缁勭粐鍗曞厓澶辫触: {orgUnit.DisplayName}",
                ex.Message,
                "OrganizationUnit",
                orgUnit.Id);
            return false;
        }
    }

    public async Task<bool> UpdateOrganizationUnitAsync(OrganizationUnitModel orgUnit)
    {
        try
        {
            OrganizationUnit? existingEntity = await _db.Queryable<OrganizationUnit>()
                .FirstAsync(o => o.Id == orgUnit.Id);

            if (existingEntity is null)
            {
                return false;
            }

            existingEntity.DisplyName = orgUnit.DisplayName;
            existingEntity.Code = orgUnit.Code;
            existingEntity.ParentId = orgUnit.ParentId;
            existingEntity.LastModificationTime = DateTime.Now;

            int result = await _db.Updateable(existingEntity)
                .Where(o => o.Id == orgUnit.Id)
                .ExecuteCommandAsync();

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                $"鏇存柊缁勭粐鍗曞厓: {orgUnit.DisplayName}",
                "OrganizationUnit",
                orgUnit.Id);

            return result > 0;
        }
        catch (Exception ex) when (AccountOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                $"鏇存柊缁勭粐鍗曞厓澶辫触: {orgUnit.DisplayName}",
                ex.Message,
                "OrganizationUnit",
                orgUnit.Id);
            return false;
        }
    }

    public async Task<bool> DeleteOrganizationUnitAsync(int id)
    {
        try
        {
            OrganizationUnit? entity = await _db.Queryable<OrganizationUnit>()
                .FirstAsync(o => o.Id == id);

            if (entity is null)
            {
                return false;
            }

            bool hasChildren = await _db.Queryable<OrganizationUnit>()
                .AnyAsync(o => o.ParentId == id);

            if (hasChildren)
            {
                HandyControl.Controls.Growl.Warning("璇ョ粍缁囦笅瀛樺湪瀛愮粍缁囷紝鏃犳硶鍒犻櫎锛?");
                return false;
            }

            bool hasUsers = await _db.Queryable<UserOrganizationUnit>()
                .AnyAsync(uo => uo.OrganizationUnitId == id);

            if (hasUsers)
            {
                await _db.Deleteable<UserOrganizationUnit>()
                    .Where(uo => uo.OrganizationUnitId == id)
                    .ExecuteCommandAsync();
            }

            int result = await _db.Deleteable<OrganizationUnit>()
                .Where(o => o.Id == id)
                .ExecuteCommandAsync();
            OrganizationUnitModel orgModel = MapToModel(entity);

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                $"鍒犻櫎缁勭粐鍗曞厓: {orgModel.DisplayName}",
                "OrganizationUnit",
                id);

            return result > 0;
        }
        catch (Exception ex) when (AccountOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                "鍒犻櫎缁勭粐鍗曞厓澶辫触",
                ex.Message,
                "OrganizationUnit",
                id);
            return false;
        }
    }

    public async Task<List<UserModel>> GetOrganizationUsersAsync(int orgUnitId)
    {
        List<UserEntity> users = await _db.Queryable<UserEntity, UserOrganizationUnit>(
                (u, uo) => new JoinQueryInfos(
                    JoinType.Inner, u.Id == uo.UserId))
            .Where((u, uo) => uo.OrganizationUnitId == orgUnitId && !u.IsDeleted)
            .Select((u, uo) => u)
            .ToListAsync();

        return users.Select(u => new UserModel
        {
            Id = u.Id,
            Username = u.UserName,
            RealName = u.SurName,
            Phone = u.PhoneNumber,
            IsEnabled = u.IsActive,
            CreatedTime = u.CreationTime
        }).ToList();
    }

    public async Task<bool> AssignUsersToOrganizationAsync(int orgUnitId, List<int> userIds)
    {
        var committed = false;
        try
        {
            OrganizationUnit? orgUnit = await _db.Queryable<OrganizationUnit>()
                .FirstAsync(o => o.Id == orgUnitId);

            if (orgUnit is null)
            {
                return false;
            }

            _db.Ado.BeginTran();

            foreach (int userId in userIds)
            {
                bool exists = await _db.Queryable<UserOrganizationUnit>()
                    .AnyAsync(uo => uo.UserId == userId && uo.OrganizationUnitId == orgUnitId);

                if (!exists)
                {
                    UserOrganizationUnit userOrg = new()
                    {
                        UserId = userId,
                        OrganizationUnitId = orgUnitId,
                        CreationTime = DateTime.Now,
                        CreatorId = _userSession.GetAuditUserId()
                    };

                    await _db.Insertable(userOrg).ExecuteCommandAsync();
                }
            }

            _db.Ado.CommitTran();
            committed = true;

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                $"鍒嗛厤鐢ㄦ埛鍒扮粍缁? {orgUnit.DisplyName}",
                "OrganizationUnit",
                orgUnitId);

            return true;
        }
        finally
        {
            if (!committed)
            {
                _db.Ado.RollbackTran();
            }
        }
    }

    public async Task<bool> RemoveUserFromOrganizationAsync(int orgUnitId, int userId)
    {
        try
        {
            int result = await _db.Deleteable<UserOrganizationUnit>()
                .Where(uo => uo.UserId == userId && uo.OrganizationUnitId == orgUnitId)
                .ExecuteCommandAsync();

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                "浠庣粍缁囦腑绉婚櫎鐢ㄦ埛",
                "OrganizationUnit",
                orgUnitId);

            return result > 0;
        }
        catch (Exception ex) when (AccountOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                "浠庣粍缁囦腑绉婚櫎鐢ㄦ埛澶辫触",
                ex.Message,
                "OrganizationUnit",
                orgUnitId);
            return false;
        }
    }

    public async Task<List<OrganizationUnitModel>> GetUserOrganizationsAsync(int userId)
    {
        List<OrganizationUnit> orgUnits = await _db.Queryable<OrganizationUnit, UserOrganizationUnit>(
                (o, uo) => new JoinQueryInfos(
                    JoinType.Inner, o.Id == uo.OrganizationUnitId))
            .Where((o, uo) => uo.UserId == userId)
            .Select((o, uo) => o)
            .ToListAsync();

        return orgUnits.Select(MapToModel).ToList();
    }

    public async Task<List<OrganizationUnitModel>> BuildOrganizationTreeAsync()
    {
        List<OrganizationUnitModel> allOrgs = await GetAllOrganizationUnitsAsync();
        List<OrganizationUnitModel> rootOrgs = allOrgs.Where(o => o.ParentId == 0).ToList();

        foreach (OrganizationUnitModel rootOrg in rootOrgs)
        {
            await BuildTreeRecursive(rootOrg, allOrgs);
        }

        return rootOrgs;
    }

    private async Task BuildTreeRecursive(OrganizationUnitModel parent, List<OrganizationUnitModel> allOrgs)
    {
        List<OrganizationUnitModel> children = allOrgs.Where(o => o.ParentId == parent.Id).ToList();
        foreach (OrganizationUnitModel child in children)
        {
            parent.Children.Add(child);
            await BuildTreeRecursive(child, allOrgs);
        }
    }

    private static OrganizationUnitModel MapToModel(OrganizationUnit entity)
    {
        return new OrganizationUnitModel
        {
            Id = entity.Id,
            DisplayName = entity.DisplyName,
            Code = entity.Code,
            ParentId = entity.ParentId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime
        };
    }

    private static OrganizationUnit MapToEntity(OrganizationUnitModel model)
    {
        return new OrganizationUnit
        {
            Id = model.Id,
            DisplyName = model.DisplayName,
            Code = model.Code,
            ParentId = model.ParentId,
            CreationTime = model.CreationTime,
            LastModificationTime = model.LastModificationTime
        };
    }
}
