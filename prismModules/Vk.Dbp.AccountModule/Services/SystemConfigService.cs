using System;
using System.Threading.Tasks;
using SqlSugar;
using Dabp.Infrastructure.Entities;

namespace Vk.Dbp.AccountModule.Services
{
    /// <summary>
    /// 系统配置服务实现
    /// </summary>
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISqlSugarClient _db;

        public SystemConfigService(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task<string?> GetConfigValueAsync(string key)
        {
            var config = await _db.Queryable<SystemConfig>()
                .FirstAsync(c => c.ConfigKey == key);
            return config?.ConfigValue;
        }

        public async Task<bool> SetConfigValueAsync(string key, string value, string? description = null)
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
                return await _db.Insertable(config).ExecuteCommandAsync() > 0;
            }
            else
            {
                config.ConfigValue = value;
                config.UpdatedAt = DateTime.Now;
                if (description != null)
                {
                    config.Description = description;
                }
                return await _db.Updateable(config).ExecuteCommandAsync() > 0;
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
    }
}