using System;
using System.Threading.Tasks;
using System.Windows;
using Dabp.Utils.Exceptions;
using Dabp.WpfWindow.ViewModels;
using Dabp.WpfWindow.Views;
using HandyControl.Controls;
using Prism.Ioc;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 閿佸睆鏈嶅姟瀹炵幇
    /// </summary>
    public class LockScreenService : ILockScreenService
    {
        private readonly IContainerProvider _container;
        private readonly IUserSession _userSession;

        private LockScreenWindow? _lockScreenWindow;

        public bool IsLocked => _userSession.IsLocked;

        public event EventHandler<LockScreenEventArgs>? Locked;
        public event EventHandler? Unlocked;

        public LockScreenService(
            IContainerProvider container,
            IUserSession userSession)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        /// <summary>
        /// 閿佸畾灞忓箷
        /// </summary>
        public void Lock(string reason)
        {
            // 鐢ㄦ埛鏈櫥褰曟椂涓嶈Е鍙戦攣灞?
            if (!_userSession.IsLoggedIn)
                return;

            // 宸茬粡閿佸睆鐘舵€佷笉閲嶅瑙﹀彂
            if (_userSession.IsLocked)
                return;

            // 璁剧疆浼氳瘽閿佸睆鐘舵€?
            _userSession.Lock(reason);

            // 鏄剧ず Toast 鎻愮ず
            Growl.Warning($"浼氳瘽宸查攣瀹? {reason}");

            // 瑙﹀彂閿佸睆浜嬩欢
            Locked?.Invoke(this, new LockScreenEventArgs
            {
                Reason = reason,
                LockTime = DateTime.Now
            });

            // 鏄剧ず閿佸睆绐楀彛
            ShowLockScreenWindow(reason);
        }

        /// <summary>
        /// 解锁屏幕 - 验证原用户密码（异步，禁止 UI 线程同步等待数据库）
        /// </summary>
        public async Task<bool> UnlockAsync(string password)
        {
            if (!_userSession.IsLocked)
                return true;

            if (string.IsNullOrEmpty(password))
                return false;

            string? failureReason = null;
            bool transientError = false;

            try
            {
                var credentialService = _container.Resolve<IUserCredentialService>();
                var result = await credentialService.VerifyUserPasswordAsync(_userSession.UserId, password);

                failureReason = result switch
                {
                    PasswordVerificationResult.UserNotFound => "User not found",
                    PasswordVerificationResult.NoPasswordHash => "Missing password hash",
                    PasswordVerificationResult.Mismatch => "Invalid password",
                    _ => null
                };
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                failureReason = ex.Message;
                transientError = true;
            }

            // 本方法的 await 已被 ConfigureAwait.Fody 织入，续体可能在线程池线程；
            // 会话状态、锁屏窗口、Growl 与事件的落地统一调度回 UI 线程。
            return Application.Current.Dispatcher.Invoke(() =>
            {
                if (failureReason == null)
                {
                    _userSession.Unlock();
                    LogUnlockSuccess();

                    CloseLockScreenWindow();

                    Unlocked?.Invoke(this, EventArgs.Empty);

                    Growl.Success("解锁成功");
                    return true;
                }

                Growl.Error(transientError ? "解锁失败，请稍后重试" : "密码错误，请重新输入");
                LogUnlockFailure(failureReason);
                return false;
            });
        }

        private void LogUnlockSuccess()
        {
            _ = LogUnlockSuccessAsync();
        }

        private void LogUnlockFailure(string reason)
        {
            _ = LogUnlockFailureAsync(reason);
        }

        private async Task LogUnlockSuccessAsync()
        {
            try
            {
                var auditLogService = _container.Resolve<IAuditLogService>();
                await auditLogService.LogOperationAsync(
                    _userSession.GetAuditUserId(),
                    _userSession.GetAuditUsername(),
                    AuditActionType.Update,
                    "Account",
                    $"用户解锁成功: {_userSession.Username}",
                    "Session",
                    _userSession.UserId);
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is ArgumentException ||
                ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"Unlock audit failed: {ex.Message}");
            }
        }

        private async Task LogUnlockFailureAsync(string reason)
        {
            try
            {
                var auditLogService = _container.Resolve<IAuditLogService>();
                await auditLogService.LogFailureAsync(
                    _userSession.GetAuditUserId(),
                    _userSession.GetAuditUsername(),
                    AuditActionType.Update,
                    "Account",
                    $"用户解锁失败: {_userSession.Username}",
                    reason,
                    "Session",
                    _userSession.UserId);
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is ArgumentException ||
                ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"Unlock audit failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 鏄剧ず閿佸睆绐楀彛
        /// </summary>
        private void ShowLockScreenWindow(string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_lockScreenWindow == null)
                {
                    // 閫氳繃瀹瑰櫒瑙ｆ瀽 ViewModel
                    var viewModel = _container.Resolve<LockScreenViewModel>();
                    viewModel.Initialize(_userSession.Username, _userSession.RealName, reason);

                    _lockScreenWindow = new LockScreenWindow();
                    _lockScreenWindow.DataContext = viewModel;
                    _lockScreenWindow.Closed += OnLockScreenWindowClosed;
                }
                else
                {
                    var viewModel = _lockScreenWindow.DataContext as LockScreenViewModel;
                    viewModel?.Initialize(_userSession.Username, _userSession.RealName, reason);
                }

                _lockScreenWindow.Show();
            });
        }

        /// <summary>
        /// 鍏抽棴閿佸睆绐楀彛
        /// </summary>
        private void CloseLockScreenWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_lockScreenWindow != null)
                {
                    _lockScreenWindow.Closed -= OnLockScreenWindowClosed;
                    _lockScreenWindow.Close();
                    _lockScreenWindow = null;
                }
            });
        }

        /// <summary>
        /// 閿佸睆绐楀彛鍏抽棴浜嬩欢澶勭悊
        /// </summary>
        private void OnLockScreenWindowClosed(object? sender, EventArgs e)
        {
            _lockScreenWindow = null;
        }
    }
}
