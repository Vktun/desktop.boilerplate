using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Dabp.Infrastructure.Repositories;
using Dabp.Utils.Exceptions;
using SqlSugar;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.Services.Alarm
{
    /// <summary>
    /// 告警配置服务实现类
    /// </summary>
    public class AlarmConfigService : IAlarmConfigService
    {
        private readonly ISqlSugarClient _db;
        private readonly IRepository<AlarmConfig> _alarmConfigRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserSession _userSession;

        public AlarmConfigService(
            ISqlSugarClient db,
            IRepository<AlarmConfig> alarmConfigRepository,
            IAuditLogService auditLogService,
            IUserSession userSession)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _alarmConfigRepository = alarmConfigRepository ?? throw new ArgumentNullException(nameof(alarmConfigRepository));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
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

        public async Task<AlarmConfig?> GetAlarmConfigByIdAsync(int id)
        {
            return await _alarmConfigRepository.GetByIdAsync(id);
        }

        public async Task<bool> SaveAlarmConfigAsync(AlarmConfig config)
        {
            try
            {
                if (config.Id == 0)
                {
                    config.CreatedAt = DateTime.Now;
                    config.UpdatedAt = null;

                    bool success = await _alarmConfigRepository.InsertAsync(config) > 0;
                    if (success)
                    {
                        await LogAlarmConfigOperationAsync(AuditActionType.Create, config, "创建告警配置");
                    }

                    return success;
                }

                AlarmConfig? oldConfig = await GetAlarmConfigByIdAsync(config.Id);
                config.UpdatedAt = DateTime.Now;

                bool updateSuccess = await _alarmConfigRepository.UpdateAsync(config) > 0;
                if (updateSuccess)
                {
                    await LogAlarmConfigOperationAsync(AuditActionType.Update, config, "更新告警配置", oldConfig);
                }

                return updateSuccess;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                await LogAlarmConfigFailureAsync(
                    config.Id == 0 ? AuditActionType.Create : AuditActionType.Update,
                    config.Id,
                    "保存告警配置失败",
                    ex.Message);

                return false;
            }
        }

        public async Task<bool> DeleteAlarmConfigAsync(int id)
        {
            try
            {
                AlarmConfig? config = await GetAlarmConfigByIdAsync(id);
                bool success = await _alarmConfigRepository.DeleteByIdAsync(id) > 0;
                if (success)
                {
                    await _auditLogService.LogDeleteAsync(
                        _userSession.GetAuditUserId(),
                        _userSession.GetAuditUsername(),
                        "Alarm",
                        "AlarmConfig",
                        id,
                        config,
                        $"删除告警配置: {config?.AlarmCode ?? id.ToString()}");
                }

                return success;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                await LogAlarmConfigFailureAsync(AuditActionType.Delete, id, "删除告警配置失败", ex.Message);
                return false;
            }
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

        private async Task LogAlarmConfigOperationAsync(
            AuditActionType actionType,
            AlarmConfig config,
            string description,
            AlarmConfig? oldConfig = null)
        {
            if (actionType == AuditActionType.Create)
            {
                await _auditLogService.LogCreateAsync(
                    _userSession.GetAuditUserId(),
                    _userSession.GetAuditUsername(),
                    "Alarm",
                    "AlarmConfig",
                    config.Id,
                    config,
                    $"{description}: {config.AlarmCode}");
                return;
            }

            object oldData = oldConfig is null ? new { config.Id } : oldConfig;

            await _auditLogService.LogUpdateAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Alarm",
                "AlarmConfig",
                config.Id,
                oldData,
                config,
                $"{description}: {config.AlarmCode}");
        }

        private async Task LogAlarmConfigFailureAsync(AuditActionType actionType, int id, string description, string reason)
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                actionType,
                "Alarm",
                description,
                reason,
                "AlarmConfig",
                id == 0 ? null : id);
        }
    }
}
