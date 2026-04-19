using System;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// 会话超时检测服务接口
    /// </summary>
    public interface ISessionTimeoutService
    {
        /// <summary>
        /// 开始监控用户活动
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// 停止监控
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// 重置超时计时器（用户有活动时调用）
        /// </summary>
        void ResetTimeout();

        /// <summary>
        /// 超时时长（分钟）
        /// </summary>
        int TimeoutMinutes { get; set; }

        /// <summary>
        /// 超时事件
        /// </summary>
        event EventHandler? Timeout;
    }
}