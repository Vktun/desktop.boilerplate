using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 会话超时检测服务实现
    /// 使用 DispatcherTimer 定期检查活动时间
    /// 监听 InputManager.PreProcessInput 事件捕获用户操作
    /// </summary>
    public class SessionTimeoutService : ISessionTimeoutService, IDisposable
    {
        private readonly IUserSession _userSession;
        private readonly ILockScreenService _lockScreenService;
        private readonly DispatcherTimer _checkTimer;
        private readonly DispatcherTimer _activityResetTimer;

        private bool _isMonitoring;
        private DateTime _lastCheckTime;
        private bool _disposed;

        /// <summary>
        /// 超时时长（分钟），默认15分钟
        /// </summary>
        public int TimeoutMinutes { get; set; } = 15;

        /// <summary>
        /// 检查间隔（秒），默认60秒检查一次
        /// </summary>
        public int CheckIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 超时事件
        /// </summary>
        public event EventHandler? Timeout;

        public SessionTimeoutService(IUserSession userSession, ILockScreenService lockScreenService)
        {
            _userSession = userSession;
            _lockScreenService = lockScreenService;

            // 创建检查定时器
            _checkTimer = new DispatcherTimer();
            _checkTimer.Interval = TimeSpan.FromSeconds(CheckIntervalSeconds);
            _checkTimer.Tick += OnCheckTimerTick;

            // 创建活动重置定时器（用于限制事件触发频率）
            _activityResetTimer = new DispatcherTimer();
            _activityResetTimer.Interval = TimeSpan.FromSeconds(1);
            _activityResetTimer.Tick += OnActivityResetTimerTick;

            _lastCheckTime = DateTime.Now;
        }

        /// <summary>
        /// 开始监控用户活动
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _lastCheckTime = DateTime.Now;
            _userSession.UpdateActivity();

            // 订阅输入事件
            InputManager.Current.PreProcessInput += OnPreProcessInput;

            // 启动检查定时器
            _checkTimer.Start();

            // 启动活动重置定时器
            _activityResetTimer.Start();
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;

            // 取消订阅输入事件
            InputManager.Current.PreProcessInput -= OnPreProcessInput;

            // 停止定时器
            _checkTimer.Stop();
            _activityResetTimer.Stop();
        }

        /// <summary>
        /// 重置超时计时器
        /// </summary>
        public void ResetTimeout()
        {
            _userSession.UpdateActivity();
            _lastCheckTime = DateTime.Now;
        }

        /// <summary>
        /// 检查定时器触发
        /// </summary>
        private void OnCheckTimerTick(object? sender, EventArgs e)
        {
            // 用户未登录或已锁屏时不检查
            if (!_userSession.IsLoggedIn || _userSession.IsLocked)
                return;

            // 检查是否超时
            var inactiveTime = DateTime.Now - _userSession.LastActivityTime;
            if (inactiveTime.TotalMinutes >= TimeoutMinutes)
            {
                // 触发超时
                OnTimeout();
            }
        }

        /// <summary>
        /// 活动重置定时器触发 - 用于批量处理活动更新
        /// </summary>
        private void OnActivityResetTimerTick(object? sender, EventArgs e)
        {
            // 检查是否有活动需要更新
            if (DateTime.Now - _lastCheckTime > TimeSpan.FromSeconds(1))
            {
                // 如果最近有输入活动，更新会话活动时间
                if (DateTime.Now - _lastActivityDetected < TimeSpan.FromSeconds(1))
                {
                    _userSession.UpdateActivity();
                }
            }
        }

        private DateTime _lastActivityDetected = DateTime.MinValue;

        /// <summary>
        /// 预处理输入事件 - 检测用户鼠标/键盘活动
        /// </summary>
        private void OnPreProcessInput(object? sender, PreProcessInputEventArgs e)
        {
            if (!_isMonitoring)
                return;

            // 检测键盘或鼠标输入
            var inputEventArgs = e.StagingItem.Input;
            if (inputEventArgs is KeyboardEventArgs || inputEventArgs is MouseEventArgs)
            {
                _lastActivityDetected = DateTime.Now;
            }
        }

        /// <summary>
        /// 超时处理
        /// </summary>
        private void OnTimeout()
        {
            // 停止检查（锁屏后不再检查）
            _checkTimer.Stop();

            // 触发超时事件
            Timeout?.Invoke(this, EventArgs.Empty);

            // 锁定屏幕
            _lockScreenService.Lock("会话超时，请重新验证身份");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopMonitoring();
            _checkTimer?.Stop();
            _activityResetTimer?.Stop();
        }
    }
}