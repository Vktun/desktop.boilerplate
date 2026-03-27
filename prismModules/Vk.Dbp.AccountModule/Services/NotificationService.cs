using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    public class NotificationService : INotificationService
    {
        private readonly List<Notification> _notifications = new();
        private int _nextNotificationId = 1;

        public NotificationService()
        {
            InitializeSampleData();
        }

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            return await Task.FromResult(_notifications.OrderByDescending(n => n.CreatedTime).ToList());
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            return await Task.FromResult(
                _notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedTime)
                    .ToList());
        }

        public async Task<Notification> GetNotificationByIdAsync(int id)
        {
            return await Task.FromResult(_notifications.FirstOrDefault(n => n.Id == id));
        }

        public async Task<bool> CreateNotificationAsync(Notification notification)
        {
            try
            {
                notification.Id = _nextNotificationId++;
                notification.CreatedTime = DateTime.Now;
                notification.IsRead = false;
                _notifications.Add(notification);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateNotificationAsync(Notification notification)
        {
            try
            {
                var existingNotification = _notifications.FirstOrDefault(n => n.Id == notification.Id);
                if (existingNotification == null)
                    return false;

                existingNotification.Title = notification.Title;
                existingNotification.Content = notification.Content;
                existingNotification.Type = notification.Type;
                existingNotification.IsRead = notification.IsRead;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            try
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == id);
                if (notification == null)
                    return false;

                _notifications.Remove(notification);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == id);
            if (notification == null)
                return false;

            notification.IsRead = true;
            return await Task.FromResult(true);
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var userNotifications = _notifications.Where(n => n.UserId == userId && !n.IsRead);
            foreach (var notification in userNotifications)
            {
                notification.IsRead = true;
            }
            return await Task.FromResult(true);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead));
        }

        private void InitializeSampleData()
        {
            _notifications.AddRange(new[]
            {
                new Notification
                {
                    Id = _nextNotificationId++,
                    Title = "系统通知",
                    Content = "欢迎使用桌面应用程序！",
                    Type = "System",
                    IsRead = false,
                    CreatedTime = DateTime.Now.AddHours(-2),
                    UserId = 1
                },
                new Notification
                {
                    Id = _nextNotificationId++,
                    Title = "安全提醒",
                    Content = "您的密码已超过30天未更新，建议及时修改密码。",
                    Type = "Warning",
                    IsRead = false,
                    CreatedTime = DateTime.Now.AddHours(-1),
                    UserId = 1
                },
                new Notification
                {
                    Id = _nextNotificationId++,
                    Title = "更新通知",
                    Content = "系统已更新至最新版本 v2.0.0",
                    Type = "Info",
                    IsRead = true,
                    CreatedTime = DateTime.Now.AddDays(-1),
                    UserId = 1
                },
                new Notification
                {
                    Id = _nextNotificationId++,
                    Title = "任务完成",
                    Content = "数据备份任务已完成",
                    Type = "Success",
                    IsRead = false,
                    CreatedTime = DateTime.Now.AddMinutes(-30),
                    UserId = 1
                }
            });
        }
    }
}
