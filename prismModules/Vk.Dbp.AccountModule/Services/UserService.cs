using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;
using SqlSugar;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Extensions;
using Vk.Dbp.Core.Audit.Interfaces;
using UserEntity = Dabp.Infrastructure.Entities.User;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using UserModel = Vk.Dbp.AccountModule.Models.User;
using RoleModel = Vk.Dbp.AccountModule.Models.Role;

namespace Vk.Dbp.AccountModule.Services
{
    public class UserService : IUserService
    {
        private readonly ISqlSugarClient _db;
        private readonly IAuditLogService _auditLogService;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(ISqlSugarClient db, IAuditLogService auditLogService, IPasswordHasher passwordHasher)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            var entities = await _db.Queryable<UserEntity>()
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            var result = new List<UserModel>();
            foreach (var entity in entities)
            {
                var userModel = await MapToModelAsync(entity);
                result.Add(userModel);
            }
            return result;
        }

        public async Task<UserModel> GetUserByIdAsync(int id)
        {
            var entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == id && !u.IsDeleted);
            return entity == null ? null : await MapToModelAsync(entity);
        }

        public async Task<UserModel> GetUserByUsernameAsync(string username)
        {
            var entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.UserName == username && !u.IsDeleted);
            return entity == null ? null : await MapToModelAsync(entity);
        }

        public async Task<bool> CreateUserAsync(UserModel user)
        {
            try
            {
                var entity = MapToEntity(user);
                entity.CreationTime = DateTime.Now;
                entity.IsActive = true;
                entity.IsDeleted = false;

                var result = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
                user.Id = result;

                await _auditLogService.LogCreateAsync(
                    1, "admin", "Account", "User", user.Id, user,
                    $"创建用户: {user.Username}");

                return result > 0;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建用户失败: {user.Username}", ex.Message, "User", user.Id);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(UserModel user)
        {
            try
            {
                var existingEntity = await _db.Queryable<UserEntity>()
                    .FirstAsync(u => u.Id == user.Id && !u.IsDeleted);
                if (existingEntity == null)
                    return false;

                var oldData = new
                {
                    RealName = existingEntity.SurName,
                    Phone = existingEntity.PhoneNumber
                };

                existingEntity.SurName = user.RealName;
                existingEntity.PhoneNumber = user.Phone;
                existingEntity.LastModificationTime = DateTime.Now;

                var result = await _db.Updateable(existingEntity).ExecuteCommandAsync();

                var newData = new
                {
                    RealName = existingEntity.SurName,
                    Phone = existingEntity.PhoneNumber
                };

                await _auditLogService.LogUpdateAsync(
                    1, "admin", "Account", "User", user.Id, oldData, newData,
                    $"更新用户: {user.Username}");

                return result > 0;
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
                var entity = await _db.Queryable<UserEntity>()
                    .FirstAsync(u => u.Id == id && !u.IsDeleted);
                if (entity == null)
                    return false;

                entity.IsDeleted = true;
                entity.DeletionTime = DateTime.Now;

                var result = await _db.Updateable(entity).ExecuteCommandAsync();

                var userModel = await MapToModelAsync(entity);
                await _auditLogService.LogDeleteAsync(
                    1, "admin", "Account", "User", id, userModel,
                    $"删除用户: {userModel.Username}");

                return result > 0;
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
            var entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == id && !u.IsDeleted);
            if (entity == null)
                return false;

            entity.IsActive = isEnabled;
            entity.LastModificationTime = DateTime.Now;

            var result = await _db.Updateable(entity).ExecuteCommandAsync();

            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.Update, "Account",
                $"{(isEnabled ? "启用" : "禁用")}用户: {entity.UserName}", "User", id);

            return result > 0;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == userId && !u.IsDeleted);
            if (entity == null)
                return false;

            if (!_passwordHasher.VerifyPassword(oldPassword, entity.PasswordHash))
                return false;

            entity.PasswordHash = _passwordHasher.HashPassword(newPassword);
            entity.ChangePasswordLastTime = DateTime.Now;

            var result = await _db.Updateable(entity).ExecuteCommandAsync();
            await _auditLogService.LogChangePasswordAsync(userId, entity.UserName);

            return result > 0;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == userId && !u.IsDeleted);
            if (entity == null)
                return false;

            entity.PasswordHash = _passwordHasher.HashPassword(newPassword);
            entity.ChangePasswordLastTime = DateTime.Now;

            var result = await _db.Updateable(entity).ExecuteCommandAsync();

            await _auditLogService.LogOperationAsync(
                1, "admin", AuditActionType.ChangePassword, "Account",
                $"重置用户密码: {entity.UserName}", "User", userId);

            return result > 0;
        }

        public async Task<bool> AssignRolesToUserAsync(int userId, List<int> roleIds)
        {
            var entity = await _db.Queryable<UserEntity>()
                .FirstAsync(u => u.Id == userId && !u.IsDeleted);
            if (entity == null)
                return false;

            try
            {
                _db.Ado.BeginTran();

                await _db.Deleteable<UserRole>()
                    .Where(ur => ur.UserId == userId)
                    .ExecuteCommandAsync();

                if (roleIds != null && roleIds.Any())
                {
                    var userRoles = roleIds.Select(roleId => new UserRole
                    {
                        UserId = userId,
                        RoleId = roleId
                    }).ToList();

                    await _db.Insertable(userRoles).ExecuteCommandAsync();
                }

                _db.Ado.CommitTran();

                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"修改用户角色: {entity.UserName}", "User", userId);

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
            var roles = await _db.Queryable<RoleEntity, UserRole>(
                (r, ur) => new JoinQueryInfos(
                    JoinType.Inner, r.Id == ur.RoleId
                ))
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
            var roleIds = await GetUserRoleIdsAsync(entity.Id);
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
            var roleIds = await _db.Queryable<UserRole>()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
            return roleIds;
        }
    }
}
