using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Exceptions;
using Dabp.Utils.Security;
using Serilog;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.Contracts.Navigation;

namespace Dabp.Infrastructure
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private const string DefaultAdminUsername = "admin";
        private const string InitialAdminPasswordEnvironmentVariable = "DBP_INITIAL_ADMIN_PASSWORD";
        private const string DefaultAdminDisplayName = "系统管理员";
        private const string AdminRoleName = "管理员";
        private const string UserRoleName = "普通用户";

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

        public void InitializeDatabase()
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
                typeof(Notification),
                typeof(SystemConfig),
                typeof(AlarmRecord),
                typeof(AlarmConfig),
                typeof(Device),
                typeof(DevicePoint),
                typeof(DeviceCommand),
                typeof(PointHistory));
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
                catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
                {
                    Log.Warning(ex, "Failed to ensure Unicode text column");
                }
            }
        }

        public async Task InitializeDataAsync()
        {
            var adminRoleEntity = await EnsureDefaultRoleAsync(AdminRoleName, true, 1);
            var userRoleEntity = await EnsureDefaultRoleAsync(UserRoleName, false, 2);
            var permissions = await EnsureSeedPermissionsAsync();

            var adminUser = await _db.Queryable<User>()
                .Where(u => u.UserName == DefaultAdminUsername && !u.IsDeleted)
                .FirstAsync();
            if (adminUser == null)
            {
                string initialPassword = GetInitialAdminPassword();
                adminUser = new User
                {
                    UserName = DefaultAdminUsername,
                    PasswordHash = _passwordHasher.HashPassword(initialPassword),
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

                Log.Warning(
                    "Default administrator account created - username: {Username}. Configure DBP_INITIAL_ADMIN_PASSWORD and change it immediately after first login.",
                    DefaultAdminUsername);
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

                var existingRolePermissionIds = new HashSet<int>(existingRolePermissions.Select(rp => rp.PermissionId));
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

            if (userRoleEntity != null)
            {
                var existingRolePermissions = await _db.Queryable<RolePermission>()
                    .Where(rp => rp.RoleId == userRoleEntity.Id)
                    .ToListAsync();

                if (existingRolePermissions.Count == 0)
                {
                    var defaultUserPermissionCodes = ShellMenuDefinitions.All
                        .Where(definition => definition.DefaultUserVisible)
                        .Select(definition => definition.PermissionCode)
                        .ToList();

                    var basicPermissions = await _db.Queryable<Permission>()
                        .Where(p => defaultUserPermissionCodes.Contains(p.ProviderKey))
                        .ToListAsync();

                    var rolePermissions = basicPermissions.Select(permission => new RolePermission
                    {
                        RoleId = userRoleEntity.Id,
                        PermissionId = permission.Id,
                        CreationTime = DateTime.Now,
                        CreatorId = adminUser?.Id ?? 0
                    }).ToList();

                    await _db.Insertable(rolePermissions).ExecuteCommandAsync();
                }
            }

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

            await EnsureIndustrialSystemConfigsAsync();
            await EnsureDemoDeviceAsync();
        }

        private async Task EnsureIndustrialSystemConfigsAsync()
        {
            // 上方的 SystemConfig 种子是"整表为空才插入"，已有库永远拿不到新增键；
            // 工业引擎配置必须逐键判存补种，读取端（GetBool/GetIntConfigAsync）自带默认值兜底。
            var seedConfigs = new List<SystemConfig>
            {
                new SystemConfig
                {
                    ConfigKey = SystemConfigKeys.IndustrialEngineEnabled,
                    ConfigValue = "True",
                    Description = "是否启用工业采集引擎",
                    ConfigType = "Boolean",
                    CreatedAt = DateTime.Now
                },
                new SystemConfig
                {
                    ConfigKey = SystemConfigKeys.IndustrialPollIntervalMs,
                    ConfigValue = "1000",
                    Description = "采集轮询间隔（毫秒，设备连接配置可按台覆盖）",
                    ConfigType = "Integer",
                    CreatedAt = DateTime.Now
                },
                new SystemConfig
                {
                    ConfigKey = SystemConfigKeys.IndustrialHistoryEnabled,
                    ConfigValue = "True",
                    Description = "是否启用点位历史落盘",
                    ConfigType = "Boolean",
                    CreatedAt = DateTime.Now
                },
                new SystemConfig
                {
                    ConfigKey = SystemConfigKeys.IndustrialHistoryRetentionDays,
                    ConfigValue = "90",
                    Description = "点位历史保留天数（超期数据每日清理一次）",
                    ConfigType = "Integer",
                    CreatedAt = DateTime.Now
                },
                new SystemConfig
                {
                    ConfigKey = SystemConfigKeys.IndustrialEventThrottleMs,
                    ConfigValue = "500",
                    Description = "实时事件发布节流间隔（毫秒）",
                    ConfigType = "Integer",
                    CreatedAt = DateTime.Now
                }
            };

            var seedKeys = seedConfigs.Select(config => config.ConfigKey).ToHashSet();
            var existingKeys = (await _db.Queryable<SystemConfig>()
                    .Where(config => seedKeys.Contains(config.ConfigKey))
                    .Select(config => config.ConfigKey)
                    .ToListAsync())
                .ToHashSet();

            var missingConfigs = seedConfigs
                .Where(config => !existingKeys.Contains(config.ConfigKey))
                .ToList();
            if (missingConfigs.Count > 0)
            {
                await _db.Insertable(missingConfigs).ExecuteCommandAsync();
            }
        }

        private async Task EnsureDemoDeviceAsync()
        {
            // 首启种入一台内置模拟设备，让实时监控页开箱即有数据。
            // 幂等策略：库里已有任意设备即跳过（用户清空全部设备后下次启动会重新补种演示设备）。
            if (await _db.Queryable<Device>().AnyAsync())
            {
                return;
            }

            var demoDevice = new Device
            {
                Code = "SIM-DEMO-01",
                Name = "模拟演示设备",
                ProtocolType = ProtocolTypes.Simulated,
                ConnectionConfig = "{\"pollIntervalMs\":1000}",
                Description = "内置模拟驱动的演示设备，首启自动创建，用于实时监控页开箱演示",
                IsEnabled = true,
                CreatedAt = DateTime.Now
            };
            demoDevice.Id = await _db.Insertable(demoDevice).ExecuteReturnIdentityAsync();

            var demoPoints = new List<DevicePoint>
            {
                NewDemoPoint(demoDevice.Id, "TEMP-01", "炉膛温度", PointDataType.Double, "sine(20,80,60)", "℃", alarmHigh: 75m, alarmLow: 25m),
                NewDemoPoint(demoDevice.Id, "PRESS-01", "系统压力", PointDataType.Double, "rand(0.4,1.2)", "MPa", alarmLow: 0.5m),
                NewDemoPoint(demoDevice.Id, "SPEED-01", "电机转速", PointDataType.Double, "ramp(0,1500,120)", "rpm"),
                NewDemoPoint(demoDevice.Id, "FLOW-01", "瞬时流量", PointDataType.Double, "pulse(30)", "m³/h"),
                NewDemoPoint(demoDevice.Id, "VALVE-01", "进料阀", PointDataType.Boolean, "pulse(45)"),
                NewDemoPoint(demoDevice.Id, "HUMID-01", "环境湿度", PointDataType.Double, "sine(35,65,90)", "%RH"),
                NewDemoPoint(demoDevice.Id, "LEVEL-01", "料位", PointDataType.Double, "const(62.5)", "%"),
                NewDemoPoint(demoDevice.Id, "POWER-01", "主电机功率", PointDataType.Double, "sine(1.5,4.5,75)", "kW")
            };
            await _db.Insertable(demoPoints).ExecuteCommandAsync();

            var demoCommands = new List<DeviceCommand>
            {
                NewDemoCommand(demoDevice.Id, "CMD-VALVE-ON", "打开进料阀", "VALVE-01", "1"),
                NewDemoCommand(demoDevice.Id, "CMD-VALVE-OFF", "关闭进料阀", "VALVE-01", "0")
            };
            await _db.Insertable(demoCommands).ExecuteCommandAsync();

            Log.Information(
                "Demo device {DeviceCode} seeded with {PointCount} points and {CommandCount} commands",
                demoDevice.Code,
                demoPoints.Count,
                demoCommands.Count);
        }

        private static DevicePoint NewDemoPoint(
            int deviceId,
            string code,
            string name,
            PointDataType dataType,
            string address,
            string? unit = null,
            decimal? alarmHigh = null,
            decimal? alarmLow = null)
        {
            return new DevicePoint
            {
                DeviceId = deviceId,
                Code = code,
                Name = name,
                DataType = dataType,
                Unit = unit,
                Address = address,
                AlarmHigh = alarmHigh,
                AlarmLow = alarmLow,
                IsEnabled = true,
                CreatedAt = DateTime.Now
            };
        }

        private static DeviceCommand NewDemoCommand(int deviceId, string code, string name, string targetPointCode, string writeValue)
        {
            return new DeviceCommand
            {
                DeviceId = deviceId,
                Code = code,
                Name = name,
                TargetPointCode = targetPointCode,
                WriteValue = writeValue,
                IsEnabled = true,
                CreatedAt = DateTime.Now
            };
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
            var duplicateIds = duplicateRoleIds
                .Where(id => id > 0 && id != keepRoleId)
                .Distinct()
                .ToList();
            if (duplicateIds.Count == 0)
            {
                return;
            }

            var committed = false;
            _db.Ado.BeginTran();
            try
            {
                var duplicateUserRoles = await _db.Queryable<UserRole>()
                    .Where(ur => duplicateIds.Contains(ur.RoleId))
                    .ToListAsync();
                var existingKeepUserIds = (await _db.Queryable<UserRole>()
                        .Where(ur => ur.RoleId == keepRoleId)
                        .ToListAsync())
                    .Select(ur => ur.UserId)
                    .ToHashSet();

                var userRolesToInsert = duplicateUserRoles
                    .Where(ur => !existingKeepUserIds.Contains(ur.UserId))
                    .GroupBy(ur => ur.UserId)
                    .Select(group => new UserRole
                    {
                        UserId = group.Key,
                        RoleId = keepRoleId
                    })
                    .ToList();
                if (userRolesToInsert.Count > 0)
                {
                    await _db.Insertable(userRolesToInsert).ExecuteCommandAsync();
                }

                await _db.Deleteable<UserRole>()
                    .Where(ur => duplicateIds.Contains(ur.RoleId))
                    .ExecuteCommandAsync();

                var duplicateRolePermissions = await _db.Queryable<RolePermission>()
                    .Where(rp => duplicateIds.Contains(rp.RoleId))
                    .ToListAsync();
                var existingKeepPermissionIds = (await _db.Queryable<RolePermission>()
                        .Where(rp => rp.RoleId == keepRoleId)
                        .ToListAsync())
                    .Select(rp => rp.PermissionId)
                    .ToHashSet();

                var rolePermissionsToInsert = duplicateRolePermissions
                    .Where(rp => !existingKeepPermissionIds.Contains(rp.PermissionId))
                    .GroupBy(rp => rp.PermissionId)
                    .Select(group =>
                    {
                        var permission = group.First();
                        return new RolePermission
                        {
                            RoleId = keepRoleId,
                            PermissionId = group.Key,
                            CreationTime = permission.CreationTime == default ? DateTime.Now : permission.CreationTime,
                            CreatorId = permission.CreatorId
                        };
                    })
                    .ToList();
                if (rolePermissionsToInsert.Count > 0)
                {
                    await _db.Insertable(rolePermissionsToInsert).ExecuteCommandAsync();
                }

                await _db.Deleteable<RolePermission>()
                    .Where(rp => duplicateIds.Contains(rp.RoleId))
                    .ExecuteCommandAsync();
                await _db.Deleteable<RoleOrganizationUnit>()
                    .Where(rou => duplicateIds.Contains(rou.RoleId))
                    .ExecuteCommandAsync();
                await _db.Deleteable<Role>()
                    .Where(role => duplicateIds.Contains(role.Id))
                    .ExecuteCommandAsync();

                _db.Ado.CommitTran();
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    _db.Ado.RollbackTran();
                }
            }
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
            return ShellMenuDefinitions.All
                .Select(definition => new Permission
                {
                    DisplyName = definition.DisplayName,
                    ParentName = string.Empty,
                    ProviderKey = definition.PermissionCode,
                    ProviderId = definition.ProviderId,
                    IsEnabled = true,
                    CreationTime = DateTime.Now
                })
                .ToList();
        }

        private static bool IsInvalidSeedText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) || value.Trim().All(c => c == '?');
        }

        private static string GetInitialAdminPassword()
        {
            string? configuredPassword = Environment.GetEnvironmentVariable(InitialAdminPasswordEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(configuredPassword))
            {
                throw new InvalidOperationException(
                    $"Missing initial administrator password. Set {InitialAdminPasswordEnvironmentVariable} before first database initialization.");
            }

            return configuredPassword;
        }
    }
}
