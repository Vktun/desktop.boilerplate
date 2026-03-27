using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Interfaces;
using UserEntity = Dabp.Infrastructure.Entities.User;
using UserModel = Vk.Dbp.AccountModule.Models.User;

namespace Vk.Dbp.AccountModule.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly ISqlSugarClient _db;
        private readonly IAuditLogService _auditLogService;

        public OrganizationService(ISqlSugarClient db, IAuditLogService auditLogService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        public async Task<List<OrganizationUnitModel>> GetAllOrganizationUnitsAsync()
        {
            var entities = await _db.Queryable<OrganizationUnit>()
                .OrderBy(o => o.Code)
                .ToListAsync();
            return entities.Select(MapToModel).ToList();
        }

        public async Task<OrganizationUnitModel> GetOrganizationUnitByIdAsync(int id)
        {
            var entity = await _db.Queryable<OrganizationUnit>()
                .FirstAsync(o => o.Id == id);
            return entity == null ? null : MapToModel(entity);
        }

        public async Task<bool> CreateOrganizationUnitAsync(OrganizationUnitModel orgUnit)
        {
            try
            {
                var entity = MapToEntity(orgUnit);
                entity.CreationTime = DateTime.Now;

                var result = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
                orgUnit.Id = result;

                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建组织单元: {orgUnit.DisplayName}", "OrganizationUnit", orgUnit.Id);

                return result > 0;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Create, "Account",
                    $"创建组织单元失败: {orgUnit.DisplayName}", ex.Message, "OrganizationUnit", orgUnit.Id);
                return false;
            }
        }

        public async Task<bool> UpdateOrganizationUnitAsync(OrganizationUnitModel orgUnit)
        {
            try
            {
                var existingEntity = await _db.Queryable<OrganizationUnit>()
                    .FirstAsync(o => o.Id == orgUnit.Id);
                if (existingEntity == null)
                    return false;

                existingEntity.DisplyName = orgUnit.DisplayName;
                existingEntity.Code = orgUnit.Code;
                existingEntity.ParentId = orgUnit.ParentId;
                existingEntity.LastModificationTime = DateTime.Now;

                var result = await _db.Updateable(existingEntity).ExecuteCommandAsync();

                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"更新组织单元: {orgUnit.DisplayName}", "OrganizationUnit", orgUnit.Id);

                return result > 0;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"更新组织单元失败: {orgUnit.DisplayName}", ex.Message, "OrganizationUnit", orgUnit.Id);
                return false;
            }
        }

        public async Task<bool> DeleteOrganizationUnitAsync(int id)
        {
            try
            {
                var entity = await _db.Queryable<OrganizationUnit>()
                    .FirstAsync(o => o.Id == id);
                if (entity == null)
                    return false;

                var hasChildren = await _db.Queryable<OrganizationUnit>()
                    .AnyAsync(o => o.ParentId == id);
                if (hasChildren)
                {
                    HandyControl.Controls.Growl.Warning("该组织下存在子组织，无法删除！");
                    return false;
                }

                var hasUsers = await _db.Queryable<UserOrganizationUnit>()
                    .AnyAsync(uo => uo.OrganizationUnitId == id);
                if (hasUsers)
                {
                    await _db.Deleteable<UserOrganizationUnit>()
                        .Where(uo => uo.OrganizationUnitId == id)
                        .ExecuteCommandAsync();
                }

                var result = await _db.Deleteable(entity).ExecuteCommandAsync();

                var orgModel = MapToModel(entity);
                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"删除组织单元: {orgModel.DisplayName}", "OrganizationUnit", id);

                return result > 0;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"删除组织单元失败", ex.Message, "OrganizationUnit", id);
                return false;
            }
        }

        public async Task<List<UserModel>> GetOrganizationUsersAsync(int orgUnitId)
        {
            var users = await _db.Queryable<UserEntity, UserOrganizationUnit>(
                (u, uo) => new JoinQueryInfos(
                    JoinType.Inner, u.Id == uo.UserId
                ))
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
            try
            {
                var orgUnit = await _db.Queryable<OrganizationUnit>()
                    .FirstAsync(o => o.Id == orgUnitId);
                if (orgUnit == null)
                    return false;

                _db.Ado.BeginTran();

                foreach (var userId in userIds)
                {
                    var exists = await _db.Queryable<UserOrganizationUnit>()
                        .AnyAsync(uo => uo.UserId == userId && uo.OrganizationUnitId == orgUnitId);

                    if (!exists)
                    {
                        var userOrg = new UserOrganizationUnit
                        {
                            UserId = userId,
                            OrganizationUnitId = orgUnitId,
                            CreationTime = DateTime.Now,
                            CreatorId = 1
                        };
                        await _db.Insertable(userOrg).ExecuteCommandAsync();
                    }
                }

                _db.Ado.CommitTran();

                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Update, "Account",
                    $"分配用户到组织: {orgUnit.DisplyName}", "OrganizationUnit", orgUnitId);

                return true;
            }
            catch
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }

        public async Task<bool> RemoveUserFromOrganizationAsync(int orgUnitId, int userId)
        {
            try
            {
                var result = await _db.Deleteable<UserOrganizationUnit>()
                    .Where(uo => uo.UserId == userId && uo.OrganizationUnitId == orgUnitId)
                    .ExecuteCommandAsync();

                await _auditLogService.LogOperationAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"从组织中移除用户", "OrganizationUnit", orgUnitId);

                return result > 0;
            }
            catch (Exception ex)
            {
                await _auditLogService.LogFailureAsync(
                    1, "admin", AuditActionType.Delete, "Account",
                    $"从组织中移除用户失败", ex.Message, "OrganizationUnit", orgUnitId);
                return false;
            }
        }

        public async Task<List<OrganizationUnitModel>> GetUserOrganizationsAsync(int userId)
        {
            var orgUnits = await _db.Queryable<OrganizationUnit, UserOrganizationUnit>(
                (o, uo) => new JoinQueryInfos(
                    JoinType.Inner, o.Id == uo.OrganizationUnitId
                ))
                .Where((o, uo) => uo.UserId == userId)
                .Select((o, uo) => o)
                .ToListAsync();

            return orgUnits.Select(MapToModel).ToList();
        }

        public async Task<List<OrganizationUnitModel>> BuildOrganizationTreeAsync()
        {
            var allOrgs = await GetAllOrganizationUnitsAsync();
            var rootOrgs = allOrgs.Where(o => o.ParentId == 0).ToList();

            foreach (var rootOrg in rootOrgs)
            {
                await BuildTreeRecursive(rootOrg, allOrgs);
            }

            return rootOrgs;
        }

        private async Task BuildTreeRecursive(OrganizationUnitModel parent, List<OrganizationUnitModel> allOrgs)
        {
            var children = allOrgs.Where(o => o.ParentId == parent.Id).ToList();
            foreach (var child in children)
            {
                parent.Children.Add(child);
                await BuildTreeRecursive(child, allOrgs);
            }
        }

        private OrganizationUnitModel MapToModel(OrganizationUnit entity)
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

        private OrganizationUnit MapToEntity(OrganizationUnitModel model)
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
}
