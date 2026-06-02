using Dabp.Utils.Security;
using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;
using RoleEntity = Dabp.Infrastructure.Entities.Role;
using UserEntity = Dabp.Infrastructure.Entities.User;
using UserModel = Vk.Dbp.AccountModule.Models.User;
using UserRoleEntity = Dabp.Infrastructure.Entities.UserRole;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class UserServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly Mock<IAuditLogService> _auditLogService;
    private readonly Mock<IPasswordHasher> _passwordHasher;
    private readonly UserService _userService;

    public UserServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();

        _auditLogService = new Mock<IAuditLogService>();
        _passwordHasher = new Mock<IPasswordHasher>();
        var userSession = new Mock<IUserSession>();

        userSession.SetupGet(x => x.IsLoggedIn).Returns(true);
        userSession.SetupGet(x => x.UserId).Returns(99);
        userSession.SetupGet(x => x.Username).Returns("tester");
        SetupAuditLogService();

        _userService = new UserService(
            _db,
            _auditLogService.Object,
            _passwordHasher.Object,
            userSession.Object);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ReturnsMappedUserWithRoleIds()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");
        SeedUserRole(userId: 1, roleId: 10);
        SeedUserRole(userId: 1, roleId: 20);

        UserModel? user = await _userService.GetUserByUsernameAsync("alice");

        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
        user.Username.Should().Be("alice");
        user.RealName.Should().Be("Alice");
        user.Email.Should().Be("alice@example.com");
        user.RoleIds.Should().BeEquivalentTo(new[] { 10, 20 });
    }

    [Fact]
    public async Task GetAllUsersAsync_ExcludesSoftDeletedUsers()
    {
        SeedUser(id: 1, username: "active", realName: "Active", phone: "13800000000");
        SeedUser(id: 2, username: "deleted", realName: "Deleted", phone: "13900000000", isDeleted: true);

        List<UserModel> users = await _userService.GetAllUsersAsync();

        users.Should().ContainSingle();
        users[0].Username.Should().Be("active");
    }

    [Fact]
    public async Task GetUsersPagedAsync_FiltersByKeywordAndPreservesRoleIds()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");
        SeedUser(id: 2, username: "bob", realName: "Bob", phone: "13900000000");
        SeedUser(id: 3, username: "charlie", realName: "Charlie", phone: "13700000000", isDeleted: true);
        SeedUserRole(userId: 1, roleId: 10);
        SeedUserRole(userId: 1, roleId: 20);

        var result = await _userService.GetUsersPagedAsync(1, 10, "ali");

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Username.Should().Be("alice");
        result.Items.Single().RoleIds.Should().BeEquivalentTo(new[] { 10, 20 });
    }

    [Fact]
    public async Task GetUsersPagedAsync_SupportsPaging()
    {
        SeedUser(id: 1, username: "u1", realName: "User1", phone: "13800000001");
        SeedUser(id: 2, username: "u2", realName: "User2", phone: "13800000002");
        SeedUser(id: 3, username: "u3", realName: "User3", phone: "13800000003");

        var result = await _userService.GetUsersPagedAsync(2, 2);

        result.TotalCount.Should().Be(3);
        result.PageIndex.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateUserAsync_InsertsUserAndAssignsGeneratedId()
    {
        var user = new UserModel
        {
            Username = "newuser",
            RealName = "New User",
            Phone = "13700000000",
            PasswordHash = "hashed-password"
        };

        bool result = await _userService.CreateUserAsync(user);

        result.Should().BeTrue();
        user.Id.Should().BeGreaterThan(0);

        UserEntity? entity = await _db.Queryable<UserEntity>()
            .FirstAsync(x => x.UserName == "newuser");
        entity.Should().NotBeNull();
        entity!.SurName.Should().Be("New User");
        entity.PhoneNumber.Should().Be("13700000000");
        entity.IsActive.Should().BeTrue();
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesEditableProfileFields()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");

        bool result = await _userService.UpdateUserAsync(new UserModel
        {
            Id = 1,
            Username = "alice",
            RealName = "Alice Updated",
            Phone = "13900000000"
        });

        result.Should().BeTrue();

        UserEntity entity = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == 1);
        entity.SurName.Should().Be("Alice Updated");
        entity.PhoneNumber.Should().Be("13900000000");
        entity.LastModificationTime.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_SoftDeletesUser()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");

        bool result = await _userService.DeleteUserAsync(1);

        result.Should().BeTrue();

        UserEntity entity = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == 1);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletionTime.Should().NotBeNull();
    }

    [Fact]
    public async Task EnableUserAsync_UpdatesActiveState()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");

        bool result = await _userService.EnableUserAsync(1, false);

        result.Should().BeTrue();

        UserEntity entity = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == 1);
        entity.IsActive.Should().BeFalse();
        entity.LastModificationTime.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_UpdatesHashWhenOldPasswordMatches()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000", passwordHash: "old-hash");
        _passwordHasher.Setup(x => x.VerifyPassword("old-password", "old-hash")).Returns(true);
        _passwordHasher.Setup(x => x.HashPassword("new-password")).Returns("new-hash");

        bool result = await _userService.ChangePasswordAsync(1, "old-password", "new-password");

        result.Should().BeTrue();

        UserEntity entity = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == 1);
        entity.PasswordHash.Should().Be("new-hash");
        entity.ChangePasswordLastTime.Should().NotBe(default);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalseWhenOldPasswordDoesNotMatch()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000", passwordHash: "old-hash");
        _passwordHasher.Setup(x => x.VerifyPassword("wrong-password", "old-hash")).Returns(false);

        bool result = await _userService.ChangePasswordAsync(1, "wrong-password", "new-password");

        result.Should().BeFalse();

        UserEntity entity = await _db.Queryable<UserEntity>().FirstAsync(x => x.Id == 1);
        entity.PasswordHash.Should().Be("old-hash");
        _passwordHasher.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_ReplacesExistingRoles()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");
        SeedUserRole(userId: 1, roleId: 10);

        bool result = await _userService.AssignRolesToUserAsync(1, new List<int> { 20, 30 });

        result.Should().BeTrue();

        List<int> roleIds = await _db.Queryable<UserRoleEntity>()
            .Where(x => x.UserId == 1)
            .Select(x => x.RoleId)
            .ToListAsync();
        roleIds.Should().BeEquivalentTo(new[] { 20, 30 });
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsRolesAssignedToUser()
    {
        SeedUser(id: 1, username: "alice", realName: "Alice", phone: "13800000000");
        SeedRole(id: 10, name: "Admin");
        SeedRole(id: 20, name: "Operator");
        SeedUserRole(userId: 1, roleId: 10);
        SeedUserRole(userId: 1, roleId: 20);

        var roles = await _userService.GetUserRolesAsync(1);

        roles.Should().HaveCount(2);
        roles.Select(x => x.Name).Should().BeEquivalentTo(new[] { "Admin", "Operator" });
    }

    private void SetupAuditLogService()
    {
        _auditLogService
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

        _auditLogService
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
        _db.Deleteable<UserRoleEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<RoleEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<UserEntity>().Where(_ => true).ExecuteCommand();
    }

    private void SeedUser(
        int id,
        string username,
        string realName,
        string phone,
        string passwordHash = "hashed-password",
        bool isActive = true,
        bool isDeleted = false)
    {
        _db.Insertable(new UserEntity
        {
            Id = id,
            UserName = username,
            SurName = realName,
            PhoneNumber = phone,
            PasswordHash = passwordHash,
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreationTime = DateTime.Now
        }).ExecuteCommand();
    }

    private void SeedRole(int id, string name)
    {
        _db.Insertable(new RoleEntity
        {
            Id = id,
            Name = name,
            RoleLevel = 1
        }).ExecuteCommand();
    }

    private void SeedUserRole(int userId, int roleId)
    {
        _db.Insertable(new UserRoleEntity
        {
            UserId = userId,
            RoleId = roleId
        }).ExecuteCommand();
    }
}
