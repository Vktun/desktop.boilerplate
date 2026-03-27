using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetAllNotificationsAsync();

        Task<List<Notification>> GetNotificationsByUserIdAsync(int userId);

        Task<Notification> GetNotificationByIdAsync(int id);

        Task<bool> CreateNotificationAsync(Notification notification);

        Task<bool> UpdateNotificationAsync(Notification notification);

        Task<bool> DeleteNotificationAsync(int id);

        Task<bool> MarkAsReadAsync(int id);

        Task<bool> MarkAllAsReadAsync(int userId);

        Task<int> GetUnreadCountAsync(int userId);
    }
}
