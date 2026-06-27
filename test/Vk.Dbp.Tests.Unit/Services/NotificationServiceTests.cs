using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class NotificationServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly UserSession _userSession = new();

    public NotificationServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();
        _userSession.Login(1, "admin", "管理员", "", "", "");
        SetupAuditLogService();
    }

    [Fact]
    public async Task NotificationService_PersistsAcrossServiceInstances()
    {
        var firstService = CreateService();
        var createResult = await firstService.CreateNotificationAsync(new Notification
        {
            Title = "Test",
            Content = "Persist me",
            Type = "Info",
            UserId = 1,
            IsRead = false
        });

        createResult.Should().BeTrue();

        var secondService = CreateService();
        List<Notification> notifications = await secondService.GetNotificationsByUserIdAsync(1);

        notifications.Should().ContainSingle();
        notifications[0].Title.Should().Be("Test");
        notifications[0].Content.Should().Be("Persist me");
    }

    [Fact]
    public async Task NotificationService_SupportsReadCountAndDelete()
    {
        var service = CreateService();

        await service.CreateNotificationAsync(new Notification
        {
            Title = "A",
            Content = "A1",
            Type = "Info",
            UserId = 2,
            IsRead = false
        });

        await service.CreateNotificationAsync(new Notification
        {
            Title = "B",
            Content = "B1",
            Type = "Warning",
            UserId = 2,
            IsRead = false
        });

        int unreadCount = await service.GetUnreadCountAsync(2);
        unreadCount.Should().Be(2);

        List<Notification> notifications = await service.GetNotificationsByUserIdAsync(2);
        Notification notificationToMark = notifications.First(n => n.Title == "A");
        Notification notificationToDelete = notifications.First(n => n.Title == "B");

        bool markResult = await service.MarkAsReadAsync(notificationToMark.Id);
        markResult.Should().BeTrue();

        unreadCount = await service.GetUnreadCountAsync(2);
        unreadCount.Should().Be(1);

        bool deleteResult = await service.DeleteNotificationAsync(notificationToDelete.Id);
        deleteResult.Should().BeTrue();

        notifications = await service.GetNotificationsByUserIdAsync(2);
        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Be("A");
    }

    private void ResetDatabase()
    {
        _db.Deleteable<Dabp.Infrastructure.Entities.Notification>().Where(_ => true).ExecuteCommand();
    }

    private NotificationService CreateService()
    {
        return new NotificationService(_db, _auditLogService.Object, _userSession);
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
}
