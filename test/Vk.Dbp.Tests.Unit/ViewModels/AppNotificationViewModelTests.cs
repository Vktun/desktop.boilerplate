using FluentAssertions;
using Moq;
using Prism.Events;
using Vk.Dbp.Contracts.Models;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;
using Vk.Dbp.WpfWindow.ViewModels;
using Xunit;

namespace Vk.Dbp.Tests.Unit.ViewModels;

public class AppNotificationViewModelTests
{
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IUserSession> _userSession = new();

    private AppNotificationViewModel CreateViewModel(bool isLoggedIn = true)
    {
        _userSession.SetupGet(x => x.IsLoggedIn).Returns(isLoggedIn);
        _userSession.SetupGet(x => x.UserId).Returns(1);

        // VM 构造器以 ThreadOption.UIThread 订阅，要求事件上已捕获 SynchronizationContext，
        // 因此在真实 EventAggregator 解析事件前给测试线程安装一个同步上下文。
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        try
        {
            return new AppNotificationViewModel(
                _notificationService.Object,
                _userSession.Object,
                new EventAggregator());
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [Fact]
    public void LoadNotifications_ResultExceedsCap_CapsDisplayListAndKeepsFullUnreadCount()
    {
        // 前 200 条已读、尾部 50 条未读：截断恰好丢掉未读项，验证徽标仍反映完整结果集
        var notifications = Enumerable.Range(1, 250)
            .Select(i => new Notification
            {
                Id = i,
                Title = $"通知 {i}",
                Content = $"内容 {i}",
                Type = "Info",
                IsRead = i <= 200,
                CreatedTime = DateTime.Now.AddMinutes(-i),
                UserId = 1
            })
            .ToList();
        _notificationService
            .Setup(x => x.GetNotificationsByUserIdAsync(1))
            .ReturnsAsync(notifications);
        var viewModel = CreateViewModel();

        viewModel.LoadCommand.Execute();

        viewModel.Notifications.Should().HaveCount(
            AppNotificationViewModel.MaxDisplayedNotifications,
            "通知中心 VM 已注册为单例，展示列表必须有上限避免常驻集合无限增长");
        viewModel.Notifications[0].Id.Should().Be(1, "应保留结果集中最新的前 N 条");
        viewModel.UnreadCount.Should().Be(50, "未读徽标应反映完整结果集而非截断后的列表");
        viewModel.IsLoading.Should().BeFalse("加载完成后应复位加载状态");
    }

    [Fact]
    public void LoadNotifications_LoggedOut_ClearsListAndUnreadCount()
    {
        var viewModel = CreateViewModel(isLoggedIn: false);

        viewModel.LoadCommand.Execute();

        viewModel.Notifications.Should().BeEmpty("登出后不应残留任何通知");
        viewModel.UnreadCount.Should().Be(0, "登出后未读计数应清零");
        _notificationService.Verify(
            x => x.GetNotificationsByUserIdAsync(It.IsAny<int>()),
            Times.Never,
            "登出状态下不应触发数据库查询");
    }
}
