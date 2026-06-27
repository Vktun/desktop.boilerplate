using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Utils.Exceptions;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using NotificationEntity = Dabp.Infrastructure.Entities.Notification;

namespace Vk.Dbp.AccountModule.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ISqlSugarClient _db;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserSession _userSession;

        public NotificationService(ISqlSugarClient db, IAuditLogService auditLogService, IUserSession userSession)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            List<NotificationEntity> entities = await _db.Queryable<NotificationEntity>()
                .OrderByDescending(n => n.CreatedTime)
                .ToListAsync();

            return entities.Select(MapToModel).ToList();
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            List<NotificationEntity> entities = await _db.Queryable<NotificationEntity>()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedTime)
                .ToListAsync();

            return entities.Select(MapToModel).ToList();
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            NotificationEntity? entity = await _db.Queryable<NotificationEntity>()
                .FirstAsync(n => n.Id == id);

            return entity is null ? null : MapToModel(entity);
        }

        public async Task<bool> CreateNotificationAsync(Notification notification)
        {
            try
            {
                NotificationEntity entity = MapToEntity(notification);
                entity.CreatedTime = notification.CreatedTime == default ? DateTime.Now : notification.CreatedTime;
                entity.IsRead = notification.IsRead;

                int id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
                notification.Id = id;
                notification.CreatedTime = entity.CreatedTime;
                if (id > 0)
                {
                    await _auditLogService.LogCreateAsync(
                        _userSession.GetAuditUserId(),
                        _userSession.GetAuditUsername(),
                        "Notification",
                        "Notification",
                        id,
                        notification,
                        $"创建通知: {notification.Title}");
                }

                return id > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                await LogNotificationFailureAsync(AuditActionType.Create, notification.Id, "创建通知失败", ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateNotificationAsync(Notification notification)
        {
            try
            {
                NotificationEntity? existingNotification = await _db.Queryable<NotificationEntity>()
                    .FirstAsync(n => n.Id == notification.Id);
                if (existingNotification == null)
                {
                    await LogNotificationFailureAsync(AuditActionType.Update, notification.Id, "更新通知失败", "Notification not found");
                    return false;
                }

                Notification oldNotification = MapToModel(existingNotification);
                existingNotification.Title = notification.Title;
                existingNotification.Content = notification.Content;
                existingNotification.Type = notification.Type;
                existingNotification.IsRead = notification.IsRead;

                int result = await _db.Updateable(existingNotification)
                    .Where(n => n.Id == notification.Id)
                    .ExecuteCommandAsync();

                if (result > 0)
                {
                    await _auditLogService.LogUpdateAsync(
                        _userSession.GetAuditUserId(),
                        _userSession.GetAuditUsername(),
                        "Notification",
                        "Notification",
                        notification.Id,
                        oldNotification,
                        notification,
                        $"更新通知: {notification.Title}");
                }

                return result > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                await LogNotificationFailureAsync(AuditActionType.Update, notification.Id, "更新通知失败", ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            try
            {
                Notification? notification = await GetNotificationByIdAsync(id);
                int result = await _db.Deleteable<NotificationEntity>()
                    .Where(n => n.Id == id)
                    .ExecuteCommandAsync();
                if (result > 0)
                {
                    await _auditLogService.LogDeleteAsync(
                        _userSession.GetAuditUserId(),
                        _userSession.GetAuditUsername(),
                        "Notification",
                        "Notification",
                        id,
                        notification,
                        $"删除通知: {notification?.Title ?? id.ToString()}");
                }

                return result > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                await LogNotificationFailureAsync(AuditActionType.Delete, id, "删除通知失败", ex.Message);
                return false;
            }
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            NotificationEntity? notification = await _db.Queryable<NotificationEntity>()
                .FirstAsync(n => n.Id == id);
            if (notification == null)
            {
                await LogNotificationFailureAsync(AuditActionType.Update, id, "标记通知已读失败", "Notification not found");
                return false;
            }

            bool wasRead = notification.IsRead;
            notification.IsRead = true;
            int result = await _db.Updateable(notification)
                .Where(n => n.Id == id)
                .ExecuteCommandAsync();
            if (result > 0 && !wasRead)
            {
                await _auditLogService.LogOperationAsync(
                    _userSession.GetAuditUserId(),
                    _userSession.GetAuditUsername(),
                    AuditActionType.Update,
                    "Notification",
                    $"标记通知已读: {notification.Title}",
                    "Notification",
                    id,
                    "Unread",
                    "Read");
            }

            return result > 0;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            int result = await _db.Updateable<NotificationEntity>()
                .SetColumns(n => new NotificationEntity { IsRead = true })
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteCommandAsync();

            if (result > 0)
            {
                await _auditLogService.LogOperationAsync(
                    _userSession.GetAuditUserId(),
                    _userSession.GetAuditUsername(),
                    AuditActionType.Update,
                    "Notification",
                    $"批量标记通知已读: {result} 条",
                    "Notification");
            }

            return result >= 0;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _db.Queryable<NotificationEntity>()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        private static Notification MapToModel(NotificationEntity entity)
        {
            return new Notification
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
                Type = entity.Type,
                IsRead = entity.IsRead,
                CreatedTime = entity.CreatedTime,
                UserId = entity.UserId
            };
        }

        private static NotificationEntity MapToEntity(Notification model)
        {
            return new NotificationEntity
            {
                Id = model.Id,
                Title = model.Title,
                Content = model.Content,
                Type = model.Type,
                IsRead = model.IsRead,
                CreatedTime = model.CreatedTime,
                UserId = model.UserId
            };
        }

        private async Task LogNotificationFailureAsync(AuditActionType actionType, int id, string description, string reason)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                actionType,
                "Notification",
                description,
                reason,
                "Notification",
                id == 0 ? null : id);
        }
    }
}
