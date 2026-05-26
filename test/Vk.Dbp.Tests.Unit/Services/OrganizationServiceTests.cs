using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;
using OrganizationUnitEntity = Dabp.Infrastructure.Entities.OrganizationUnit;
using UserEntity = Dabp.Infrastructure.Entities.User;
using UserOrganizationUnitEntity = Dabp.Infrastructure.Entities.UserOrganizationUnit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class OrganizationServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly OrganizationService _organizationService;

    public OrganizationServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();

        var auditLogService = new Mock<IAuditLogService>();
        var userSession = new Mock<IUserSession>();

        userSession.SetupGet(x => x.IsLoggedIn).Returns(true);
        userSession.SetupGet(x => x.UserId).Returns(99);
        userSession.SetupGet(x => x.Username).Returns("tester");
        SetupAuditLogService(auditLogService);

        _organizationService = new OrganizationService(_db, auditLogService.Object, userSession.Object);
    }

    [Fact]
    public async Task GetAllOrganizationUnitsAsync_ReturnsOrganizationsOrderedByCode()
    {
        SeedOrganization(id: 1, displayName: "Second", code: "B", parentId: 0);
        SeedOrganization(id: 2, displayName: "First", code: "A", parentId: 0);

        List<OrganizationUnitModel> organizations = await _organizationService.GetAllOrganizationUnitsAsync();

        organizations.Select(x => x.Code).Should().Equal("A", "B");
    }

    [Fact]
    public async Task CreateOrganizationUnitAsync_InsertsOrganizationAndAssignsGeneratedId()
    {
        var organization = new OrganizationUnitModel
        {
            DisplayName = "Workshop",
            Code = "WS",
            ParentId = 0
        };

        bool result = await _organizationService.CreateOrganizationUnitAsync(organization);

        result.Should().BeTrue();
        organization.Id.Should().BeGreaterThan(0);

        OrganizationUnitEntity? entity = await _db.Queryable<OrganizationUnitEntity>()
            .FirstAsync(x => x.Code == "WS");
        entity.Should().NotBeNull();
        entity!.DisplyName.Should().Be("Workshop");
    }

    [Fact]
    public async Task UpdateOrganizationUnitAsync_UpdatesEditableFields()
    {
        SeedOrganization(id: 1, displayName: "Old", code: "OLD", parentId: 0);

        bool result = await _organizationService.UpdateOrganizationUnitAsync(new OrganizationUnitModel
        {
            Id = 1,
            DisplayName = "New",
            Code = "NEW",
            ParentId = 2
        });

        result.Should().BeTrue();

        OrganizationUnitEntity entity = await _db.Queryable<OrganizationUnitEntity>().FirstAsync(x => x.Id == 1);
        entity.DisplyName.Should().Be("New");
        entity.Code.Should().Be("NEW");
        entity.ParentId.Should().Be(2);
        entity.LastModificationTime.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildOrganizationTreeAsync_ReturnsRootNodesWithChildren()
    {
        SeedOrganization(id: 1, displayName: "Root", code: "001", parentId: 0);
        SeedOrganization(id: 2, displayName: "Child", code: "001001", parentId: 1);
        SeedOrganization(id: 3, displayName: "Grandchild", code: "001001001", parentId: 2);

        List<OrganizationUnitModel> roots = await _organizationService.BuildOrganizationTreeAsync();

        roots.Should().ContainSingle();
        roots[0].DisplayName.Should().Be("Root");
        roots[0].Children.Should().ContainSingle();
        roots[0].Children[0].DisplayName.Should().Be("Child");
        roots[0].Children[0].Children.Should().ContainSingle();
        roots[0].Children[0].Children[0].DisplayName.Should().Be("Grandchild");
    }

    [Fact]
    public async Task AssignUsersToOrganizationAsync_AddsMissingLinksWithoutDuplicatingExistingLinks()
    {
        SeedOrganization(id: 1, displayName: "Workshop", code: "WS", parentId: 0);
        SeedUser(id: 10, username: "alice", realName: "Alice");
        SeedUser(id: 20, username: "bob", realName: "Bob");
        SeedUserOrganization(userId: 10, organizationUnitId: 1);

        bool result = await _organizationService.AssignUsersToOrganizationAsync(1, new List<int> { 10, 20 });

        result.Should().BeTrue();
        List<int> userIds = await _db.Queryable<UserOrganizationUnitEntity>()
            .Where(x => x.OrganizationUnitId == 1)
            .Select(x => x.UserId)
            .ToListAsync();
        userIds.Should().BeEquivalentTo(new[] { 10, 20 });
    }

    [Fact]
    public async Task GetOrganizationUsersAsync_ReturnsNonDeletedUsersAssignedToOrganization()
    {
        SeedOrganization(id: 1, displayName: "Workshop", code: "WS", parentId: 0);
        SeedUser(id: 10, username: "alice", realName: "Alice");
        SeedUser(id: 20, username: "deleted", realName: "Deleted", isDeleted: true);
        SeedUserOrganization(userId: 10, organizationUnitId: 1);
        SeedUserOrganization(userId: 20, organizationUnitId: 1);

        List<User> users = await _organizationService.GetOrganizationUsersAsync(1);

        users.Should().ContainSingle();
        users[0].Username.Should().Be("alice");
        users[0].RealName.Should().Be("Alice");
    }

    [Fact]
    public async Task RemoveUserFromOrganizationAsync_RemovesMatchingLink()
    {
        SeedUserOrganization(userId: 10, organizationUnitId: 1);

        bool result = await _organizationService.RemoveUserFromOrganizationAsync(1, 10);

        result.Should().BeTrue();
        bool exists = await _db.Queryable<UserOrganizationUnitEntity>()
            .AnyAsync(x => x.OrganizationUnitId == 1 && x.UserId == 10);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserOrganizationsAsync_ReturnsOrganizationsAssignedToUser()
    {
        SeedOrganization(id: 1, displayName: "Workshop", code: "WS", parentId: 0);
        SeedOrganization(id: 2, displayName: "Warehouse", code: "WH", parentId: 0);
        SeedUserOrganization(userId: 10, organizationUnitId: 1);
        SeedUserOrganization(userId: 10, organizationUnitId: 2);

        List<OrganizationUnitModel> organizations = await _organizationService.GetUserOrganizationsAsync(10);

        organizations.Select(x => x.Code).Should().BeEquivalentTo(new[] { "WS", "WH" });
    }

    [Fact]
    public async Task DeleteOrganizationUnitAsync_RemovesOrganizationAndUserLinksWhenItHasNoChildren()
    {
        SeedOrganization(id: 1, displayName: "Workshop", code: "WS", parentId: 0);
        SeedUserOrganization(userId: 10, organizationUnitId: 1);

        bool result = await _organizationService.DeleteOrganizationUnitAsync(1);

        result.Should().BeTrue();
        bool organizationExists = await _db.Queryable<OrganizationUnitEntity>().AnyAsync(x => x.Id == 1);
        bool linkExists = await _db.Queryable<UserOrganizationUnitEntity>().AnyAsync(x => x.OrganizationUnitId == 1);
        organizationExists.Should().BeFalse();
        linkExists.Should().BeFalse();
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
        _db.Deleteable<UserOrganizationUnitEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<UserEntity>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<OrganizationUnitEntity>().Where(_ => true).ExecuteCommand();
    }

    private void SeedOrganization(int id, string displayName, string code, int parentId)
    {
        _db.Insertable(new OrganizationUnitEntity
        {
            Id = id,
            DisplyName = displayName,
            Code = code,
            ParentId = parentId,
            CreationTime = DateTime.Now
        }).ExecuteCommand();
    }

    private void SeedUser(int id, string username, string realName, bool isDeleted = false)
    {
        _db.Insertable(new UserEntity
        {
            Id = id,
            UserName = username,
            SurName = realName,
            PhoneNumber = "13800000000",
            PasswordHash = "hashed-password",
            IsActive = true,
            IsDeleted = isDeleted,
            CreationTime = DateTime.Now
        }).ExecuteCommand();
    }

    private void SeedUserOrganization(int userId, int organizationUnitId)
    {
        _db.Insertable(new UserOrganizationUnitEntity
        {
            UserId = userId,
            OrganizationUnitId = organizationUnitId,
            CreationTime = DateTime.Now,
            CreatorId = 0
        }).ExecuteCommand();
    }
}
