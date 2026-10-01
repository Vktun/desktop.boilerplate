using System.Collections.Generic;
using System.Threading.Tasks;
using Vk.Dbp.Contracts.Models;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// 通知服务契约（实现位于 AccountModule；shell 通知中心与跨模块通知发布共用）
    /// </summary>
    public interface INotificationService
    {
        /// <summary>获取全部通知</summary>
        Task<List<Notification>> GetAllNotificationsAsync();

        /// <summary>获取指定用户的通知列表</summary>
        Task<List<Notification>> GetNotificationsByUserIdAsync(int userId);

        /// <summary>按 ID 获取通知</summary>
        Task<Notification?> GetNotificationByIdAsync(int id);

        /// <summary>创建通知</summary>
        Task<bool> CreateNotificationAsync(Notification notification);

        /// <summary>更新通知</summary>
        Task<bool> UpdateNotificationAsync(Notification notification);

        /// <summary>删除通知</summary>
        Task<bool> DeleteNotificationAsync(int id);

        /// <summary>标记通知为已读</summary>
        Task<bool> MarkAsReadAsync(int id);

        /// <summary>标记指定用户全部通知为已读</summary>
        Task<bool> MarkAllAsReadAsync(int userId);

        /// <summary>获取指定用户未读通知数</summary>
        Task<int> GetUnreadCountAsync(int userId);
    }
}
