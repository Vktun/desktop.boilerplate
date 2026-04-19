using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;
using Serilog;

namespace Dabp.Infrastructure
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "123456";
        private const string DefaultAdminDisplayName = "系统管理员";
        private const string AdminRoleName = "管理员";
        private const string UserRoleName = "普通用户";
        private const int MenuPermissionProviderId = 1;

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
            await EnsureUnicodeTextColumnsAsync();

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
                typeof(AuditLog),
                typeof(SystemConfig),
                typeof(AlarmRecord),
                typeof(AlarmConfig)
            );

        }

        private async Task EnsureUnicodeTextColumnsAsync()
        {
            string[] statements =
            {
                "IF COL_LENGTH('dbo.Role', 'Name') IS NOT NULL ALTER TABLE [Role] ALTER COLUMN [Name] NVARCHAR(50) NULL",
                "IF COL_LENGTH('dbo.Permission', 'DisplyName') IS NOT NULL ALTER TABLE [Permission] ALTER COLUMN [DisplyName] NVARCHAR(100) NULL",
                "IF COL_LENGTH('dbo.Permission', 'ParentName') IS NOT NULL ALTER TABLE [Permission] ALTER COLUMN [ParentName] NVARCHAR(100) NULL",
                "IF COL_LENGTH('dbo.User', 'SurName') IS NOT NULL ALTER TABLE [User] ALTER COLUMN [SurName] NVARCHAR(50) NULL"
            };

            foreach (var statement in statements)
            {
                try
                {
                    await _db.Ado.ExecuteCommandAsync(statement);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to ensure Unicode text column");
                }
            }
        }

        public async Task InitializeDataAsync()
        {
            // 创建或补齐默认角色
            var adminRoleEntity = await EnsureDefaultRoleAsync(AdminRoleName, true, 1);
            var userRoleEntity = await EnsureDefaultRoleAsync(UserRoleName, false, 2);

            // 创建或补齐默认权限
            var permissions = await EnsureSeedPermissionsAsync();

            // 创建默认用户
            var adminUser = await _db.Queryable<User>()
                .Where(u => u.UserName == DefaultAdminUsername && !u.IsDeleted)
                .FirstAsync();
            if (adminUser == null)
            {
                // The initial admin password is intentionally fixed for local bootstrap.
                const string defaultPassword = DefaultAdminPassword;
                adminUser = new User
                {
                    UserName = DefaultAdminUsername,
                    PasswordHash = _passwordHasher.HashPassword(defaultPassword),
                    SurName = DefaultAdminDisplayName,
                    PhoneNumber = "13800138000",
                    IsActive = true,
                    ChangePasswordLastTime = DateTime.Now,
                    ValideDays = 90,
                    CreationTime = DateTime.Now,
                    CreatorId = 0,
                    IsDeleted = false
                };
                adminUser.Id = await _db.Insertable(adminUser).ExecuteReturnIdentityAsync();

                // 记录默认密码到日志（仅首次初始化时）
                Log.Warning(
                    "Default administrator account created - username: {Username}, initial password: {Password} (please change immediately)",
                    DefaultAdminUsername,
                    defaultPassword);
            }
            else
            {
                var shouldUpdateAdminUser = false;
                if (IsInvalidSeedText(adminUser.SurName))
                {
                    adminUser.SurName = DefaultAdminDisplayName;
                    shouldUpdateAdminUser = true;
                }

                if (!adminUser.IsActive)
                {
                    adminUser.IsActive = true;
                    shouldUpdateAdminUser = true;
                }

                if (shouldUpdateAdminUser)
                {
                    await _db.Updateable(adminUser).ExecuteCommandAsync();
                }
            }

            // 为管理员角色分配所有权限
            if (adminRoleEntity != null)
            {
                if (adminUser != null && !await _db.Queryable<UserRole>().AnyAsync(ur =>
                        ur.UserId == adminUser.Id && ur.RoleId == adminRoleEntity.Id))
                {
                    await _db.Insertable(new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRoleEntity.Id
                    }).ExecuteCommandAsync();
                }

                var existingRolePermissions = await _db.Queryable<RolePermission>()
                    .Where(rp => rp.RoleId == adminRoleEntity.Id)
                    .ToListAsync();

                var existingRolePermissionIds = new HashSet<int>(
                    existingRolePermissions.Select(rp => rp.PermissionId));
                var rolePermissions = permissions
                    .Where(permission => !existingRolePermissionIds.Contains(permission.Id))
                    .Select(permission => new RolePermission
                    {
                        RoleId = adminRoleEntity.Id,
                        PermissionId = permission.Id,
                        CreationTime = DateTime.Now,
                        CreatorId = adminUser?.Id ?? 0
                    })
                    .ToList();

                if (rolePermissions.Count > 0)
                {
                    await _db.Insertable(rolePermissions).ExecuteCommandAsync();
                }
            }

            // 为普通用户角色分配基本权限
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
                            PermissionId = permission.Id,
                            CreationTime = DateTime.Now,
                            CreatorId = adminUser?.Id ?? 0
                        });
                    }
                    await _db.Insertable(rolePermissions).ExecuteCommandAsync();
                }
            }

            // 初始化系统配置
            if (!await _db.Queryable<SystemConfig>().AnyAsync())
            {
                var defaultConfigs = new List<SystemConfig>
                {
                    new SystemConfig
                    {
                        ConfigKey = SystemConfigKeys.SessionTimeoutEnabled,
                        ConfigValue = "True",
                        Description = "是否启用会话超时",
                        ConfigType = "Boolean",
                        CreatedAt = DateTime.Now
                    },
                    new SystemConfig
                    {
                        ConfigKey = SystemConfigKeys.SessionTimeoutMinutes,
                        ConfigValue = "15",
                        Description = "会话超时时间（分钟）",
                        ConfigType = "Integer",
                        CreatedAt = DateTime.Now
                    }
                };
                await _db.Insertable(defaultConfigs).ExecuteCommandAsync();
            }

            // 初始化告警配置
            if (!await _db.Queryable<AlarmConfig>().AnyAsync())
            {
                var defaultAlarmConfigs = new List<AlarmConfig>
                {
                    new AlarmConfig
                    {
                        AlarmCode = "TEMP_HIGH",
                        AlarmName = "温度过高告警",
                        Description = "设备温度超过设定阈值时触发",
                        ThresholdMin = null,
                        ThresholdMax = 85,
                        ThresholdUnit = "℃",
                        ComparisonType = ComparisonTypes.GreaterThan,
                        EnablePopup = true,
                        EnableSound = false,
                        AutoAcknowledge = false,
                        AcknowledgeTimeout = 30,
                        DisplayColor = "Red",
                        Priority = 1,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    },
                    new AlarmConfig
                    {
                        AlarmCode = "PRESSURE_LOW",
                        AlarmName = "压力过低告警",
                        Description = "系统压力低于设定阈值时触发",
                        ThresholdMin = 0.5m,
                        ThresholdMax = null,
                        ThresholdUnit = "MPa",
                        ComparisonType = ComparisonTypes.LessThan,
                        EnablePopup = true,
                        EnableSound = true,
                        AutoAcknowledge = false,
                        AcknowledgeTimeout = 15,
                        DisplayColor = "Orange",
                        Priority = 2,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    },
                    new AlarmConfig
                    {
                        AlarmCode = "DEVICE_FAULT",
                        AlarmName = "设备故障告警",
                        Description = "设备运行异常或故障时触发",
                        EnablePopup = true,
                        EnableSound = true,
                        AutoAcknowledge = false,
                        AcknowledgeTimeout = 10,
                        DisplayColor = "Red",
                        Priority = 0,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    },
                    new AlarmConfig
                    {
                        AlarmCode = "SYSTEM_INFO",
                        AlarmName = "系统信息提示",
                        Description = "系统运行状态信息提示",
                        EnablePopup = true,
                        EnableSound = false,
                        AutoAcknowledge = true,
                        AcknowledgeTimeout = 60,
                        DisplayColor = "Blue",
                        Priority = 3,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    }
                };
                await _db.Insertable(defaultAlarmConfigs).ExecuteCommandAsync();
            }

        }

        private async Task<Role> EnsureDefaultRoleAsync(string roleName, bool isDefault, int roleLevel)
        {
            var candidates = await _db.Queryable<Role>()
                .Where(r => r.Name == roleName ||
                            ((r.Name == null || r.Name == string.Empty || r.Name.Contains("?")) &&
                             r.IsDefault == isDefault &&
                             r.RoleLevel == roleLevel))
                .OrderBy(r => r.Id)
                .ToListAsync();

            var role = candidates.FirstOrDefault();
            if (role == null)
            {
                role = new Role
                {
                    Name = roleName,
                    IsDefault = isDefault,
                    RoleLevel = roleLevel
                };
                role.Id = await _db.Insertable(role).ExecuteReturnIdentityAsync();
                return role;
            }

            var shouldUpdate = false;
            if (role.Name != roleName)
            {
                role.Name = roleName;
                shouldUpdate = true;
            }

            if (role.IsDefault != isDefault)
            {
                role.IsDefault = isDefault;
                shouldUpdate = true;
            }

            if (role.RoleLevel != roleLevel)
            {
                role.RoleLevel = roleLevel;
                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                await _db.Updateable(role).ExecuteCommandAsync();
            }

            var duplicateRoleIds = candidates
                .Skip(1)
                .Select(r => r.Id)
                .ToList();
            if (duplicateRoleIds.Count > 0)
            {
                await MergeDuplicateRolesAsync(role.Id, duplicateRoleIds);
            }

            return role;
        }

        private async Task MergeDuplicateRolesAsync(int keepRoleId, List<int> duplicateRoleIds)
        {
            var duplicateIds = string.Join(",", duplicateRoleIds);
            if (string.IsNullOrWhiteSpace(duplicateIds))
            {
                return;
            }

            var sql = $@"
INSERT INTO [UserRole] ([UserId], [RoleId])
SELECT ur.[UserId], {keepRoleId}
FROM [UserRole] ur
WHERE ur.[RoleId] IN ({duplicateIds})
  AND NOT EXISTS (
      SELECT 1 FROM [UserRole] existing
      WHERE existing.[UserId] = ur.[UserId] AND existing.[RoleId] = {keepRoleId});

DELETE FROM [UserRole] WHERE [RoleId] IN ({duplicateIds});

INSERT INTO [RolePermission] ([RoleId], [PermissionId], [CreationTime], [CreatorId])
SELECT {keepRoleId}, rp.[PermissionId], COALESCE(rp.[CreationTime], GETDATE()), COALESCE(rp.[CreatorId], 0)
FROM [RolePermission] rp
WHERE rp.[RoleId] IN ({duplicateIds})
  AND NOT EXISTS (
      SELECT 1 FROM [RolePermission] existing
      WHERE existing.[RoleId] = {keepRoleId} AND existing.[PermissionId] = rp.[PermissionId]);

DELETE FROM [RolePermission] WHERE [RoleId] IN ({duplicateIds});
DELETE FROM [RoleOrganizationUnit] WHERE [RoleId] IN ({duplicateIds});
DELETE FROM [Role] WHERE [Id] IN ({duplicateIds});";

            await _db.Ado.ExecuteCommandAsync(sql);
        }

        private async Task<List<Permission>> EnsureSeedPermissionsAsync()
        {
            var seedPermissions = CreateSeedPermissions();
            var existingPermissions = await _db.Queryable<Permission>().ToListAsync();

            foreach (var seedPermission in seedPermissions)
            {
                var existingPermission = existingPermissions.FirstOrDefault(p =>
                    string.Equals(p.ProviderKey, seedPermission.ProviderKey, StringComparison.OrdinalIgnoreCase));

                if (existingPermission == null)
                {
                    seedPermission.Id = await _db.Insertable(seedPermission).ExecuteReturnIdentityAsync();
                    existingPermissions.Add(seedPermission);
                    continue;
                }

                var shouldUpdate = false;
                if (existingPermission.DisplyName != seedPermission.DisplyName)
                {
                    existingPermission.DisplyName = seedPermission.DisplyName;
                    shouldUpdate = true;
                }

                if (existingPermission.ProviderId != seedPermission.ProviderId)
                {
                    existingPermission.ProviderId = seedPermission.ProviderId;
                    shouldUpdate = true;
                }

                if (!existingPermission.IsEnabled)
                {
                    existingPermission.IsEnabled = true;
                    shouldUpdate = true;
                }

                if (shouldUpdate)
                {
                    await _db.Updateable(existingPermission).ExecuteCommandAsync();
                }
            }

            return existingPermissions;
        }

        private static List<Permission> CreateSeedPermissions()
        {
            return new List<Permission>
            {
                new Permission { DisplyName = "驾驶舱", ProviderKey = "Dashboard", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "自检", ProviderKey = "SelfCheck", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "生产信息", ProviderKey = "Production", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "生产记录", ProviderKey = "ProductionRecord", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "报警记录", ProviderKey = "AlarmRecord", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "审计追踪", ProviderKey = "AuditRecord", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "后台管理", ProviderKey = "AdminSettingView", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "用户管理", ProviderKey = "UserManagement", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "角色管理", ProviderKey = "RoleManagement", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "权限管理", ProviderKey = "PermissionManagement", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "组织管理", ProviderKey = "OrganizationManagement", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now },
                new Permission { DisplyName = "审计日志", ProviderKey = "AuditLog", ProviderId = MenuPermissionProviderId, IsEnabled = true, CreationTime = DateTime.Now }
            };
        }

        private static bool IsInvalidSeedText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) || value.Trim().All(c => c == '?');
        }
    }
}
