using System;
using System.Threading.Tasks;
using Prism.Events;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 全局通知发布者实现 - 用于跨模块发送通知
    /// </summary>
    public class GlobalNotificationPublisher : IGlobalNotificationPublisher
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly INotificationService _notificationService;
        private readonly IUserSession _userSession;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="notificationService">通知服务（持久化）</param>
        /// <param name="userSession">用户会话</param>
        public GlobalNotificationPublisher(
            IEventAggregator eventAggregator,
            INotificationService notificationService,
            IUserSession userSession)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        /// <summary>
        /// 发布全局广播通知（所有用户可见）
        /// </summary>
        public async Task<int> PublishGlobalAsync(
            string title,
            string content,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? sourceModule = null)
        {
            var payload = new GlobalNotificationPayload
            {
                Title = title,
                Content = content,
                Type = type,
                Priority = priority,
                SourceModule = sourceModule,
                TargetUserId = null, // 广播通知
                CreatedTime = DateTime.Now
            };

            return await PublishAndNotifyAsync(payload, null);
        }

        /// <summary>
        /// 向指定用户发送通知
        /// </summary>
        public async Task<int> PublishToUserAsync(
            int userId,
            string title,
            string content,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? sourceModule = null,
            string? actionUrl = null)
        {
            var payload = new GlobalNotificationPayload
            {
                Title = title,
                Content = content,
                Type = type,
                Priority = priority,
                SourceModule = sourceModule,
                TargetUserId = userId,
                ActionUrl = actionUrl,
                CreatedTime = DateTime.Now
            };

            return await PublishAndNotifyAsync(payload, userId);
        }

        /// <summary>
        /// 向当前登录用户发送通知
        /// </summary>
        public async Task<int> PublishToCurrentUserAsync(
            string title,
            string content,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? sourceModule = null,
            string? actionUrl = null)
        {
            if (!_userSession.IsLoggedIn)
            {
                return 0; // 用户未登录，不发送通知
            }

            return await PublishToUserAsync(
                _userSession.UserId,
                title,
                content,
                type,
                priority,
                sourceModule,
                actionUrl);
        }

        /// <summary>
        /// 发布自定义Payload通知
        /// </summary>
        public async Task<int> PublishCustomAsync(GlobalNotificationPayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            // 确保创建时间已设置
            if (payload.CreatedTime == default)
                payload.CreatedTime = DateTime.Now;

            return await PublishAndNotifyAsync(payload, payload.TargetUserId);
        }

        #region 便捷方法

        /// <summary>
        /// 发布错误通知给当前用户
        /// </summary>
        public async Task<int> PublishErrorAsync(string title, string content, string? sourceModule = null)
        {
            return await PublishToCurrentUserAsync(title, content, NotificationType.Error, NotificationPriority.High, sourceModule);
        }

        /// <summary>
        /// 发布警告通知给当前用户
        /// </summary>
        public async Task<int> PublishWarningAsync(string title, string content, string? sourceModule = null)
        {
            return await PublishToCurrentUserAsync(title, content, NotificationType.Warning, NotificationPriority.Normal, sourceModule);
        }

        /// <summary>
        /// 发布信息通知给当前用户
        /// </summary>
        public async Task<int> PublishInfoAsync(string title, string content, string? sourceModule = null)
        {
            return await PublishToCurrentUserAsync(title, content, NotificationType.Info, NotificationPriority.Normal, sourceModule);
        }

        /// <summary>
        /// 发布成功通知给当前用户
        /// </summary>
        public async Task<int> PublishSuccessAsync(string title, string content, string? sourceModule = null)
        {
            return await PublishToCurrentUserAsync(title, content, NotificationType.Success, NotificationPriority.Normal, sourceModule);
        }

        /// <summary>
        /// 发布系统通知给当前用户
        /// </summary>
        public async Task<int> PublishSystemAsync(string title, string content, string? sourceModule = null)
        {
            return await PublishToCurrentUserAsync(title, content, NotificationType.System, NotificationPriority.Normal, sourceModule);
        }

        #endregion

        /// <summary>
        /// 核心发布逻辑：持久化 + 发布事件
        /// </summary>
        /// <param name="payload">通知Payload</param>
        /// <param name="targetUserId">目标用户ID（null为广播）</param>
        /// <returns>创建的通知ID</returns>
        private async Task<int> PublishAndNotifyAsync(GlobalNotificationPayload payload, int? targetUserId)
        {
            int notificationId = 0;

            // 如果有目标用户，进行持久化存储
            if (targetUserId.HasValue)
            {
                var notification = new Notification
                {
                    Title = payload.Title,
                    Content = payload.Content,
                    Type = ConvertNotificationTypeToString(payload.Type),
                    UserId = targetUserId.Value,
                    IsRead = false,
                    CreatedTime = payload.CreatedTime
                };

                var success = await _notificationService.CreateNotificationAsync(notification);
                if (success)
                {
                    notificationId = notification.Id;
                    payload.Id = notificationId;
                }
            }

            // 发布事件（无论是否持久化都发布，让订阅者处理）
            _eventAggregator.GetEvent<GlobalNotificationEvent>().Publish(payload);

            // 发布数量变更事件（通知更新Badge）
            if (targetUserId.HasValue)
            {
                var unreadCount = await _notificationService.GetUnreadCountAsync(targetUserId.Value);
                _eventAggregator.GetEvent<NotificationCountChangedEvent>().Publish(
                    new NotificationCountChangedPayload
                    {
                        UserId = targetUserId.Value,
                        UnreadCount = unreadCount,
                        ChangedTime = DateTime.Now
                    });
            }

            return notificationId;
        }

        /// <summary>
        /// 将 NotificationType 枚举转换为字符串（兼容现有 Notification 模型）
        /// </summary>
        private string ConvertNotificationTypeToString(NotificationType type)
        {
            return type.ToString();
        }
    }
}