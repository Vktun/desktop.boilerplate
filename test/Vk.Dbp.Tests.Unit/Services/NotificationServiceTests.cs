using FluentAssertions;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class NotificationServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;

    public NotificationServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();
    }

    [Fact]
    public async Task NotificationService_PersistsAcrossServiceInstances()
    {
        var firstService = new NotificationService(_db);
        var createResult = await firstService.CreateNotificationAsync(new Notification
        {
            Title = "Test",
            Content = "Persist me",
            Type = "Info",
            UserId = 1,
            IsRead = false
        });

        createResult.Should().BeTrue();

        var secondService = new NotificationService(_db);
        List<Notification> notifications = await secondService.GetNotificationsByUserIdAsync(1);

        notifications.Should().ContainSingle();
        notifications[0].Title.Should().Be("Test");
        notifications[0].Content.Should().Be("Persist me");
    }

    [Fact]
    public async Task NotificationService_SupportsReadCountAndDelete()
    {
        var service = new NotificationService(_db);

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
}
