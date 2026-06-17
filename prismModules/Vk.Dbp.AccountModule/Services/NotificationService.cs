using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Utils.Exceptions;
using SqlSugar;
using Vk.Dbp.AccountModule.Models;
using NotificationEntity = Dabp.Infrastructure.Entities.Notification;

namespace Vk.Dbp.AccountModule.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ISqlSugarClient _db;

        public NotificationService(ISqlSugarClient db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
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
                return id > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
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
                    return false;
                }

                existingNotification.Title = notification.Title;
                existingNotification.Content = notification.Content;
                existingNotification.Type = notification.Type;
                existingNotification.IsRead = notification.IsRead;

                int result = await _db.Updateable(existingNotification)
                    .Where(n => n.Id == notification.Id)
                    .ExecuteCommandAsync();

                return result > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            try
            {
                int result = await _db.Deleteable<NotificationEntity>()
                    .Where(n => n.Id == id)
                    .ExecuteCommandAsync();
                return result > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                return false;
            }
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            NotificationEntity? notification = await _db.Queryable<NotificationEntity>()
                .FirstAsync(n => n.Id == id);
            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;
            int result = await _db.Updateable(notification)
                .Where(n => n.Id == id)
                .ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            int result = await _db.Updateable<NotificationEntity>()
                .SetColumns(n => new NotificationEntity { IsRead = true })
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteCommandAsync();

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
    }
}
