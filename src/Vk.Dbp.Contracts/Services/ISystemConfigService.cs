using System.Threading.Tasks;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// 系统配置服务契约（实现位于 AccountModule；shell 启动流程读取会话超时配置）
    /// </summary>
    public interface ISystemConfigService
    {
        /// <summary>获取配置值</summary>
        Task<string?> GetConfigValueAsync(string key);

        /// <summary>设置配置值</summary>
        Task<bool> SetConfigValueAsync(string key, string value, string? description = null);

        /// <summary>获取整数配置值</summary>
        Task<int> GetIntConfigAsync(string key, int defaultValue = 0);

        /// <summary>获取布尔配置值</summary>
        Task<bool> GetBoolConfigAsync(string key, bool defaultValue = false);

        /// <summary>获取会话超时时间（分钟）</summary>
        Task<int> GetSessionTimeoutMinutesAsync();

        /// <summary>设置会话超时时间（分钟）</summary>
        Task<bool> SetSessionTimeoutMinutesAsync(int minutes);

        /// <summary>获取是否启用会话超时</summary>
        Task<bool> GetSessionTimeoutEnabledAsync();

        /// <summary>设置是否启用会话超时</summary>
        Task<bool> SetSessionTimeoutEnabledAsync(bool enabled);
    }
}
