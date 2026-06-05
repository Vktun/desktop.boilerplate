using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Constants;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.WpfWindow.ViewModels
{
    /// <summary>
    /// 全局告警弹窗ViewModel - 用于HeaderView告警图标点击后展示
    /// </summary>
    public class AppAlarmViewModel : BindableBase, IDisposable
    {
        private readonly IAlarmService _alarmService;
        private readonly IUserSession _userSession;
        private readonly INavigationService _navigationService;
        private readonly IEventAggregator _eventAggregator;
        private bool _isDisposed = false;

        #region Properties

        private ObservableCollection<AlarmRecord> _activeAlarms = new();
        public ObservableCollection<AlarmRecord> ActiveAlarms
        {
            get { return _activeAlarms; }
            set { SetProperty(ref _activeAlarms, value); }
        }

        private AlarmRecord? _selectedAlarm;
        public AlarmRecord? SelectedAlarm
        {
            get { return _selectedAlarm; }
            set { SetProperty(ref _selectedAlarm, value); }
        }

        private int _activeAlarmCount;
        public int ActiveAlarmCount
        {
            get { return _activeAlarmCount; }
            set { SetProperty(ref _activeAlarmCount, value); }
        }

        private int _criticalAlarmCount;
        public int CriticalAlarmCount
        {
            get { return _criticalAlarmCount; }
            set { SetProperty(ref _criticalAlarmCount, value); }
        }

        private bool _hasCriticalAlarm;
        public bool HasCriticalAlarm
        {
            get { return _hasCriticalAlarm; }
            set { SetProperty(ref _hasCriticalAlarm, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        #endregion

        #region Commands

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<AlarmRecord?> AcknowledgeCommand { get; }
        public DelegateCommand AcknowledgeAllCommand { get; }
        public DelegateCommand<AlarmRecord?> ResolveCommand { get; }
        public DelegateCommand NavigateToDetailCommand { get; }
        public DelegateCommand OpenConfigCommand { get; }

        #endregion

        public AppAlarmViewModel(
            IAlarmService alarmService,
            IUserSession userSession,
            INavigationService navigationService,
            IEventAggregator eventAggregator)
        {
            _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            ActiveAlarms = new ObservableCollection<AlarmRecord>();

            LoadCommand = new DelegateCommand(async () => await LoadActiveAlarmsAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            AcknowledgeCommand = new DelegateCommand<AlarmRecord?>(async a => await AcknowledgeAlarmAsync(a), CanAcknowledge);
            AcknowledgeAllCommand = new DelegateCommand(async () => await AcknowledgeAllAlarmsAsync(), CanAcknowledgeAll);
            ResolveCommand = new DelegateCommand<AlarmRecord?>(async a => await ResolveAlarmAsync(a), CanResolve);
            NavigateToDetailCommand = new DelegateCommand(NavigateToAlarmRecord, CanNavigate);
            OpenConfigCommand = new DelegateCommand(NavigateToAlarmConfig, CanNavigate);

            // Subscribe to alarm events for real-time updates
            _eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);
            _eventAggregator.GetEvent<AlarmStatusChangedEvent>().Subscribe(OnAlarmStatusChanged);

            // Initial load
            LoadCommand.Execute();
        }

        #region Data Loading

        private async Task LoadActiveAlarmsAsync()
        {
            if (!_userSession.IsLoggedIn) return;

            IsLoading = true;
            try
            {
                var userId = _userSession.UserId;

                // Get active and acknowledged alarms (not resolved)
                var alarms = await _alarmService.GetAlarmRecordsAsync(
                    userId,
                    null, // all status except resolved
                    null,
                    null,
                    null);

                // Filter to show only active and acknowledged
                var filteredAlarms = alarms
                    .Where(a => a.AlarmStatus != AlarmStatus.Resolved)
                    .OrderByDescending(a => a.AlarmLevel)
                    .ThenByDescending(a => a.TriggeredTime)
                    .ToList();

                ActiveAlarms = new ObservableCollection<AlarmRecord>(filteredAlarms);

                // Update counts
                ActiveAlarmCount = filteredAlarms.Count(a => a.AlarmStatus == AlarmStatus.Active);
                CriticalAlarmCount = filteredAlarms.Count(a => a.AlarmLevel == AlarmLevel.Critical);
                HasCriticalAlarm = CriticalAlarmCount > 0;

                // Publish count changed event for HeaderView Badge update
                _eventAggregator.GetEvent<AlarmCountChangedEvent>().Publish(new AlarmCountChangedPayload
                {
                    ActiveCount = ActiveAlarmCount,
                    CriticalCount = CriticalAlarmCount,
                    HasCriticalAlarm = HasCriticalAlarm,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load active alarms error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshAsync()
        {
            await LoadActiveAlarmsAsync();
        }

        #endregion

        #region Alarm Operations

        private bool CanAcknowledge(AlarmRecord? alarm)
        {
            return alarm != null && alarm.AlarmStatus == AlarmStatus.Active && !_isLoading;
        }

        private async Task AcknowledgeAlarmAsync(AlarmRecord? alarm)
        {
            if (alarm == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmService.AcknowledgeAlarmAsync(alarm.Id, _userSession.UserId);
                if (success)
                {
                    await LoadActiveAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Acknowledge alarm error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanAcknowledgeAll()
        {
            return ActiveAlarmCount > 0 && !_isLoading;
        }

        private async Task AcknowledgeAllAlarmsAsync()
        {
            IsLoading = true;
            try
            {
                var count = await _alarmService.AcknowledgeAllAsync(_userSession.UserId);
                if (count > 0)
                {
                    await LoadActiveAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Acknowledge all alarms error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanResolve(AlarmRecord? alarm)
        {
            return alarm != null && alarm.AlarmStatus != AlarmStatus.Resolved && !_isLoading;
        }

        private async Task ResolveAlarmAsync(AlarmRecord? alarm)
        {
            if (alarm == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmService.ResolveAlarmAsync(alarm.Id, _userSession.UserId);
                if (success)
                {
                    await LoadActiveAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resolve alarm error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanNavigate()
        {
            return _userSession.IsLoggedIn;
        }

        private void NavigateToAlarmRecord()
        {
            _navigationService.NavigateTo(ViewNames.AlarmRecord);
            CloseAlarmPopup();
        }

        private void NavigateToAlarmConfig()
        {
            _navigationService.NavigateTo(ViewNames.AlarmConfigView);
            CloseAlarmPopup();
        }

        private void CloseAlarmPopup()
        {
            // Close the notification popup - HandyControl will handle this
            // when the user interacts with the notification area
        }

        #endregion

        #region Event Handlers

        private void OnAlarmTriggered(AlarmTriggeredPayload payload)
        {
            if (_isDisposed) return;

            // Refresh when new alarm triggered
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadActiveAlarmsAsync();
            });
        }

        private void OnAlarmStatusChanged(AlarmStatusChangedPayload payload)
        {
            if (_isDisposed) return;

            // Update UI when status changed
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadActiveAlarmsAsync();
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            _eventAggregator.GetEvent<AlarmTriggeredEvent>().Unsubscribe(OnAlarmTriggered);
            _eventAggregator.GetEvent<AlarmStatusChangedEvent>().Unsubscribe(OnAlarmStatusChanged);

            _isDisposed = true;
        }

        #endregion
    }
}
