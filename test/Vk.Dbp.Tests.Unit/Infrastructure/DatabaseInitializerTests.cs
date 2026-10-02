using Dabp.Infrastructure;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;
using FluentAssertions;
using SqlSugar;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Infrastructure;

public sealed class DatabaseInitializerTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly DatabaseInitializer _initializer;

    public DatabaseInitializerTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        _initializer = new DatabaseInitializer(_db, new PasswordHasher());
        ResetDatabase();
    }

    [Fact]
    public async Task InitializeDataAsync_SeedsDemoDeviceAndIndustrialConfigsIdempotently()
    {
        SeedUser(id: 1, username: "admin");

        await _initializer.InitializeDataAsync();
        await _initializer.InitializeDataAsync();

        var deviceCount = await _db.Queryable<Device>().CountAsync();
        deviceCount.Should().Be(1, "演示设备种子应幂等，重复初始化不应重复创建");

        var pointCount = await _db.Queryable<DevicePoint>().CountAsync();
        pointCount.Should().Be(8, "演示设备应带 8 个点位");

        var commandCount = await _db.Queryable<DeviceCommand>().CountAsync();
        commandCount.Should().Be(2, "演示设备应带 2 条命令");

        var deviceCode = await _db.Queryable<Device>().Select(entity => entity.Code).FirstAsync();
        deviceCode.Should().Be("SIM-DEMO-01", "演示设备编码应为 SIM-DEMO-01");

        var industrialConfigKeys = await _db.Queryable<SystemConfig>()
            .Where(config => config.ConfigKey.StartsWith("Industrial."))
            .Select(config => config.ConfigKey)
            .ToListAsync();
        industrialConfigKeys.Should().HaveCount(5, "工业引擎配置键应逐键补种且不重复");
    }

    [Fact]
    public async Task InitializeDataAsync_DoesNotReseedDemoDeviceWhenUserDevicesExist()
    {
        SeedUser(id: 1, username: "admin");
        _db.Insertable(new Device
        {
            Code = "USER-DEVICE-01",
            Name = "用户自建设备",
            ProtocolType = ProtocolTypes.Simulated,
            IsEnabled = true,
            CreatedAt = DateTime.Now
        }).ExecuteCommand();

        await _initializer.InitializeDataAsync();

        var deviceCount = await _db.Queryable<Device>().CountAsync();
        deviceCount.Should().Be(1, "库里已有设备时不应再种演示设备");
    }

    [Fact]
    public async Task InitializeDataAsync_MergesDuplicateDefaultRolesWithoutRawSql()
    {
        SeedRole(id: 1, name: "管理员", isDefault: true, roleLevel: 1);
        SeedRole(id: 2, name: "?", isDefault: true, roleLevel: 1);
        SeedUser(id: 1, username: "admin");
        SeedUser(id: 2, username: "operator");
        SeedUserRole(userId: 2, roleId: 2);
        SeedRolePermission(roleId: 2, permissionId: 100);

        await _initializer.InitializeDataAsync();

        var roles = await _db.Queryable<Role>()
            .Where(role => role.IsDefault && role.RoleLevel == 1)
            .ToListAsync();
        roles.Should().ContainSingle(role => role.Name == "管理员");
        roles.Should().NotContain(role => role.Id == 2);

        var operatorRole = await _db.Queryable<UserRole>()
            .FirstAsync(role => role.UserId == 2 && role.RoleId == 1);
        operatorRole.Should().NotBeNull();

        var migratedPermission = await _db.Queryable<RolePermission>()
            .FirstAsync(permission => permission.RoleId == 1 && permission.PermissionId == 100);
        migratedPermission.Should().NotBeNull();

        var duplicateRelations = await _db.Queryable<UserRole>()
            .Where(role => role.RoleId == 2)
            .CountAsync();
        duplicateRelations.Should().Be(0);
    }

    private void ResetDatabase()
    {
        // 子表先删，避免外键语义下的删除顺序问题（DeviceCommand/DevicePoint → PointHistory → Device）
        _db.Deleteable<DeviceCommand>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<DevicePoint>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<PointHistory>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Device>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<UserRole>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<RolePermission>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<RoleOrganizationUnit>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<AlarmConfig>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<SystemConfig>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Permission>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<User>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Role>().Where(_ => true).ExecuteCommand();
    }

    private void SeedRole(int id, string name, bool isDefault, int roleLevel)
    {
        _db.Insertable(new Role
        {
            Id = id,
            Name = name,
            IsDefault = isDefault,
            RoleLevel = roleLevel
        }).ExecuteCommand();
    }

    private void SeedUser(int id, string username)
    {
        _db.Insertable(new User
        {
            Id = id,
            UserName = username,
            PasswordHash = "hash",
            SurName = username,
            PhoneNumber = "13800138000",
            IsActive = true,
            ChangePasswordLastTime = DateTime.Now,
            ValideDays = 90,
            CreationTime = DateTime.Now,
            CreatorId = 0,
            IsDeleted = false
        }).ExecuteCommand();
    }

    private void SeedUserRole(int userId, int roleId)
    {
        _db.Insertable(new UserRole
        {
            UserId = userId,
            RoleId = roleId
        }).ExecuteCommand();
    }

    private void SeedRolePermission(int roleId, int permissionId)
    {
        _db.Insertable(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreationTime = DateTime.Now,
            CreatorId = 0
        }).ExecuteCommand();
    }
}
