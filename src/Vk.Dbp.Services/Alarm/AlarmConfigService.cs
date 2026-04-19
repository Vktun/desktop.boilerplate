using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Infrastructure.Repositories;
using SqlSugar;

namespace Vk.Dbp.Services.Alarm
{
    /// <summary>
    /// 告警配置服务实现类
    /// </summary>
    public class AlarmConfigService : IAlarmConfigService
    {
        private readonly ISqlSugarClient _db;
        private readonly IRepository<AlarmConfig> _alarmConfigRepository;

        public AlarmConfigService(
            ISqlSugarClient db,
            IRepository<AlarmConfig> alarmConfigRepository)
        {
            _db = db;
            _alarmConfigRepository = alarmConfigRepository;
        }

        public async Task<List<AlarmConfig>> GetAlarmConfigsAsync()
        {
            return await _db.Queryable<AlarmConfig>()
                .OrderBy(c => c.Priority)
                .ToListAsync();
        }

        public async Task<AlarmConfig> GetAlarmConfigByCodeAsync(string alarmCode)
        {
            return await _db.Queryable<AlarmConfig>()
                .Where(c => c.AlarmCode == alarmCode)
                .FirstAsync();
        }

        public async Task<AlarmConfig> GetAlarmConfigByIdAsync(int id)
        {
            return await _alarmConfigRepository.GetByIdAsync(id);
        }

        public async Task<bool> SaveAlarmConfigAsync(AlarmConfig config)
        {
            if (config.Id == 0)
            {
                // 新增配置
                config.CreatedAt = DateTime.Now;
                config.UpdatedAt = null;
                return await _alarmConfigRepository.InsertAsync(config) > 0;
            }
            else
            {
                // 更新配置
                config.UpdatedAt = DateTime.Now;
                return await _alarmConfigRepository.UpdateAsync(config) > 0;
            }
        }

        public async Task<bool> DeleteAlarmConfigAsync(int id)
        {
            return await _alarmConfigRepository.DeleteByIdAsync(id) > 0;
        }

        public async Task<List<AlarmConfig>> GetEnabledAlarmConfigsAsync()
        {
            return await _db.Queryable<AlarmConfig>()
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.Priority)
                .ToListAsync();
        }

        public async Task<bool> ValidateThresholdAsync(string alarmCode, decimal value)
        {
            var config = await GetAlarmConfigByCodeAsync(alarmCode);
            if (config == null || !config.IsEnabled)
            {
                return false;
            }

            var comparisonType = config.ComparisonType;

            switch (comparisonType)
            {
                case ComparisonTypes.GreaterThan:
                    return config.ThresholdMax.HasValue && value > config.ThresholdMax.Value;

                case ComparisonTypes.LessThan:
                    return config.ThresholdMin.HasValue && value < config.ThresholdMin.Value;

                case ComparisonTypes.InRange:
                    return config.ThresholdMin.HasValue && config.ThresholdMax.HasValue &&
                           value >= config.ThresholdMin.Value && value <= config.ThresholdMax.Value;

                case ComparisonTypes.OutOfRange:
                    return config.ThresholdMin.HasValue && config.ThresholdMax.HasValue &&
                           (value < config.ThresholdMin.Value || value > config.ThresholdMax.Value);

                case ComparisonTypes.Equal:
                    return (config.ThresholdMin.HasValue && value == config.ThresholdMin.Value) ||
                           (config.ThresholdMax.HasValue && value == config.ThresholdMax.Value);

                case ComparisonTypes.NotEqual:
                    return (config.ThresholdMin.HasValue && value != config.ThresholdMin.Value) ||
                           (config.ThresholdMax.HasValue && value != config.ThresholdMax.Value);

                default:
                    return false;
            }
        }
    }
}