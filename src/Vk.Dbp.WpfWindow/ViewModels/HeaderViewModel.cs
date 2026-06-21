using HandyControl.Controls;
using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Vk.Dbp.WpfWindow.Views;
using Vk.Dbp.Contracts.Constants;
using Dabp.WpfWindow.Services;
using Dabp.Utils.Exceptions;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Services.Alarm;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Services;
using System.Windows;

namespace Vk.Dbp.WpfWindow.ViewModels
{
    public class HeaderViewModel : BindableBase, IDisposable
    {
        private const string AdminUsername = "admin";
        private const string AdminDisplayName = "系统管理员";

        private readonly INavigationService _navigationService;
        private readonly IThemeService _themeService;
        private readonly IMenuPermissionFilter _menuPermissionFilter;
        private readonly IUserSession _userSession;
        private readonly ILockScreenService _lockScreenService;
        private readonly IUiDialogService _uiDialogService;
        private readonly IContainerProvider _container;
        private IAlarmService? _alarmService;
        private readonly IEventAggregator _eventAggregator;
        private bool _isDisposed = false;

        private IAlarmService AlarmService => _alarmService ??= _container.Resolve<IAlarmService>();

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

        private bool _isAccountPopupOpen;
        public bool IsAccountPopupOpen
        {
            get { return _isAccountPopupOpen; }
            set { SetProperty(ref _isAccountPopupOpen, value); }
        }

        private string _currentTheme = "Light";
        public string CurrentTheme
        {
            get { return _currentTheme; }
            set { SetProperty(ref _currentTheme, value); }
        }

        private int _notificationBadgeCount;
        public int NotificationBadgeCount
        {
            get { return _notificationBadgeCount; }
            set { SetProperty(ref _notificationBadgeCount, value); }
        }

        private int _alarmBadgeCount;
        public int AlarmBadgeCount
        {
            get { return _alarmBadgeCount; }
            set { SetProperty(ref _alarmBadgeCount, value); }
        }

        private bool _hasCriticalAlarm;
        public bool HasCriticalAlarm
        {
            get { return _hasCriticalAlarm; }
            set { SetProperty(ref _hasCriticalAlarm, value); }
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
        public DelegateCommand AlarmCommand { get; private set; }
        public DelegateCommand<string> AccountCommand { get; private set; }
        public DelegateCommand<string> ToggleThemeCommand { get; private set; }
        public DelegateCommand LoginCommand { get; private set; }
        public DelegateCommand LockCommand { get; private set; }

        private readonly INotifyPropertyChanged? _userSessionNotifier;

        public HeaderViewModel(
            INavigationService navigationService,
            IThemeService themeService,
            IMenuPermissionFilter menuPermissionFilter,
            IUserSession userSession,
            ILockScreenService lockScreenService,
            IUiDialogService uiDialogService,
            IContainerProvider container,
            IEventAggregator eventAggregator)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _menuPermissionFilter = menuPermissionFilter ?? throw new ArgumentNullException(nameof(menuPermissionFilter));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _lockScreenService = lockScreenService ?? throw new ArgumentNullException(nameof(lockScreenService));
            _uiDialogService = uiDialogService ?? throw new ArgumentNullException(nameof(uiDialogService));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            NavigateCommand = new DelegateCommand<string>(navigate);
            NotificationCommand = new DelegateCommand(appNotification);
            AlarmCommand = new DelegateCommand(showAlarm);
            AccountCommand = new DelegateCommand<string>(handleAccountAction);
            ToggleThemeCommand = new DelegateCommand<string>(handleToggleTheme);
            LoginCommand = new DelegateCommand(handleLogin);
            LockCommand = new DelegateCommand(handleLock, canLock);

            CurrentTheme = _themeService.CurrentTheme;

            _themeService.ThemeChanged += OnThemeChanged;

            if (_userSession is INotifyPropertyChanged notifyPropertyChanged)
            {
                _userSessionNotifier = notifyPropertyChanged;
                _userSessionNotifier.PropertyChanged += OnUserSessionPropertyChanged;
            }

            _eventAggregator.GetEvent<AlarmCountChangedEvent>().Subscribe(OnAlarmCountChanged);
            _eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);
            _eventAggregator.GetEvent<AlarmStatusChangedEvent>().Subscribe(OnAlarmStatusChanged);

            UpdateUserInfo();
            UpdateMenuVisibility();
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (_isDisposed) return;
            CurrentTheme = e.NewTheme;
        }

        private void OnUserSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isDisposed) return;
            if (e.PropertyName == nameof(IUserSession.IsLoggedIn) ||
                e.PropertyName == nameof(IUserSession.Permissions) ||
                e.PropertyName == nameof(IUserSession.IsLocked))
            {
                UpdateUserInfo();
                UpdateMenuVisibility();
                LockCommand.RaiseCanExecuteChanged();

                if (_userSession.IsLoggedIn)
                {
                    _ = UpdateAlarmBadgeAsync();
                }
            }
        }

        private void OnAlarmCountChanged(AlarmCountChangedPayload payload)
        {
            if (_isDisposed) return;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AlarmBadgeCount = payload.ActiveCount;
                HasCriticalAlarm = payload.HasCriticalAlarm;
            });
        }

        private void OnAlarmTriggered(AlarmTriggeredPayload payload)
        {
            if (_isDisposed) return;

            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await UpdateAlarmBadgeAsync();
            });
        }

        private void OnAlarmStatusChanged(AlarmStatusChangedPayload payload)
        {
            if (_isDisposed) return;

            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await UpdateAlarmBadgeAsync();
            });
        }

        private async Task UpdateAlarmBadgeAsync()
        {
            if (!_userSession.IsLoggedIn) return;

            try
            {
                var activeCount = await AlarmService.GetActiveAlarmCountAsync(_userSession.UserId);
                var criticalCount = await AlarmService.GetCriticalAlarmCountAsync(_userSession.UserId);

                AlarmBadgeCount = activeCount;
                HasCriticalAlarm = criticalCount > 0;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"Update alarm badge error: {ex.Message}");
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

            _eventAggregator.GetEvent<AlarmCountChangedEvent>().Unsubscribe(OnAlarmCountChanged);
            _eventAggregator.GetEvent<AlarmTriggeredEvent>().Unsubscribe(OnAlarmTriggered);
            _eventAggregator.GetEvent<AlarmStatusChangedEvent>().Unsubscribe(OnAlarmStatusChanged);

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
                UserName = GetDisplayName(_userSession.Username, _userSession.RealName);
                UserStatus = "在线";
                IsLoggedIn = true;
            }
            else
            {
                UserName = "未登录";
                UserStatus = "离线";
                IsLoggedIn = false;
                IsAccountPopupOpen = false;
                AlarmBadgeCount = 0;
                HasCriticalAlarm = false;
            }
        }

        private static string GetDisplayName(string? username, string? realName)
        {
            if (!IsInvalidDisplayName(realName))
            {
                return realName!.Trim();
            }

            if (string.Equals(username, AdminUsername, StringComparison.OrdinalIgnoreCase))
            {
                return AdminDisplayName;
            }

            return string.IsNullOrWhiteSpace(username) ? "未知用户" : username.Trim();
        }

        private static bool IsInvalidDisplayName(string? displayName)
        {
            return string.IsNullOrWhiteSpace(displayName) || displayName.Trim().All(c => c == '?');
        }

        private void navigate(string navigatePath)
        {
            if (!string.IsNullOrWhiteSpace(navigatePath))
            {
                _navigationService.NavigateTo(navigatePath);
            }
        }

        private void appNotification()
        {
            HandyControl.Controls.Notification.Show(new AppNotificationView(), ShowAnimation.Fade, true);
        }

        private void showAlarm()
        {
            HandyControl.Controls.Notification.Show(new AppAlarmView(), ShowAnimation.Fade, true);
        }

        private void handleAccountAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            IsAccountPopupOpen = false;

            switch (action)
            {
                case AccountActions.ChangePassword:
                    _navigationService.NavigateTo(ViewNames.ChangePasswordView);
                    break;
                case AccountActions.Logout:
                    handleLogout();
                    break;
                case AccountActions.Shutdown:
                    handleShutdown();
                    break;
                case AccountActions.Close:
                    handleClose();
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

            _navigationService.NavigateTo(ViewNames.LoginView);
        }

        private void handleLogin()
        {
            _navigationService.NavigateTo(ViewNames.LoginView);
        }

        private bool canLock()
        {
            return _userSession.IsLoggedIn && !_userSession.IsLocked;
        }

        private void handleLock()
        {
            _lockScreenService.Lock("用户手动锁定");
        }

        private void handleShutdown()
        {
            _uiDialogService.ShowInformation("关机功能已被禁用，请使用操作系统提供的关机选项。", "提示");
        }

        private void handleClose()
        {
            bool shouldClose = _uiDialogService.Confirm("确定要关闭程序吗？", "确认关闭");
            if (!shouldClose)
            {
                return;
            }

            Application.Current?.Shutdown();
        }
    }
}
