using Dabp.Infrastructure;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Security;
using FluentAssertions;
using SqlSugar;
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
