using System;
using System.Threading.Tasks;
using Dabp.Utils.Exceptions;
using SqlSugar;
using Dabp.Infrastructure.Entities;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 系统配置服务实现
    /// </summary>
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISqlSugarClient _db;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserSession _userSession;

        public SystemConfigService(ISqlSugarClient db, IAuditLogService auditLogService, IUserSession userSession)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        public async Task<string?> GetConfigValueAsync(string key)
        {
            var config = await _db.Queryable<SystemConfig>()
                .FirstAsync(c => c.ConfigKey == key);
            return config?.ConfigValue;
        }

        public async Task<bool> SetConfigValueAsync(string key, string value, string? description = null)
        {
            try
            {
                var config = await _db.Queryable<SystemConfig>()
                    .FirstAsync(c => c.ConfigKey == key);

                if (config == null)
                {
                    config = new SystemConfig
                    {
                        ConfigKey = key,
                        ConfigValue = value,
                        Description = description,
                        CreatedAt = DateTime.Now
                    };

                    bool success = await _db.Insertable(config).ExecuteCommandAsync() > 0;
                    if (success)
                    {
                        await LogConfigOperationAsync(AuditActionType.Create, key, description);
                    }

                    return success;
                }
                else
                {
                    config.ConfigValue = value;
                    config.UpdatedAt = DateTime.Now;
                    if (description != null)
                    {
                        config.Description = description;
                    }

                    bool success = await _db.Updateable(config).ExecuteCommandAsync() > 0;
                    if (success)
                    {
                        await LogConfigOperationAsync(AuditActionType.Update, key, description);
                    }

                    return success;
                }
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                await _auditLogService.LogFailureAsync(
                    _userSession.GetAuditUserId(),
                    _userSession.GetAuditUsername(),
                    AuditActionType.Update,
                    "System",
                    $"保存系统配置失败: {key}",
                    ex.Message,
                    "SystemConfig");

                return false;
            }
        }

        public async Task<int> GetIntConfigAsync(string key, int defaultValue = 0)
        {
            var value = await GetConfigValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (int.TryParse(value, out int result))
                return result;

            return defaultValue;
        }

        public async Task<bool> GetBoolConfigAsync(string key, bool defaultValue = false)
        {
            var value = await GetConfigValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (bool.TryParse(value, out bool result))
                return result;

            return defaultValue;
        }

        public async Task<int> GetSessionTimeoutMinutesAsync()
        {
            return await GetIntConfigAsync(SystemConfigKeys.SessionTimeoutMinutes, 15);
        }

        public async Task<bool> SetSessionTimeoutMinutesAsync(int minutes)
        {
            if (minutes < 1)
                minutes = 1;
            if (minutes > 480)
                minutes = 480;

            return await SetConfigValueAsync(
                SystemConfigKeys.SessionTimeoutMinutes,
                minutes.ToString(),
                "会话超时时间（分钟）");
        }

        public async Task<bool> GetSessionTimeoutEnabledAsync()
        {
            return await GetBoolConfigAsync(SystemConfigKeys.SessionTimeoutEnabled, true);
        }

        public async Task<bool> SetSessionTimeoutEnabledAsync(bool enabled)
        {
            return await SetConfigValueAsync(
                SystemConfigKeys.SessionTimeoutEnabled,
                enabled.ToString(),
                "是否启用会话超时");
        }

        public async Task<SystemConfig?> GetConfigByKeyAsync(string key)
        {
            return await _db.Queryable<SystemConfig>()
                .FirstAsync(c => c.ConfigKey == key);
        }

        private async Task LogConfigOperationAsync(AuditActionType actionType, string key, string? description)
        {
            await _auditLogService.LogOperationAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                actionType,
                "System",
                $"保存系统配置: {key}",
                "SystemConfig",
                null,
                null,
                description);
        }
    }
}
