using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Infrastructure.Repositories;
using SqlSugar;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.Services.Alarm
{
    /// <summary>
    /// 告警服务实现类
    /// </summary>
    public class AlarmService : IAlarmService
    {
        private readonly ISqlSugarClient _db;
        private readonly IRepository<AlarmRecord> _alarmRecordRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserSession _userSession;

        public AlarmService(
            ISqlSugarClient db,
            IRepository<AlarmRecord> alarmRecordRepository,
            IAuditLogService auditLogService,
            IUserSession userSession)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _alarmRecordRepository = alarmRecordRepository ?? throw new ArgumentNullException(nameof(alarmRecordRepository));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        public async Task<List<AlarmRecord>> GetAlarmRecordsAsync(int userId, AlarmStatus? status = null, AlarmLevel? level = null, DateTime? startTime = null, DateTime? endTime = null)
        {
            var query = _db.Queryable<AlarmRecord>()
                .Where(a => a.UserId == userId || a.UserId == 0); // UserId=0 表示全局告警

            if (status is { } alarmStatus)
            {
                query = query.Where(a => a.AlarmStatus == alarmStatus);
            }

            if (level is { } alarmLevel)
            {
                query = query.Where(a => a.AlarmLevel == alarmLevel);
            }

            if (startTime is { } from)
            {
                query = query.Where(a => a.TriggeredTime >= from);
            }

            if (endTime is { } to)
            {
                query = query.Where(a => a.TriggeredTime <= to);
            }

            return await query.OrderByDescending(a => a.TriggeredTime).ToListAsync();
        }

        public async Task<int> GetActiveAlarmCountAsync(int userId)
        {
            return await _db.Queryable<AlarmRecord>()
                .Where(a => (a.UserId == userId || a.UserId == 0) && a.AlarmStatus == AlarmStatus.Active)
                .CountAsync();
        }

        public async Task<int> GetCriticalAlarmCountAsync(int userId)
        {
            return await _db.Queryable<AlarmRecord>()
                .Where(a => (a.UserId == userId || a.UserId == 0) && a.AlarmStatus == AlarmStatus.Active && a.AlarmLevel == AlarmLevel.Critical)
                .CountAsync();
        }

        public async Task<AlarmRecord?> GetAlarmByIdAsync(int id)
        {
            return await _alarmRecordRepository.GetByIdAsync(id);
        }

        public async Task<bool> CreateAlarmAsync(AlarmRecord record)
        {
            record.CreatedAt = DateTime.Now;
            record.TriggeredTime = DateTime.Now;
            record.AlarmStatus = AlarmStatus.Active;

            var result = await _alarmRecordRepository.InsertAsync(record);
            return result > 0;
        }

        public async Task<bool> AcknowledgeAlarmAsync(int id, int userId)
        {
            var alarm = await GetAlarmByIdAsync(id);
            if (alarm == null || alarm.AlarmStatus != AlarmStatus.Active)
            {
                await LogAlarmFailureAsync(AuditActionType.Update, id, "确认告警失败", "Alarm not found or not active");
                return false;
            }

            AlarmStatus oldStatus = alarm.AlarmStatus;
            alarm.AlarmStatus = AlarmStatus.Acknowledged;
            alarm.AcknowledgedTime = DateTime.Now;
            alarm.AcknowledgedBy = userId;

            var result = await _alarmRecordRepository.UpdateAsync(alarm);
            bool success = result > 0;
            if (success)
            {
                await LogAlarmStatusChangeAsync(alarm, oldStatus, alarm.AlarmStatus, "确认告警");
            }
            else
            {
                await LogAlarmFailureAsync(AuditActionType.Update, id, "确认告警失败", "Database update returned no affected rows");
            }

            return success;
        }

        public async Task<bool> ResolveAlarmAsync(int id, int userId)
        {
            var alarm = await GetAlarmByIdAsync(id);
            if (alarm == null || alarm.AlarmStatus == AlarmStatus.Resolved)
            {
                await LogAlarmFailureAsync(AuditActionType.Update, id, "解决告警失败", "Alarm not found or already resolved");
                return false;
            }

            AlarmStatus oldStatus = alarm.AlarmStatus;
            alarm.AlarmStatus = AlarmStatus.Resolved;
            alarm.ResolvedTime = DateTime.Now;
            alarm.ResolvedBy = userId;

            var result = await _alarmRecordRepository.UpdateAsync(alarm);
            bool success = result > 0;
            if (success)
            {
                await LogAlarmStatusChangeAsync(alarm, oldStatus, alarm.AlarmStatus, "解决告警");
            }
            else
            {
                await LogAlarmFailureAsync(AuditActionType.Update, id, "解决告警失败", "Database update returned no affected rows");
            }

            return success;
        }

        public async Task<bool> IgnoreAlarmAsync(int id, int userId)
        {
            var alarm = await GetAlarmByIdAsync(id);
            if (alarm == null || alarm.AlarmStatus == AlarmStatus.Resolved || alarm.AlarmStatus == AlarmStatus.Ignored)
            {
                await LogAlarmFailureAsync(AuditActionType.Update, id, "忽略告警失败", "Alarm not found, resolved, or already ignored");
                return false;
            }

            AlarmStatus oldStatus = alarm.AlarmStatus;
            alarm.AlarmStatus = AlarmStatus.Ignored;
            alarm.ResolvedTime = DateTime.Now;
            alarm.ResolvedBy = userId;

            var result = await _alarmRecordRepository.UpdateAsync(alarm);
            bool success = result > 0;
            if (success)
            {
                await LogAlarmStatusChangeAsync(alarm, oldStatus, alarm.AlarmStatus, "忽略告警");
            }
            else
            {
                await LogAlarmFailureAsync(AuditActionType.Update, id, "忽略告警失败", "Database update returned no affected rows");
            }

            return success;
        }

        public async Task<int> AcknowledgeAllAsync(int userId)
        {
            var activeAlarms = await _db.Queryable<AlarmRecord>()
                .Where(a => (a.UserId == userId || a.UserId == 0) && a.AlarmStatus == AlarmStatus.Active)
                .ToListAsync();

            if (!activeAlarms.Any())
            {
                return 0;
            }

            int count = 0;
            foreach (var alarm in activeAlarms)
            {
                AlarmStatus oldStatus = alarm.AlarmStatus;
                alarm.AlarmStatus = AlarmStatus.Acknowledged;
                alarm.AcknowledgedTime = DateTime.Now;
                alarm.AcknowledgedBy = userId;

                if (await _alarmRecordRepository.UpdateAsync(alarm) > 0)
                {
                    await LogAlarmStatusChangeAsync(alarm, oldStatus, alarm.AlarmStatus, "批量确认告警");
                    count++;
                }
            }

            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Alarm",
                $"批量确认告警: {count} 条",
                "AlarmRecord");

            return count;
        }

        public async Task<int> GetTodayAlarmCountAsync(int userId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _db.Queryable<AlarmRecord>()
                .Where(a => (a.UserId == userId || a.UserId == 0) && a.TriggeredTime >= today && a.TriggeredTime < tomorrow)
                .CountAsync();
        }

        public async Task<(List<AlarmRecord> list, int total)> GetAlarmRecordsPageAsync(int userId, int pageIndex, int pageSize, AlarmStatus? status = null, AlarmLevel? level = null)
        {
            var query = _db.Queryable<AlarmRecord>()
                .Where(a => a.UserId == userId || a.UserId == 0);

            if (status is { } alarmStatus)
            {
                query = query.Where(a => a.AlarmStatus == alarmStatus);
            }

            if (level is { } alarmLevel)
            {
                query = query.Where(a => a.AlarmLevel == alarmLevel);
            }

            RefAsync<int> total = 0;
            var list = await query
                .OrderByDescending(a => a.TriggeredTime)
                .ToPageListAsync(pageIndex, pageSize, total);

            return (list, total);
        }

        private async Task LogAlarmStatusChangeAsync(AlarmRecord alarm, AlarmStatus oldStatus, AlarmStatus newStatus, string description)
        {
            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Update,
                "Alarm",
                $"{description}: {alarm.AlarmTitle}",
                "AlarmRecord",
                alarm.Id,
                oldStatus.ToString(),
                newStatus.ToString());
        }

        private async Task LogAlarmFailureAsync(AuditActionType actionType, int alarmId, string description, string reason)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                actionType,
                "Alarm",
                description,
                reason,
                "AlarmRecord",
                alarmId);
        }
    }
}
