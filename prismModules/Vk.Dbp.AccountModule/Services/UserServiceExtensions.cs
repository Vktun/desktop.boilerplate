using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.Contracts.Data;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// UserService扩展方法 - 提供分页查询和批量加载优化
    /// </summary>
    public static class UserServiceExtensions
    {
        /// <summary>
        /// 分页查询用户（优化N+1查询）
        /// </summary>
        public static async Task<PagedResult<User>> GetUsersPagedAsync(
            this IUserService userService,
            ISqlSugarClient db,
            IPagedQuery<User> query)
        {
            // 一次性查询用户和角色关联，避免N+1问题
            var queryable = db.Queryable<UserEntity>()
                .Where(u => !u.IsDeleted);
            
            // 应用过滤条件
            if (query.Filter != null)
            {
                queryable = queryable.Where(query.Filter);
            }
            
            // 获取总数
            int totalCount = await queryable.CountAsync();
            
            // 应用排序
            if (!string.IsNullOrWhiteSpace(query.SortField))
            {
                queryable = query.SortDirection?.ToLower() == "desc"
                    ? queryable.OrderByDescending(query.SortField)
                    : queryable.OrderBy(query.SortField);
            }
            else
            {
                queryable = queryable.OrderByDescending(u => u.CreationTime);
            }
            
            // 分页查询
            var entities = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
            
            // 批量加载角色关联（避免N+1）
            var userIds = entities.Select(u => u.Id).ToList();
            var userRoles = await db.Queryable<UserRole>()
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();
            
            // 批量加载角色信息
            var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
            var roles = await db.Queryable<Role>()
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();
            
            // 在内存中构建映射关系
            var roleDict = roles.ToDictionary(r => r.Id, r => r.Name);
            var userRoleDict = userRoles.GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.Select(ur => ur.RoleId).ToList());
            
            // 映射到Model
            var items = entities.Select(entity => new User
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
                RoleIds = userRoleDict.ContainsKey(entity.Id) ? userRoleDict[entity.Id] : new List<int>()
            });
            
            return new PagedResult<User>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
        }
        
        /// <summary>
        /// 优化的批量获取用户方法（避免N+1查询）
        /// </summary>
        public static async Task<List<User>> GetAllUsersOptimizedAsync(
            this IUserService userService,
            ISqlSugarClient db)
        {
            // 一次性查询用户和角色关联
            var userWithRoles = await db.Queryable<UserEntity, UserRole, Role>(
                (u, ur, r) => new JoinQueryInfos(
                    JoinType.Left, u.Id == ur.UserId,
                    JoinType.Left, ur.RoleId == r.Id
                ))
                .Where((u, ur, r) => !u.IsDeleted)
                .Select((u, ur, r) => new 
                {
                    User = u,
                    RoleId = ur.RoleId,
                    RoleName = r.Name
                })
                .ToListAsync();
            
            // 在内存中分组
            var userDict = new Dictionary<int, User>();
            foreach (var item in userWithRoles)
            {
                if (!userDict.ContainsKey(item.User.Id))
                {
                    userDict[item.User.Id] = new User
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
                        RoleIds = new List<int>()
                    };
                }
                
                if (item.RoleId > 0)
                {
                    userDict[item.User.Id].RoleIds.Add(item.RoleId);
                }
            }
            
            return userDict.Values.ToList();
        }
    }
}