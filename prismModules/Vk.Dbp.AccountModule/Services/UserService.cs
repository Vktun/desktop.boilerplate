using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;
using SqlSugar;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Extensions;
using Vk.Dbp.Core.Audit.Interfaces;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using RoleModel = Vk.Dbp.AccountModule.Models.Role;
using UserEntity = Dabp.Infrastructure.Entities.User;
using UserModel = Vk.Dbp.AccountModule.Models.User;

namespace Vk.Dbp.AccountModule.Services;

public class UserService : IUserService
{
    private readonly ISqlSugarClient _db;
    private readonly IAuditLogService _auditLogService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserSession _userSession;

    public UserService(
        ISqlSugarClient db,
        IAuditLogService auditLogService,
        IPasswordHasher passwordHasher,
        IUserSession userSession)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        List<UserEntity> entities = await _db.Queryable<UserEntity>()
            .Where(u => !u.IsDeleted)
            .ToListAsync();

        var result = new List<UserModel>();
        foreach (UserEntity entity in entities)
        {
            UserModel userModel = await MapToModelAsync(entity);
            result.Add(userModel);
        }

        return result;
    }

    public async Task<UserModel?> GetUserByIdAsync(int id)
    {
        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(u => u.Id == id && !u.IsDeleted);

        return entity is null ? null : await MapToModelAsync(entity);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(u => u.UserName == username && !u.IsDeleted);

        return entity is null ? null : await MapToModelAsync(entity);
    }

    public async Task<bool> CreateUserAsync(UserModel user)
    {
        try
        {
            UserEntity entity = MapToEntity(user);
            entity.CreationTime = DateTime.Now;
            entity.IsActive = true;
            entity.IsDeleted = false;

            int result = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
            user.Id = result;

            await _auditLogService.LogCreateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "User",
                user.Id,
                user,
                $"鍒涘缓鐢ㄦ埛: {user.Username}");

            return result > 0;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Create,
                "Account",
                $"鍒涘缓鐢ㄦ埛澶辫触: {user.Username}",
                ex.Message,
                "User",
                user.Id);
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(UserModel user)
    {
        try
        {
            UserEntity? existingEntity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == user.Id && !u.IsDeleted);

            if (existingEntity is null)
            {
                return false;
            }

            var oldData = new
            {
                RealName = existingEntity.SurName,
                Phone = existingEntity.PhoneNumber
            };

            existingEntity.SurName = user.RealName;
            existingEntity.PhoneNumber = user.Phone;
            existingEntity.LastModificationTime = DateTime.Now;

            int result = await _db.Updateable(existingEntity).ExecuteCommandAsync();

            var newData = new
            {
                RealName = existingEntity.SurName,
                Phone = existingEntity.PhoneNumber
            };

            await _auditLogService.LogUpdateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "User",
                user.Id,
                oldData,
                newData,
                $"鏇存柊鐢ㄦ埛: {user.Username}");

            return result > 0;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                $"鏇存柊鐢ㄦ埛澶辫触: {user.Username}",
                ex.Message,
                "User",
                user.Id);
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            UserEntity? entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == id && !u.IsDeleted);

            if (entity is null)
            {
                return false;
            }

            entity.IsDeleted = true;
            entity.DeletionTime = DateTime.Now;

            int result = await _db.Updateable(entity).ExecuteCommandAsync();

            UserModel userModel = await MapToModelAsync(entity);
            await _auditLogService.LogDeleteAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Account",
                "User",
                id,
                userModel,
                $"鍒犻櫎鐢ㄦ埛: {userModel.Username}");

            return result > 0;
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Delete,
                "Account",
                "鍒犻櫎鐢ㄦ埛澶辫触",
                ex.Message,
                "User",
                id);
            return false;
        }
    }

    public async Task<bool> EnableUserAsync(int id, bool isEnabled)
    {
        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(u => u.Id == id && !u.IsDeleted);

        if (entity is null)
        {
            return false;
        }

        entity.IsActive = isEnabled;
        entity.LastModificationTime = DateTime.Now;

        int result = await _db.Updateable(entity).ExecuteCommandAsync();

        await _auditLogService.LogOperationAsync(
            _userSession.GetAuditUserId(),
            _userSession.GetAuditUsername(),
            AuditActionType.Update,
            "Account",
            $"{(isEnabled ? "鍚敤" : "绂佺敤")}鐢ㄦ埛: {entity.UserName}",
            "User",
            id);

        return result > 0;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(u => u.Id == userId && !u.IsDeleted);

        if (entity is null)
        {
            return false;
        }

        if (!_passwordHasher.VerifyPassword(oldPassword, entity.PasswordHash))
        {
            return false;
        }

        entity.PasswordHash = _passwordHasher.HashPassword(newPassword);
        entity.ChangePasswordLastTime = DateTime.Now;

        int result = await _db.Updateable(entity).ExecuteCommandAsync();
        await _auditLogService.LogChangePasswordAsync(userId, entity.UserName);

        return result > 0;
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
    {
        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(u => u.Id == userId && !u.IsDeleted);

        if (entity is null)
        {
            return false;
        }

        entity.PasswordHash = _passwordHasher.HashPassword(newPassword);
        entity.ChangePasswordLastTime = DateTime.Now;

        int result = await _db.Updateable(entity).ExecuteCommandAsync();

        await _auditLogService.LogOperationAsync(
            _userSession.GetAuditUserId(),
            _userSession.GetAuditUsername(),
            AuditActionType.ChangePassword,
            "Account",
            $"閲嶇疆鐢ㄦ埛瀵嗙爜: {entity.UserName}",
            "User",
            userId);

        return result > 0;
    }

    public async Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds)
    {
        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(u => u.Id == userId && !u.IsDeleted);

        if (entity is null)
        {
            return false;
        }

        try
        {
            _db.Ado.BeginTran();

            await _db.Deleteable<UserRole>()
                .Where(ur => ur.UserId == userId)
                .ExecuteCommandAsync();

            if (roleIds is { Count: > 0 })
            {
                List<UserRole> userRoles = roleIds.Select(roleId => new UserRole
                {
                    UserId = userId,
                    RoleId = roleId
                }).ToList();

                await _db.Insertable(userRoles).ExecuteCommandAsync();
            }

            _db.Ado.CommitTran();

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Account",
                $"淇敼鐢ㄦ埛瑙掕壊: {entity.UserName}",
                "User",
                userId);

            return true;
        }
        catch
        {
            _db.Ado.RollbackTran();
            throw;
        }
    }

    public async Task<List<RoleModel>> GetUserRolesAsync(int userId)
    {
        List<RoleEntity> roles = await _db.Queryable<RoleEntity, UserRole>(
                (r, ur) => new JoinQueryInfos(
                    JoinType.Inner, r.Id == ur.RoleId))
            .Where((r, ur) => ur.UserId == userId)
            .Select((r, ur) => r)
            .ToListAsync();

        return roles.Select(r => new RoleModel
        {
            Id = r.Id,
            Name = r.Name,
            IsEnabled = true
        }).ToList();
    }

    private async Task<UserModel> MapToModelAsync(UserEntity entity)
    {
        List<int> roleIds = await GetUserRoleIdsAsync(entity.Id);
        return new UserModel
        {
            Id = entity.Id,
            Username = entity.UserName,
            RealName = entity.SurName,
            Email = $"{entity.UserName}@example.com",
            Phone = entity.PhoneNumber,
            PasswordHash = entity.PasswordHash,
            IsEnabled = entity.IsActive,
            CreatedTime = entity.CreationTime,
            LastModifiedTime = entity.LastModificationTime,
            RoleIds = roleIds
        };
    }

    private UserEntity MapToEntity(UserModel model)
    {
        return new UserEntity
        {
            Id = model.Id,
            UserName = model.Username,
            SurName = model.RealName,
            PhoneNumber = model.Phone,
            PasswordHash = model.PasswordHash ?? _passwordHasher.HashPassword("default123"),
            IsActive = model.IsEnabled
        };
    }

    private async Task<List<int>> GetUserRoleIdsAsync(int userId)
    {
        return await _db.Queryable<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();
    }
}
