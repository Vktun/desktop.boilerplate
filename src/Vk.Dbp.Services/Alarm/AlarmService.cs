using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Infrastructure.Repositories;
using SqlSugar;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.Services.Alarm
{
    /// <summary>
    /// 告警服务实现类
    /// </summary>
    public class AlarmService : IAlarmService
    {
        private readonly ISqlSugarClient _db;
        private readonly IRepository<AlarmRecord> _alarmRecordRepository;

        public AlarmService(
            ISqlSugarClient db,
            IRepository<AlarmRecord> alarmRecordRepository)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _alarmRecordRepository = alarmRecordRepository ?? throw new ArgumentNullException(nameof(alarmRecordRepository));
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
                return false;
            }

            alarm.AlarmStatus = AlarmStatus.Acknowledged;
            alarm.AcknowledgedTime = DateTime.Now;
            alarm.AcknowledgedBy = userId;

            var result = await _alarmRecordRepository.UpdateAsync(alarm);
            return result > 0;
        }

        public async Task<bool> ResolveAlarmAsync(int id, int userId)
        {
            var alarm = await GetAlarmByIdAsync(id);
            if (alarm == null || alarm.AlarmStatus == AlarmStatus.Resolved)
            {
                return false;
            }

            alarm.AlarmStatus = AlarmStatus.Resolved;
            alarm.ResolvedTime = DateTime.Now;
            alarm.ResolvedBy = userId;

            var result = await _alarmRecordRepository.UpdateAsync(alarm);
            return result > 0;
        }

        public async Task<bool> IgnoreAlarmAsync(int id, int userId)
        {
            var alarm = await GetAlarmByIdAsync(id);
            if (alarm == null || alarm.AlarmStatus == AlarmStatus.Resolved || alarm.AlarmStatus == AlarmStatus.Ignored)
            {
                return false;
            }

            alarm.AlarmStatus = AlarmStatus.Ignored;
            alarm.ResolvedTime = DateTime.Now;
            alarm.ResolvedBy = userId;

            var result = await _alarmRecordRepository.UpdateAsync(alarm);
            return result > 0;
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
                alarm.AlarmStatus = AlarmStatus.Acknowledged;
                alarm.AcknowledgedTime = DateTime.Now;
                alarm.AcknowledgedBy = userId;

                if (await _alarmRecordRepository.UpdateAsync(alarm) > 0)
                {
                    count++;
                }
            }

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
    }
}
