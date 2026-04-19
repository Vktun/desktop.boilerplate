using System;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 锁屏服务接口
    /// </summary>
    public interface ILockScreenService
    {
        /// <summary>
        /// 当前是否处于锁屏状态
        /// </summary>
        bool IsLocked { get; }

        /// <summary>
        /// 锁定屏幕
        /// </summary>
        /// <param name="reason">锁屏原因</param>
        void Lock(string reason);

        /// <summary>
        /// 解锁屏幕 - 验证密码后解锁
        /// </summary>
        /// <param name="password">用户输入的密码</param>
        /// <returns>解锁是否成功</returns>
        bool Unlock(string password);

        /// <summary>
        /// 锁屏事件
        /// </summary>
        event EventHandler<LockScreenEventArgs>? Locked;

        /// <summary>
        /// 解锁事件
        /// </summary>
        event EventHandler? Unlocked;
    }

    /// <summary>
    /// 锁屏事件参数
    /// </summary>
    public class LockScreenEventArgs : EventArgs
    {
        /// <summary>
        /// 锁屏原因
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 锁屏时间
        /// </summary>
        public DateTime LockTime { get; set; } = DateTime.Now;
    }
}