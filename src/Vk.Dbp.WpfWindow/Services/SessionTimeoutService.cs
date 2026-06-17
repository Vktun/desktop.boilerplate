using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 会话超时检测服务实现。
    /// 使用 DispatcherTimer 定期检查活动时间，
    /// 监听 InputManager.PreProcessInput 事件捕获用户操作。
    /// </summary>
    public class SessionTimeoutService : ISessionTimeoutService, IDisposable
    {
        private readonly IUserSession _userSession;
        private readonly ILockScreenService _lockScreenService;
        private readonly DispatcherTimer _checkTimer;
        private readonly DispatcherTimer _activityResetTimer;

        private bool _isMonitoring;
        private DateTime _lastCheckTime;
        private DateTime _lastActivityDetected = DateTime.MinValue;
        private bool _disposed;

        /// <summary>
        /// 超时时长（分钟），默认15分钟。
        /// </summary>
        public int TimeoutMinutes { get; set; } = 15;

        /// <summary>
        /// 检查间隔（秒），默认60秒。
        /// </summary>
        public int CheckIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 超时事件。
        /// </summary>
        public event EventHandler? Timeout;

        public SessionTimeoutService(IUserSession userSession, ILockScreenService lockScreenService)
        {
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _lockScreenService = lockScreenService ?? throw new ArgumentNullException(nameof(lockScreenService));

            _checkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(CheckIntervalSeconds)
            };
            _checkTimer.Tick += OnCheckTimerTick;

            _activityResetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _activityResetTimer.Tick += OnActivityResetTimerTick;

            _lastCheckTime = DateTime.Now;
        }

        /// <summary>
        /// 开始监控用户活动。
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
            {
                return;
            }

            _isMonitoring = true;
            _lastCheckTime = DateTime.Now;
            _userSession.UpdateActivity();

            InputManager.Current.PreProcessInput += OnPreProcessInput;
            _checkTimer.Start();
            _activityResetTimer.Start();
        }

        /// <summary>
        /// 停止监控。
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
            {
                return;
            }

            _isMonitoring = false;
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            _checkTimer.Stop();
            _activityResetTimer.Stop();
        }

        /// <summary>
        /// 重置超时计时器。
        /// </summary>
        public void ResetTimeout()
        {
            _userSession.UpdateActivity();
            _lastCheckTime = DateTime.Now;
        }

        private void OnCheckTimerTick(object? sender, EventArgs e)
        {
            if (!_userSession.IsLoggedIn || _userSession.IsLocked)
            {
                return;
            }

            TimeSpan inactiveTime = DateTime.Now - _userSession.LastActivityTime;
            if (inactiveTime.TotalMinutes >= TimeoutMinutes)
            {
                OnTimeout();
            }
        }

        private void OnActivityResetTimerTick(object? sender, EventArgs e)
        {
            if (DateTime.Now - _lastCheckTime > TimeSpan.FromSeconds(1) &&
                DateTime.Now - _lastActivityDetected < TimeSpan.FromSeconds(1))
            {
                _userSession.UpdateActivity();
            }
        }

        private void OnPreProcessInput(object? sender, PreProcessInputEventArgs e)
        {
            if (!_isMonitoring)
            {
                return;
            }

            InputEventArgs inputEventArgs = e.StagingItem.Input;
            if (inputEventArgs is KeyboardEventArgs or MouseEventArgs)
            {
                _lastActivityDetected = DateTime.Now;
            }
        }

        private void OnTimeout()
        {
            _checkTimer.Stop();
            Timeout?.Invoke(this, EventArgs.Empty);
            _lockScreenService.Lock("浼氳瘽瓒呮椂锛岃閲嶆柊楠岃瘉韬唤");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopMonitoring();
            _checkTimer.Stop();
            _activityResetTimer.Stop();
        }
    }
}
