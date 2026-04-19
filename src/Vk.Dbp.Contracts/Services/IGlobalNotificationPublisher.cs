using System.Threading.Tasks;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// 全局通知发布者接口 - 用于跨模块发送通知
    /// </summary>
    public interface IGlobalNotificationPublisher
    {
        /// <summary>
        /// 发布全局广播通知（所有用户可见）
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="content">通知内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="priority">通知优先级（默认Normal）</param>
        /// <param name="sourceModule">来源模块标识（可选）</param>
        /// <returns>创建的通知ID</returns>
        Task<int> PublishGlobalAsync(
            string title,
            string content,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? sourceModule = null);

        /// <summary>
        /// 向指定用户发送通知
        /// </summary>
        /// <param name="userId">目标用户ID</param>
        /// <param name="title">通知标题</param>
        /// <param name="content">通知内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="priority">通知优先级（默认Normal）</param>
        /// <param name="sourceModule">来源模块标识（可选）</param>
        /// <param name="actionUrl">点击跳转链接（可选）</param>
        /// <returns>创建的通知ID</returns>
        Task<int> PublishToUserAsync(
            int userId,
            string title,
            string content,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? sourceModule = null,
            string? actionUrl = null);

        /// <summary>
        /// 向当前登录用户发送通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="content">通知内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="priority">通知优先级（默认Normal）</param>
        /// <param name="sourceModule">来源模块标识（可选）</param>
        /// <param name="actionUrl">点击跳转链接（可选）</param>
        /// <returns>创建的通知ID，若用户未登录返回0</returns>
        Task<int> PublishToCurrentUserAsync(
            string title,
            string content,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            string? sourceModule = null,
            string? actionUrl = null);

        /// <summary>
        /// 发布自定义Payload通知（高级用法）
        /// </summary>
        /// <param name="payload">通知Payload</param>
        /// <returns>创建的通知ID</returns>
        Task<int> PublishCustomAsync(GlobalNotificationPayload payload);

        #region 便捷方法

        /// <summary>
        /// 发布错误通知给当前用户
        /// </summary>
        Task<int> PublishErrorAsync(string title, string content, string? sourceModule = null);

        /// <summary>
        /// 发布警告通知给当前用户
        /// </summary>
        Task<int> PublishWarningAsync(string title, string content, string? sourceModule = null);

        /// <summary>
        /// 发布信息通知给当前用户
        /// </summary>
        Task<int> PublishInfoAsync(string title, string content, string? sourceModule = null);

        /// <summary>
        /// 发布成功通知给当前用户
        /// </summary>
        Task<int> PublishSuccessAsync(string title, string content, string? sourceModule = null);

        /// <summary>
        /// 发布系统通知给当前用户
        /// </summary>
        Task<int> PublishSystemAsync(string title, string content, string? sourceModule = null);

        #endregion
    }
}