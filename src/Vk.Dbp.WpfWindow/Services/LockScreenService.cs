using System;
using System.Threading.Tasks;
using System.Windows;
using Dabp.Utils.Security;
using Dabp.Utils.Exceptions;
using Dabp.WpfWindow.ViewModels;
using Dabp.WpfWindow.Views;
using HandyControl.Controls;
using Prism.Ioc;
using Vk.Dbp.AccountModule.Services;
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
        private readonly IPasswordHasher _passwordHasher;

        private LockScreenWindow? _lockScreenWindow;

        public bool IsLocked => _userSession.IsLocked;

        public event EventHandler<LockScreenEventArgs>? Locked;
        public event EventHandler? Unlocked;

        public LockScreenService(
            IContainerProvider container,
            IUserSession userSession,
            IPasswordHasher passwordHasher)
        {
            _container = container;
            _userSession = userSession;
            _passwordHasher = passwordHasher;
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
        /// 瑙ｉ攣灞忓箷 - 楠岃瘉鍘熺敤鎴峰瘑鐮?
        /// </summary>
        public bool Unlock(string password)
        {
            if (!_userSession.IsLocked)
                return true;

            if (string.IsNullOrEmpty(password))
                return false;

            try
            {
                var userService = _container.Resolve<IUserService>();
                var user = userService.GetUserByIdAsync(_userSession.UserId).GetAwaiter().GetResult();
                if (user == null)
                    return false;

                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                    return false;

                // 楠岃瘉瀵嗙爜
                bool isValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
                if (isValid)
                {
                    // 瑙ｉ攣浼氳瘽
                    _userSession.Unlock();

                    // 鍏抽棴閿佸睆绐楀彛
                    CloseLockScreenWindow();

                    // 瑙﹀彂瑙ｉ攣浜嬩欢
                    Unlocked?.Invoke(this, EventArgs.Empty);

                    Growl.Success("瑙ｉ攣鎴愬姛");
                    return true;
                }

                Growl.Error("瀵嗙爜閿欒锛岃閲嶆柊杈撳叆");
                return false;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                Growl.Error("瑙ｉ攣澶辫触锛岃绋嶅悗閲嶈瘯");
                return false;
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
