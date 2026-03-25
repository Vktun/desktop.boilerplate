using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vk.Dbp.WpfWindow.Views;
using Dabp.WpfWindow.Services;
using Vk.Dbp.AccountModule.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace Vk.Dbp.WpfWindow.ViewModels
{
    public class HeaderViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IThemeService _themeService;

        private string _userName = "未登录";
        public string UserName
        {
            get { return _userName; }
            set { SetProperty(ref _userName, value); }
        }

        private string _userStatus = "离线";
        public string UserStatus
        {
            get { return _userStatus; }
            set { SetProperty(ref _userStatus, value); }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set { SetProperty(ref _isLoggedIn, value); }
        }

        private string _currentTheme = "Light";
        public string CurrentTheme
        {
            get { return _currentTheme; }
            set { SetProperty(ref _currentTheme, value); }
        }

        public DelegateCommand<string> NavigateCommand { get; private set; }
        public DelegateCommand NotificationCommand { get; private set; }
        public DelegateCommand<string> AccountCommand { get; private set; }
        public DelegateCommand<string> ToggleThemeCommand { get; private set; }

        public HeaderViewModel(IRegionManager regionManager, IThemeService themeService)
        {
            _regionManager = regionManager;
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            NavigateCommand = new DelegateCommand<string>(navigate);
            NotificationCommand = new DelegateCommand(appNotification);
            AccountCommand = new DelegateCommand<string>(handleAccountAction);
            ToggleThemeCommand = new DelegateCommand<string>(handleToggleTheme);

            // 初始化当前主题
            CurrentTheme = _themeService.CurrentTheme;

            // 订阅主题变更事件
            _themeService.ThemeChanged += (s, e) =>
            {
                CurrentTheme = e.NewTheme;
            };

            // 订阅用户会话变更事件
            var userSession = UserSession.Instance;
            userSession.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UserSession.IsLoggedIn))
                {
                    UpdateUserInfo();
                }
            };

            // 初始化用户信息
            UpdateUserInfo();
        }

        /// <summary>
        /// 更新用户信息显示
        /// </summary>
        private void UpdateUserInfo()
        {
            var userSession = UserSession.Instance;
            if (userSession.IsLoggedIn)
            {
                UserName = userSession.RealName ?? userSession.Username ?? "未知用户";
                UserStatus = "在线";
                IsLoggedIn = true;
            }
            else
            {
                UserName = "未登录";
                UserStatus = "离线";
                IsLoggedIn = false;
            }
        }

        private void navigate(string navigatePath)
        {
            if (navigatePath != null)
                _regionManager.RequestNavigate("ContentRegion", navigatePath);
        }

        private void appNotification()
        {
            Notification.Show(new AppNotificationView(), ShowAnimation.Fade, true);
        }

        /// <summary>
        /// 处理账户相关操作
        /// </summary>
        private void handleAccountAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            switch (action)
            {
                case "ChangePassword":
                    _regionManager.RequestNavigate("ContentRegion", "ChangePasswordView");
                    break;
                case "Logout":
                    handleLogout();
                    break;
                case "Shutdown":
                    handleShutdown();
                    break;
                case "Close":
                    System.Windows.Application.Current.Shutdown();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理主题切换
        /// </summary>
        private void handleToggleTheme(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return;

            // 如果传递的是当前主题，则切换到另一个主题
            if (theme == _themeService.CurrentTheme)
            {
                theme = _themeService.CurrentTheme == "Light" ? "Dark" : "Light";
            }

            _themeService.SetTheme(theme);
        }

        private void handleLogout()
        {
            // 清除用户会话信息
            var userSession = UserSession.Instance;
            userSession.Logout();

            // 更新显示
            UpdateUserInfo();

            // 导航到登录页面
            _regionManager.RequestNavigate("ContentRegion", "LoginView");
        }

        private void handleShutdown()
        {
            // TODO: 实现系统关机逻辑
            var result = System.Windows.MessageBox.Show("确认要关机吗？", "提示", System.Windows.MessageBoxButton.OKCancel);
            if (result == System.Windows.MessageBoxResult.OK)
            {
                // 调用系统关机命令
                System.Diagnostics.Process.Start("shutdown", "/s /t 30");
            }
        }
    }
}
