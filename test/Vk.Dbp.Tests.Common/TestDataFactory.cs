using Dabp.Infrastructure.Entities;
using System;
using System.Collections.Generic;

namespace Vk.Dbp.Tests.Common
{
    /// <summary>
    /// 测试数据工厂 - 生成测试所需的数据
    /// </summary>
    public static class TestDataFactory
    {
        /// <summary>
        /// 创建测试用户
        /// </summary>
        public static User CreateTestUser(
            int id = 1,
            string username = "testuser",
            string passwordHash = "hashedpassword",
            string realName = "测试用户",
            string phone = "13800138000",
            bool isEnabled = true)
        {
            return new User
            {
                Id = id,
                UserName = username,
                PasswordHash = passwordHash,
                SurName = realName,
                PhoneNumber = phone,
                IsActive = isEnabled,
                ChangePasswordLastTime = DateTime.Now,
                ValideDays = 90,
                CreationTime = DateTime.Now,
                CreatorId = 0,
                IsDeleted = false
            };
        }
        
        /// <summary>
        /// 创建测试角色
        /// </summary>
        public static Role CreateTestRole(
            int id = 1,
            string name = "测试角色",
            int roleLevel = 0)
        {
            return new Role
            {
                Id = id,
                Name = name,
                RoleLevel = roleLevel,
                IsDefault = false
            };
        }
        
        /// <summary>
        /// 创建测试权限
        /// </summary>
        public static Permission CreateTestPermission(
            int id = 1,
            string displyName = "测试权限",
            string providerKey = "Test",
            int providerId = 1)
        {
            return new Permission
            {
                Id = id,
                DisplyName = displyName,
                ProviderKey = providerKey,
                ProviderId = providerId,
                IsEnabled = true,
                CreationTime = DateTime.Now
            };
        }
        
        /// <summary>
        /// 创建批量测试用户
        /// </summary>
        public static List<User> CreateTestUsers(int count)
        {
            var users = new List<User>();
            for (int i = 1; i <= count; i++)
            {
                users.Add(CreateTestUser(
                    id: i,
                    username: $"user{i}",
                    realName: $"用户{i}"
                ));
            }
            return users;
        }
        
        /// <summary>
        /// 创建用户角色关联
        /// </summary>
        public static UserRole CreateUserRole(int userId, int roleId)
        {
            return new UserRole
            {
                UserId = userId,
                RoleId = roleId
            };
        }
        
        /// <summary>
        /// 创建角色权限关联
        /// </summary>
        public static RolePermission CreateRolePermission(int roleId, int permissionId)
        {
            return new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreationTime = DateTime.Now,
                CreatorId = 0
            };
        }
    }
}