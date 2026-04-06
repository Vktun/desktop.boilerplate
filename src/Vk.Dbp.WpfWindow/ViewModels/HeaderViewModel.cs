using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vk.Dbp.WpfWindow.Views;
using Vk.Dbp.WpfWindow.Constants;
using Dabp.WpfWindow.Services;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace Vk.Dbp.WpfWindow.ViewModels
{
    public class HeaderViewModel : BindableBase, IDisposable
    {
        private readonly IRegionManager _regionManager;
        private readonly IThemeService _themeService;
        private readonly IMenuPermissionFilter _menuPermissionFilter;
        private readonly IUserSession _userSession;
        private bool _isDisposed = false;

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

        private INotifyPropertyChanged _userSessionNotifier;

        public HeaderViewModel(IRegionManager regionManager, IThemeService themeService, IMenuPermissionFilter menuPermissionFilter, IUserSession userSession)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _menuPermissionFilter = menuPermissionFilter ?? throw new ArgumentNullException(nameof(menuPermissionFilter));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            NavigateCommand = new DelegateCommand<string>(navigate);
            NotificationCommand = new DelegateCommand(appNotification);
            AccountCommand = new DelegateCommand<string>(handleAccountAction);
            ToggleThemeCommand = new DelegateCommand<string>(handleToggleTheme);
            LoginCommand = new DelegateCommand(handleLogin);

            CurrentTheme = _themeService.CurrentTheme;

            _themeService.ThemeChanged += OnThemeChanged;

            if (_userSession is INotifyPropertyChanged notifyPropertyChanged)
            {
                _userSessionNotifier = notifyPropertyChanged;
                _userSessionNotifier.PropertyChanged += OnUserSessionPropertyChanged;
            }

            UpdateUserInfo();
            UpdateMenuVisibility();
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            if (_isDisposed) return;
            CurrentTheme = e.NewTheme;
        }

        private void OnUserSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isDisposed) return;
            if (e.PropertyName == nameof(IUserSession.IsLoggedIn) ||
                e.PropertyName == nameof(IUserSession.Permissions))
            {
                UpdateUserInfo();
                UpdateMenuVisibility();
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _themeService.ThemeChanged -= OnThemeChanged;
            if (_userSessionNotifier != null)
            {
                _userSessionNotifier.PropertyChanged -= OnUserSessionPropertyChanged;
            }

            _isDisposed = true;
        }

        private void UpdateMenuVisibility()
        {
            _menuPermissionFilter.RefreshPermissions();
            IsDashboardVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.Dashboard);
            IsSelfCheckVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.SelfCheck);
            IsProductionVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.Production);
            IsProductionRecordVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.ProductionRecord);
            IsAlarmRecordVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.AlarmRecord);
            IsAuditRecordVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.AuditRecord);
            IsAdminSettingVisible = _menuPermissionFilter.IsMenuVisible(ViewNames.AdminSettingView);
        }

        private void UpdateUserInfo()
        {
            if (_userSession.IsLoggedIn)
            {
                UserName = _userSession.RealName ?? _userSession.Username ?? "未知用户";
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
                _regionManager.RequestNavigate(RegionNames.ContentRegion, navigatePath);
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
                case AccountActions.ChangePassword:
                    _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.ChangePasswordView);
                    break;
                case AccountActions.Logout:
                    handleLogout();
                    break;
                case AccountActions.Shutdown:
                    handleShutdown();
                    break;
                case AccountActions.Close:
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
            _userSession.Logout();

            UpdateUserInfo();
            UpdateMenuVisibility();

            _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.LoginView);
        }

        private void handleLogin()
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, ViewNames.LoginView);
        }

        private void handleShutdown()
        {
            // 桌面应用不应直接执行系统关机命令
            // 此功能已被禁用以提高安全性
            System.Windows.MessageBox.Show("关机功能已被禁用，请使用操作系统提供的关机选项。", "提示", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
