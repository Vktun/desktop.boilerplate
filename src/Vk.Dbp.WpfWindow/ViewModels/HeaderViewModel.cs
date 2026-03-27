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
        private readonly IMenuPermissionFilter _menuPermissionFilter;

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

        private bool _isDashboardVisible = true;
        public bool IsDashboardVisible
        {
            get { return _isDashboardVisible; }
            set { SetProperty(ref _isDashboardVisible, value); }
        }

        private bool _isSelfCheckVisible;
        public bool IsSelfCheckVisible
        {
            get { return _isSelfCheckVisible; }
            set { SetProperty(ref _isSelfCheckVisible, value); }
        }

        private bool _isProductionVisible;
        public bool IsProductionVisible
        {
            get { return _isProductionVisible; }
            set { SetProperty(ref _isProductionVisible, value); }
        }

        private bool _isProductionRecordVisible;
        public bool IsProductionRecordVisible
        {
            get { return _isProductionRecordVisible; }
            set { SetProperty(ref _isProductionRecordVisible, value); }
        }

        private bool _isAlarmRecordVisible;
        public bool IsAlarmRecordVisible
        {
            get { return _isAlarmRecordVisible; }
            set { SetProperty(ref _isAlarmRecordVisible, value); }
        }

        private bool _isAuditRecordVisible;
        public bool IsAuditRecordVisible
        {
            get { return _isAuditRecordVisible; }
            set { SetProperty(ref _isAuditRecordVisible, value); }
        }

        private bool _isAdminSettingVisible;
        public bool IsAdminSettingVisible
        {
            get { return _isAdminSettingVisible; }
            set { SetProperty(ref _isAdminSettingVisible, value); }
        }

        public DelegateCommand<string> NavigateCommand { get; private set; }
        public DelegateCommand NotificationCommand { get; private set; }
        public DelegateCommand<string> AccountCommand { get; private set; }
        public DelegateCommand<string> ToggleThemeCommand { get; private set; }
        public DelegateCommand LoginCommand { get; private set; }

        public HeaderViewModel(IRegionManager regionManager, IThemeService themeService)
        {
            _regionManager = regionManager;
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _menuPermissionFilter = new MenuPermissionFilter();

            NavigateCommand = new DelegateCommand<string>(navigate);
            NotificationCommand = new DelegateCommand(appNotification);
            AccountCommand = new DelegateCommand<string>(handleAccountAction);
            ToggleThemeCommand = new DelegateCommand<string>(handleToggleTheme);
            LoginCommand = new DelegateCommand(handleLogin);

            CurrentTheme = _themeService.CurrentTheme;

            _themeService.ThemeChanged += (s, e) =>
            {
                CurrentTheme = e.NewTheme;
            };

            var userSession = UserSession.Instance;
            userSession.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UserSession.IsLoggedIn) ||
                    e.PropertyName == nameof(UserSession.Permissions))
                {
                    UpdateUserInfo();
                    UpdateMenuVisibility();
                }
            };

            UpdateUserInfo();
            UpdateMenuVisibility();
        }

        private void UpdateMenuVisibility()
        {
            IsDashboardVisible = _menuPermissionFilter.IsMenuVisible("Dashboard");
            IsSelfCheckVisible = _menuPermissionFilter.IsMenuVisible("SelfCheck");
            IsProductionVisible = _menuPermissionFilter.IsMenuVisible("Production");
            IsProductionRecordVisible = _menuPermissionFilter.IsMenuVisible("ProductionRecord");
            IsAlarmRecordVisible = _menuPermissionFilter.IsMenuVisible("AlarmRecord");
            IsAuditRecordVisible = _menuPermissionFilter.IsMenuVisible("AuditRecord");
            IsAdminSettingVisible = _menuPermissionFilter.IsMenuVisible("AdminSettingView");
        }

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
            HandyControl.Controls.Notification.Show(new AppNotificationView(), ShowAnimation.Fade, true);
        }

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

        private void handleToggleTheme(string theme)
        {
            if (string.IsNullOrEmpty(theme))
                return;

            if (theme == _themeService.CurrentTheme)
            {
                theme = _themeService.CurrentTheme == "Light" ? "Dark" : "Light";
            }

            _themeService.SetTheme(theme);
        }

        private void handleLogout()
        {
            var userSession = UserSession.Instance;
            userSession.Logout();

            UpdateUserInfo();
            UpdateMenuVisibility();

            _regionManager.RequestNavigate("ContentRegion", "LoginView");
        }

        private void handleLogin()
        {
            _regionManager.RequestNavigate("ContentRegion", "LoginView");
        }

        private void handleShutdown()
        {
            var result = System.Windows.MessageBox.Show("确认要关机吗？", "提示", System.Windows.MessageBoxButton.OKCancel);
            if (result == System.Windows.MessageBoxResult.OK)
            {
                System.Diagnostics.Process.Start("shutdown", "/s /t 30");
            }
        }
    }
}
