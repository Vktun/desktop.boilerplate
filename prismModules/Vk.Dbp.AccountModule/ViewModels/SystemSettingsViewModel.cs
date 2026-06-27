using System;
using System.Threading.Tasks;
using System.Windows.Media;
using Dabp.Utils.Exceptions;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class SystemSettingsViewModel : BindableBase, INavigationAware
    {
        private readonly ISystemConfigService _configService;
        private readonly ISessionTimeoutService _timeoutService;
        private readonly IUserSession _userSession;
        private readonly IAuditLogService _auditLogService;

        private bool _sessionTimeoutEnabled = true;
        private int _sessionTimeoutMinutes = 15;
        private string? _statusMessage;
        private bool _hasStatusMessage;
        private Color _statusColor = Colors.Green;

        /// <summary>
        /// 是否启用会话超时
        /// </summary>
        public bool SessionTimeoutEnabled
        {
            get => _sessionTimeoutEnabled;
            set => SetProperty(ref _sessionTimeoutEnabled, value);
        }

        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        public int SessionTimeoutMinutes
        {
            get => _sessionTimeoutMinutes;
            set => SetProperty(ref _sessionTimeoutMinutes, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                HasStatusMessage = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// 是否有状态消息
        /// </summary>
        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            set => SetProperty(ref _hasStatusMessage, value);
        }

        /// <summary>
        /// 状态颜色
        /// </summary>
        public Color StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        /// <summary>
        /// 保存命令
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 重置命令
        /// </summary>
        public DelegateCommand ResetCommand { get; }

        public SystemSettingsViewModel(
            ISystemConfigService configService,
            ISessionTimeoutService timeoutService,
            IUserSession userSession,
            IAuditLogService auditLogService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _timeoutService = timeoutService ?? throw new ArgumentNullException(nameof(timeoutService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync());
            ResetCommand = new DelegateCommand(async () => await ExecuteResetAsync());
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await LoadSettingsAsync();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private async Task LoadSettingsAsync()
        {
            try
            {
                SessionTimeoutEnabled = await _configService.GetSessionTimeoutEnabledAsync();
                SessionTimeoutMinutes = await _configService.GetSessionTimeoutMinutesAsync();
                StatusMessage = null;
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                Growl.Error($"加载设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            try
            {
                // 验证权限 - 只有管理员可以修改
                if (!_userSession.HasPermission("SystemConfig:Edit"))
                {
                    await _auditLogService.LogFailureAsync(
                        _userSession.GetAuditUserId(),
                        _userSession.GetAuditUsername(),
                        AuditActionType.Update,
                        "System",
                        "保存系统设置失败",
                        "Permission denied",
                        "SystemConfig");
                    Growl.Warning("您没有权限修改系统设置");
                    return;
                }

                // 验证输入
                if (SessionTimeoutMinutes < 1)
                    SessionTimeoutMinutes = 1;
                if (SessionTimeoutMinutes > 480)
                    SessionTimeoutMinutes = 480;

                // 保存到数据库
                await _configService.SetSessionTimeoutEnabledAsync(SessionTimeoutEnabled);
                await _configService.SetSessionTimeoutMinutesAsync(SessionTimeoutMinutes);

                // 更新运行时服务
                _timeoutService.TimeoutMinutes = SessionTimeoutMinutes;

                if (SessionTimeoutEnabled)
                {
                    _timeoutService.StartMonitoring();
                }
                else
                {
                    _timeoutService.StopMonitoring();
                }

                StatusColor = Colors.Green;
                StatusMessage = "设置已保存并生效";
                Growl.Success("系统设置已保存");
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
            {
                StatusColor = Colors.Red;
                StatusMessage = $"保存失败: {ex.Message}";
                Growl.Error($"保存设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重置设置
        /// </summary>
        private async Task ExecuteResetAsync()
        {
            await LoadSettingsAsync();
            Growl.Info("设置已重置");
        }
    }
}
