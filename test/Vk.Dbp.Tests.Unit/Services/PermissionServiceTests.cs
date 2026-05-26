using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;
using PermissionEntity = Dabp.Infrastructure.Entities.Permission;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;
using RolePermissionEntity = Dabp.Infrastructure.Entities.RolePermission;
using UserRoleEntity = Dabp.Infrastructure.Entities.UserRole;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class PermissionServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();

        var auditLogService = new Mock<IAuditLogService>();
        var userSession = new Mock<IUserSession>();
        _permissionService = new PermissionService(_db, auditLogService.Object, userSession.Object);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsEnabledPermissionsAssignedThroughUserRoles()
    {
        SeedUserRole(userId: 10, roleId: 20);
        SeedPermission(id: 1, name: "查看用户", code: "user:view", isEnabled: true);
        SeedPermission(id: 2, name: "编辑用户", code: "user:edit", isEnabled: false);
        SeedRolePermission(roleId: 20, permissionId: 1);
        SeedRolePermission(roleId: 20, permissionId: 2);

        List<PermissionModel> permissions = await _permissionService.GetUserPermissionsAsync(10);

        permissions.Should().ContainSingle();
        permissions[0].Code.Should().Be("user:view");
        permissions[0].Name.Should().Be("查看用户");
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsEmptyListWhenUserHasNoRoles()
    {
        SeedPermission(id: 1, name: "查看用户", code: "user:view", isEnabled: true);

        List<PermissionModel> permissions = await _permissionService.GetUserPermissionsAsync(10);

        permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsTrueWhenAssignedPermissionIsEnabled()
    {
        SeedUserRole(userId: 10, roleId: 20);
        SeedPermission(id: 1, name: "查看用户", code: "user:view", isEnabled: true);
        SeedRolePermission(roleId: 20, permissionId: 1);

        bool result = await _permissionService.HasPermissionAsync(10, "user:view");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ReturnsFalseWhenPermissionIsNotAssigned()
    {
        SeedUserRole(userId: 10, roleId: 20);
        SeedPermission(id: 1, name: "查看用户", code: "user:view", isEnabled: true);
        SeedRolePermission(roleId: 20, permissionId: 1);

        bool result = await _permissionService.HasPermissionAsync(10, "admin:delete");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ReturnsAllPermissionsWithEnabledState()
    {
        SeedPermission(id: 1, name: "查看用户", code: "user:view", isEnabled: true);
        SeedPermission(id: 2, name: "编辑用户", code: "user:edit", isEnabled: false);

        List<PermissionModel> permissions = await _permissionService.GetAllPermissionsAsync();

        permissions.Should().HaveCount(2);
        permissions.Should().Contain(p => p.Code == "user:view" && p.IsEnabled);
        permissions.Should().Contain(p => p.Code == "user:edit" && !p.IsEnabled);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_ReturnsMappedPermission()
    {
        SeedPermission(id: 1, name: "查看用户", code: "user:view", isEnabled: true);

        PermissionModel? permission = await _permissionService.GetPermissionByIdAsync(1);

        permission.Should().NotBeNull();
        permission!.Name.Should().Be("查看用户");
        permission.Code.Should().Be("user:view");
        permission.Type.Should().Be(PermissionType.Menu);
    }

    private void ResetDatabase()
    {
        _db.Deleteable<UserRoleEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<RolePermissionEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<PermissionEntity>().Where(_ => true).ExecuteCommand();
    }

    private void SeedUserRole(int userId, int roleId)
    {
        _db.Insertable(new UserRoleEntity
        {
            UserId = userId,
            RoleId = roleId
        }).ExecuteCommand();
    }

    private void SeedRolePermission(int roleId, int permissionId)
    {
        _db.Insertable(new RolePermissionEntity
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreationTime = DateTime.Now,
            CreatorId = 0
        }).ExecuteCommand();
    }

    private void SeedPermission(int id, string name, string code, bool isEnabled)
    {
        _db.Insertable(new PermissionEntity
        {
            Id = id,
            DisplyName = name,
            ParentName = string.Empty,
            ProviderKey = code,
            ProviderId = (int)PermissionType.Menu,
            IsEnabled = isEnabled,
            CreationTime = DateTime.Now
        }).ExecuteCommand();
    }
}
