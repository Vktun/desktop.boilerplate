using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;
using PermissionEntity = Dabp.Infrastructure.Entities.Permission;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using RoleModel = Vk.Dbp.AccountModule.Models.Role;
using RolePermissionEntity = Dabp.Infrastructure.Entities.RolePermission;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class RoleServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly RoleService _roleService;

    public RoleServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();

        var auditLogService = new Mock<IAuditLogService>();
        var userSession = new Mock<IUserSession>();

        userSession.SetupGet(x => x.IsLoggedIn).Returns(true);
        userSession.SetupGet(x => x.UserId).Returns(99);
        userSession.SetupGet(x => x.Username).Returns("tester");
        SetupAuditLogService(auditLogService);

        _roleService = new RoleService(_db, auditLogService.Object, userSession.Object);
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsMappedRolesWithEnabledState()
    {
        SeedRole(id: 1, name: "Admin", roleLevel: 1);
        SeedRole(id: 2, name: "Disabled", roleLevel: 0);

        List<RoleModel> roles = await _roleService.GetAllRolesAsync();

        roles.Should().HaveCount(2);
        roles.Should().Contain(x => x.Name == "Admin" && x.IsEnabled);
        roles.Should().Contain(x => x.Name == "Disabled" && !x.IsEnabled);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ReturnsMappedRole()
    {
        SeedRole(id: 1, name: "Admin", roleLevel: 1);

        RoleModel? role = await _roleService.GetRoleByIdAsync(1);

        role.Should().NotBeNull();
        role!.Name.Should().Be("Admin");
        role.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoleAsync_InsertsRoleAndAssignsGeneratedId()
    {
        var role = new RoleModel
        {
            Name = "Operator",
            IsEnabled = true
        };

        bool result = await _roleService.CreateRoleAsync(role);

        result.Should().BeTrue();
        role.Id.Should().BeGreaterThan(0);

        RoleEntity? entity = await _db.Queryable<RoleEntity>()
            .FirstAsync(x => x.Name == "Operator");
        entity.Should().NotBeNull();
        entity!.RoleLevel.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRoleAsync_UpdatesRoleName()
    {
        SeedRole(id: 1, name: "Old", roleLevel: 1);

        bool result = await _roleService.UpdateRoleAsync(new RoleModel
        {
            Id = 1,
            Name = "New",
            IsEnabled = true
        });

        result.Should().BeTrue();

        RoleEntity entity = await _db.Queryable<RoleEntity>().FirstAsync(x => x.Id == 1);
        entity.Name.Should().Be("New");
    }

    [Fact]
    public async Task DeleteRoleAsync_RemovesRoleAndRolePermissions()
    {
        SeedRole(id: 1, name: "Admin", roleLevel: 1);
        SeedPermission(id: 10, name: "View", code: "user:view", isEnabled: true);
        SeedRolePermission(roleId: 1, permissionId: 10);

        bool result = await _roleService.DeleteRoleAsync(1);

        result.Should().BeTrue();
        bool roleExists = await _db.Queryable<RoleEntity>().AnyAsync(x => x.Id == 1);
        bool rolePermissionExists = await _db.Queryable<RolePermissionEntity>().AnyAsync(x => x.RoleId == 1);
        roleExists.Should().BeFalse();
        rolePermissionExists.Should().BeFalse();
    }

    [Fact]
    public async Task AssignPermissionsToRoleAsync_ReplacesExistingPermissions()
    {
        SeedRole(id: 1, name: "Admin", roleLevel: 1);
        SeedRolePermission(roleId: 1, permissionId: 10);

        bool result = await _roleService.AssignPermissionsToRoleAsync(1, new List<int> { 20, 30 });

        result.Should().BeTrue();
        List<int> permissionIds = await _db.Queryable<RolePermissionEntity>()
            .Where(x => x.RoleId == 1)
            .Select(x => x.PermissionId)
            .ToListAsync();
        permissionIds.Should().BeEquivalentTo(new[] { 20, 30 });
    }

    [Fact]
    public async Task GetRolePermissionsAsync_ReturnsOnlyEnabledPermissions()
    {
        SeedPermission(id: 10, name: "View", code: "user:view", isEnabled: true);
        SeedPermission(id: 20, name: "Edit", code: "user:edit", isEnabled: false);
        SeedRolePermission(roleId: 1, permissionId: 10);
        SeedRolePermission(roleId: 1, permissionId: 20);

        List<PermissionModel> permissions = await _roleService.GetRolePermissionsAsync(1);

        permissions.Should().ContainSingle();
        permissions[0].Code.Should().Be("user:view");
    }

    [Fact]
    public async Task EnableRoleAsync_UpdatesRoleLevel()
    {
        SeedRole(id: 1, name: "Admin", roleLevel: 1);

        bool result = await _roleService.EnableRoleAsync(1, false);

        result.Should().BeTrue();
        RoleEntity entity = await _db.Queryable<RoleEntity>().FirstAsync(x => x.Id == 1);
        entity.RoleLevel.Should().Be(0);
    }

    private static void SetupAuditLogService(Mock<IAuditLogService> auditLogService)
    {
        auditLogService
            .Setup(x => x.LogOperationAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<AuditActionType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);

        auditLogService
            .Setup(x => x.LogFailureAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<AuditActionType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private void ResetDatabase()
    {
        _db.Deleteable<RolePermissionEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<PermissionEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<RoleEntity>().Where(_ => true).ExecuteCommand();
    }

    private void SeedRole(int id, string name, int roleLevel)
    {
        _db.Insertable(new RoleEntity
        {
            Id = id,
            Name = name,
            RoleLevel = roleLevel
        }).ExecuteCommand();
    }

    private void SeedPermission(int id, string name, string code, bool isEnabled)
    {
        _db.Insertable(new PermissionEntity
        {
            Id = id,
            DisplyName = name,
            ParentName = string.Empty,
            ProviderId = 1,
            ProviderKey = code,
            IsEnabled = isEnabled,
            CreationTime = DateTime.Now
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
}
