using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Contracts.Data;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using UserEntity = Dabp.Infrastructure.Entities.User;
using UserModel = Vk.Dbp.AccountModule.Models.User;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// UserService扩展方法，提供分页查询和批量加载优化。
    /// </summary>
    public static class UserServiceExtensions
    {
        /// <summary>
        /// 分页查询用户，避免N+1查询。
        /// </summary>
        public static async Task<PagedResult<UserModel>> GetUsersPagedAsync(
            this IUserService userService,
            ISqlSugarClient db,
            IPagedQuery<UserModel> query)
        {
            ArgumentNullException.ThrowIfNull(userService);
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(query);

            var queryable = db.Queryable<UserEntity>()
                .Where(u => !u.IsDeleted);

            int totalCount = await queryable.CountAsync();

            OrderByType orderByType = string.IsNullOrWhiteSpace(query.SortField)
                ? OrderByType.Desc
                : string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                    ? OrderByType.Desc
                    : OrderByType.Asc;

            queryable = queryable.OrderBy(u => u.CreationTime, orderByType);

            var entities = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            List<int> userIds = entities.Select(u => u.Id).ToList();
            List<UserRole> userRoles = await db.Queryable<UserRole>()
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            Dictionary<int, List<int>> userRoleDict = userRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(group => group.Key, group => group.Select(ur => ur.RoleId).ToList());

            IEnumerable<UserModel> items = entities.Select(entity => new UserModel
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
                RoleIds = userRoleDict.TryGetValue(entity.Id, out List<int>? roleIdsForUser)
                    ? roleIdsForUser
                    : []
            });

            return new PagedResult<UserModel>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 批量获取全部用户，避免N+1查询。
        /// </summary>
        public static async Task<List<UserModel>> GetAllUsersOptimizedAsync(
            this IUserService userService,
            ISqlSugarClient db)
        {
            ArgumentNullException.ThrowIfNull(userService);
            ArgumentNullException.ThrowIfNull(db);

            var userWithRoles = await db.Queryable<UserEntity, UserRole, RoleEntity>(
                    (u, ur, r) => new JoinQueryInfos(
                        JoinType.Left, u.Id == ur.UserId,
                        JoinType.Left, ur.RoleId == r.Id))
                .Where((u, ur, r) => !u.IsDeleted)
                .Select((u, ur, r) => new
                {
                    User = u,
                    RoleId = ur.RoleId
                })
                .ToListAsync();

            Dictionary<int, UserModel> userDict = new();
            foreach (var item in userWithRoles)
            {
                if (!userDict.TryGetValue(item.User.Id, out UserModel? user))
                {
                    user = new UserModel
                    {
                        Id = item.User.Id,
                        Username = item.User.UserName,
                        RealName = item.User.SurName,
                        Email = $"{item.User.UserName}@example.com",
                        Phone = item.User.PhoneNumber,
                        PasswordHash = item.User.PasswordHash,
                        IsEnabled = item.User.IsActive,
                        CreatedTime = item.User.CreationTime,
                        LastModifiedTime = item.User.LastModificationTime,
                        RoleIds = []
                    };
                    userDict[item.User.Id] = user;
                }

                if (item.RoleId > 0)
                {
                    user.RoleIds.Add(item.RoleId);
                }
            }

            return userDict.Values.ToList();
        }
    }
}
