using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;

namespace Dabp.Infrastructure
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ISqlSugarClient _db;
        private readonly IPasswordHasher _passwordHasher;

        public DatabaseInitializer(ISqlSugarClient db, IPasswordHasher passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        public async Task InitializeAsync()
        {
             InitializeDatabase();

            await InitializeDataAsync();
        }

        public  void InitializeDatabase()
        {
            if (!_db.DbMaintenance.GetDataBaseList().Contains(_db.Ado.Connection.Database))
            {
                _db.DbMaintenance.CreateDatabase();
            }

            _db.CodeFirst.InitTables(
                typeof(User),
                typeof(Role),
                typeof(Permission),
                typeof(OrganizationUnit),
                typeof(UserRole),
                typeof(UserOrganizationUnit),
                typeof(RoleOrganizationUnit),
                typeof(RolePermission),
                typeof(AuditLog)
            );

        }

        public async Task InitializeDataAsync()
        {
            // 创建默认角色
            if (!await _db.Queryable<Role>().AnyAsync())
            {
                var adminRole = new Role
                {
                    Name = "管理员",
                    IsDefault = true,
                    RoleLevel = 1
                };
                await _db.Insertable(adminRole).ExecuteCommandAsync();

                var userRole = new Role
                {
                    Name = "普通用户",
                    IsDefault = false,
                    RoleLevel = 2
                };
                await _db.Insertable(userRole).ExecuteCommandAsync();
            }

            // 创建默认权限
            if (!await _db.Queryable<Permission>().AnyAsync())
            {
                var permissions = new List<Permission>
                {
                    new Permission { DisplyName = "驾驶舱", ProviderKey = "Dashboard", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "自检", ProviderKey = "SelfCheck", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "生产信息", ProviderKey = "Production", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "生产记录", ProviderKey = "ProductionRecord", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "报警记录", ProviderKey = "AlarmRecord", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "审计追踪", ProviderKey = "AuditRecord", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "后台管理", ProviderKey = "AdminSettingView", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "用户管理", ProviderKey = "UserManagement", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "角色管理", ProviderKey = "RoleManagement", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "权限管理", ProviderKey = "PermissionManagement", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "组织管理", ProviderKey = "OrganizationManagement", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now },
                    new Permission { DisplyName = "审计日志", ProviderKey = "AuditLog", ProviderId = 2, IsEnabled = true, CreationTime = DateTime.Now }
                };
                await _db.Insertable(permissions).ExecuteCommandAsync();
            }

            // 创建默认用户
            if (!await _db.Queryable<User>().AnyAsync())
            {
                // 生成随机强密码作为默认管理员密码
                string defaultPassword = GenerateStrongPassword();
                var defaultUser = new User
                {
                    UserName = "admin",
                    PasswordHash = _passwordHasher.HashPassword(defaultPassword),
                    SurName = "系统管理员",
                    PhoneNumber = "13800138000",
                    IsActive = true,
                    ChangePasswordLastTime = DateTime.Now,
                    ValideDays = 90,
                    CreationTime = DateTime.Now,
                    CreatorId = 0,
                    IsDeleted = false
                };
                var userId = await _db.Insertable(defaultUser).ExecuteReturnIdentityAsync();

                // 记录默认密码到日志（仅首次初始化时）
                Log.Warning("默认管理员账户已创建 - 用户名: admin, 初始密码: {Password} (请立即修改)", defaultPassword);

                // 为默认用户分配管理员角色
                var adminRole = await _db.Queryable<Role>().Where(r => r.Name == "管理员").FirstAsync();
                if (adminRole != null)
                {
                    var userRoleAssignment = new UserRole
                    {
                        UserId = userId,
                        RoleId = adminRole.Id
                    };
                    await _db.Insertable(userRoleAssignment).ExecuteCommandAsync();
                }
            }

            // 为管理员角色分配所有权限
            var adminRoleEntity = await _db.Queryable<Role>().Where(r => r.Name == "管理员").FirstAsync();
            if (adminRoleEntity != null)
            {
                var existingRolePermissions = await _db.Queryable<RolePermission>()
                    .Where(rp => rp.RoleId == adminRoleEntity.Id)
                    .ToListAsync();

                if (existingRolePermissions.Count == 0)
                {
                    var permissions = await _db.Queryable<Permission>().ToListAsync();
                    var rolePermissions = new List<RolePermission>();
                    foreach (var permission in permissions)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRoleEntity.Id,
                            PermissionId = permission.Id
                        });
                    }
                    await _db.Insertable(rolePermissions).ExecuteCommandAsync();
                }
            }

            // 为普通用户角色分配基本权限
            var userRoleEntity = await _db.Queryable<Role>().Where(r => r.Name == "普通用户").FirstAsync();
            if (userRoleEntity != null)
            {
                var existingRolePermissions = await _db.Queryable<RolePermission>()
                    .Where(rp => rp.RoleId == userRoleEntity.Id)
                    .ToListAsync();

                if (existingRolePermissions.Count == 0)
                {
                    var basicPermissions = await _db.Queryable<Permission>()
                        .Where(p => p.ProviderKey == "Dashboard" || p.ProviderKey == "SelfCheck" || p.ProviderKey == "Production" || p.ProviderKey == "ProductionRecord" || p.ProviderKey == "AlarmRecord")
                        .ToListAsync();

                    var rolePermissions = new List<RolePermission>();
                    foreach (var permission in basicPermissions)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = userRoleEntity.Id,
                            PermissionId = permission.Id
                        });
                    }
                    await _db.Insertable(rolePermissions).ExecuteCommandAsync();
                }
            }

        }

        /// <summary>
        /// 生成随机强密码（12位，包含大小写字母、数字和特殊字符）
        /// </summary>
        private static string GenerateStrongPassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            
            var allChars = upperCase + lowerCase + digits + special;
            using var random = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var password = new char[12];
            
            // 确保至少包含每种类型的字符
            password[0] = upperCase[GetRandomInt(random, upperCase.Length)];
            password[1] = lowerCase[GetRandomInt(random, lowerCase.Length)];
            password[2] = digits[GetRandomInt(random, digits.Length)];
            password[3] = special[GetRandomInt(random, special.Length)];
            
            // 填充剩余字符
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[GetRandomInt(random, allChars.Length)];
            }
            
            // 打乱顺序
            return new string(password.OrderBy(_ => GetRandomInt(random, password.Length)).ToArray());
        }

        private static int GetRandomInt(System.Security.Cryptography.RNGCryptoServiceProvider rng, int maxValue)
        {
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt32(bytes, 0)) % maxValue;
        }
    }
}