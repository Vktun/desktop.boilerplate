using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.WorkshopModule.ViewModels
{
    public class AlarmRecordViewModel : BindableBase, INavigationAware, IDisposable
    {
        private readonly IAlarmService _alarmService;
        private readonly IAlarmConfigService _alarmConfigService;
        private readonly IUserSession _userSession;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IExportService _exportService;
        private bool _isDisposed = false;

        #region Properties

        private ObservableCollection<AlarmRecord> _alarmRecords = new();
        public ObservableCollection<AlarmRecord> AlarmRecords
        {
            get { return _alarmRecords; }
            set { SetProperty(ref _alarmRecords, value); }
        }

        private AlarmRecord? _selectedAlarm;
        public AlarmRecord? SelectedAlarm
        {
            get { return _selectedAlarm; }
            set { SetProperty(ref _selectedAlarm, value); }
        }

        private AlarmLevel? _selectedLevelFilter;
        public AlarmLevel? SelectedLevelFilter
        {
            get { return _selectedLevelFilter; }
            set
            {
                SetProperty(ref _selectedLevelFilter, value);
                LoadCommand.RaiseCanExecuteChanged();
            }
        }

        private AlarmStatus? _selectedStatusFilter;
        public AlarmStatus? SelectedStatusFilter
        {
            get { return _selectedStatusFilter; }
            set
            {
                SetProperty(ref _selectedStatusFilter, value);
                LoadCommand.RaiseCanExecuteChanged();
            }
        }

        private DateTime? _startTimeFilter;
        public DateTime? StartTimeFilter
        {
            get { return _startTimeFilter; }
            set { SetProperty(ref _startTimeFilter, value); }
        }

        private DateTime? _endTimeFilter;
        public DateTime? EndTimeFilter
        {
            get { return _endTimeFilter; }
            set { SetProperty(ref _endTimeFilter, value); }
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set { SetProperty(ref _searchKeyword, value); }
        }

        private int _activeCount;
        public int ActiveCount
        {
            get { return _activeCount; }
            set { SetProperty(ref _activeCount, value); }
        }

        private int _criticalCount;
        public int CriticalCount
        {
            get { return _criticalCount; }
            set { SetProperty(ref _criticalCount, value); }
        }

        private int _todayCount;
        public int TodayCount
        {
            get { return _todayCount; }
            set { SetProperty(ref _todayCount, value); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get { return _totalCount; }
            set { SetProperty(ref _totalCount, value); }
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get { return _currentPage; }
            set { SetProperty(ref _currentPage, value); }
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get { return _pageSize; }
            set { SetProperty(ref _pageSize, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        // Filter options for UI
        public ObservableCollection<AlarmLevel> AlarmLevels { get; } = new ObservableCollection<AlarmLevel>
        {
            AlarmLevel.Info,
            AlarmLevel.Warning,
            AlarmLevel.Critical
        };

        public ObservableCollection<AlarmStatus> AlarmStatuses { get; } = new ObservableCollection<AlarmStatus>
        {
            AlarmStatus.Active,
            AlarmStatus.Acknowledged,
            AlarmStatus.Resolved,
            AlarmStatus.Ignored
        };

        #endregion

        #region Commands

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<AlarmRecord> AcknowledgeCommand { get; }
        public DelegateCommand<AlarmRecord> ResolveCommand { get; }
        public DelegateCommand<AlarmRecord> IgnoreCommand { get; }
        public DelegateCommand<AlarmRecord> ViewDetailCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand ClearFilterCommand { get; }
        public DelegateCommand<string> PageChangedCommand { get; }

        #endregion

        public AlarmRecordViewModel(
            IAlarmService alarmService,
            IAlarmConfigService alarmConfigService,
            IUserSession userSession,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IExportService exportService)
        {
            _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
            _alarmConfigService = alarmConfigService ?? throw new ArgumentNullException(nameof(alarmConfigService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            AlarmRecords = new ObservableCollection<AlarmRecord>();

            LoadCommand = new DelegateCommand(async () => await LoadAlarmsAsync(), CanLoad);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            AcknowledgeCommand = new DelegateCommand<AlarmRecord>(async a => await AcknowledgeAlarmAsync(a), CanAcknowledge);
            ResolveCommand = new DelegateCommand<AlarmRecord>(async a => await ResolveAlarmAsync(a), CanResolve);
            IgnoreCommand = new DelegateCommand<AlarmRecord>(async a => await IgnoreAlarmAsync(a), CanIgnore);
            ViewDetailCommand = new DelegateCommand<AlarmRecord>(ViewAlarmDetail, CanViewDetail);
            ExportCommand = new DelegateCommand(async () => await ExportAlarmsAsync());
            ClearFilterCommand = new DelegateCommand(ClearFilters);
            PageChangedCommand = new DelegateCommand<string>(async p => await OnPageChangedAsync(p));

            // Subscribe to alarm events for real-time updates
            _eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);
            _eventAggregator.GetEvent<AlarmStatusChangedEvent>().Subscribe(OnAlarmStatusChanged);
        }

        #region Navigation

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadCommand.Execute();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        #endregion

        #region Data Loading

        private bool CanLoad()
        {
            return _userSession.IsLoggedIn && !_isLoading;
        }

        private async Task LoadAlarmsAsync()
        {
            if (!_userSession.IsLoggedIn) return;

            IsLoading = true;
            try
            {
                var userId = _userSession.UserId;
                var (list, total) = await _alarmService.GetAlarmRecordsPageAsync(
                    userId,
                    CurrentPage,
                    PageSize,
                    SelectedStatusFilter,
                    SelectedLevelFilter);

                AlarmRecords = new ObservableCollection<AlarmRecord>(list);
                TotalCount = total;

                // Load statistics
                ActiveCount = await _alarmService.GetActiveAlarmCountAsync(userId);
                CriticalCount = await _alarmService.GetCriticalAlarmCountAsync(userId);
                TodayCount = await _alarmService.GetTodayAlarmCountAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load alarms error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshAsync()
        {
            CurrentPage = 1;
            await LoadAlarmsAsync();
        }

        private async Task OnPageChangedAsync(string action)
        {
            if (action == "prev" && CurrentPage > 1)
            {
                CurrentPage--;
            }
            else if (action == "next")
            {
                CurrentPage++;
            }
            await LoadAlarmsAsync();
        }

        #endregion

        #region Alarm Operations

        private bool CanAcknowledge(AlarmRecord alarm)
        {
            return alarm != null && alarm.AlarmStatus == AlarmStatus.Active && !_isLoading;
        }

        private async Task AcknowledgeAlarmAsync(AlarmRecord alarm)
        {
            if (alarm == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmService.AcknowledgeAlarmAsync(alarm.Id, _userSession.UserId);
                if (success)
                {
                    await LoadAlarmsAsync();
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

        private bool CanResolve(AlarmRecord alarm)
        {
            return alarm != null && alarm.AlarmStatus != AlarmStatus.Resolved && !_isLoading;
        }

        private async Task ResolveAlarmAsync(AlarmRecord alarm)
        {
            if (alarm == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmService.ResolveAlarmAsync(alarm.Id, _userSession.UserId);
                if (success)
                {
                    await LoadAlarmsAsync();
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

        private bool CanIgnore(AlarmRecord alarm)
        {
            return alarm != null && alarm.AlarmStatus != AlarmStatus.Resolved && alarm.AlarmStatus != AlarmStatus.Ignored && !_isLoading;
        }

        private async Task IgnoreAlarmAsync(AlarmRecord alarm)
        {
            if (alarm == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmService.IgnoreAlarmAsync(alarm.Id, _userSession.UserId);
                if (success)
                {
                    await LoadAlarmsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ignore alarm error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanViewDetail(AlarmRecord alarm)
        {
            return alarm != null;
        }

        private void ViewAlarmDetail(AlarmRecord alarm)
        {
            if (alarm == null) return;
            SelectedAlarm = alarm;
            // TODO: Navigate to detail view or show dialog
        }

        private async Task ExportAlarmsAsync()
        {
            if (AlarmRecords == null || AlarmRecords.Count == 0)
            {
                System.Windows.MessageBox.Show("没有可导出的告警数据", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            try
            {
                // 配置导出选项
                var options = new ExcelExportOptions
                {
                    Title = "告警记录",
                    ColumnDisplayNames = new Dictionary<string, string>
                    {
                        { "Id", "ID" },
                        { "AlarmCode", "告警代码" },
                        { "AlarmTitle", "告警标题" },
                        { "AlarmContent", "告警内容" },
                        { "AlarmSource", "告警来源" },
                        { "AlarmLevel", "告警等级" },
                        { "AlarmStatus", "告警状态" },
                        { "AlarmType", "告警类型" },
                        { "TriggeredTime", "触发时间" },
                        { "AcknowledgedTime", "确认时间" },
                        { "ResolvedTime", "解决时间" },
                        { "ThresholdValue", "阈值" },
                        { "ActualValue", "实际值" },
                        { "Unit", "单位" },
                        { "AcknowledgedBy", "确认人ID" },
                        { "ResolvedBy", "解决人ID" },
                        { "CreatedAt", "创建时间" }
                    },
                    ExcludedColumns = new List<string> { "UserId" },
                    EnumMappings = new Dictionary<string, Dictionary<object, string>>
                    {
                        { "Vk.Dbp.Contracts.Events.AlarmLevel", new Dictionary<object, string>
                            {
                                { AlarmLevel.Info, "信息" },
                                { AlarmLevel.Warning, "警告" },
                                { AlarmLevel.Critical, "严重" }
                            }
                        },
                        { "Vk.Dbp.Contracts.Events.AlarmStatus", new Dictionary<object, string>
                            {
                                { AlarmStatus.Active, "活跃" },
                                { AlarmStatus.Acknowledged, "已确认" },
                                { AlarmStatus.Resolved, "已解决" },
                                { AlarmStatus.Ignored, "已忽略" }
                            }
                        },
                        { "Vk.Dbp.Contracts.Events.AlarmType", new Dictionary<object, string>
                            {
                                { AlarmType.Threshold, "阈值告警" },
                                { AlarmType.Device, "设备告警" },
                                { AlarmType.Process, "流程告警" },
                                { AlarmType.System, "系统告警" },
                                { AlarmType.Safety, "安全告警" }
                            }
                        }
                    }
                };

                // 生成带时间戳的文件名
                var fileName = $"告警记录_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                var filePath = await _exportService.ExportToExcelAsync(AlarmRecords.ToList(), fileName, options);
                
                // 提示用户并询问是否打开文件
                var result = System.Windows.MessageBox.Show(
                    $"导出成功！文件已保存到：\n{filePath}\n\n是否立即打开文件？",
                    "导出完成",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await _exportService.OpenExportedFileAsync(filePath);
                }
            }
            catch (OperationCanceledException)
            {
                // 用户取消了保存操作
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Export alarms error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearFilters()
        {
            SelectedLevelFilter = null;
            SelectedStatusFilter = null;
            StartTimeFilter = null;
            EndTimeFilter = null;
            SearchKeyword = string.Empty;
            CurrentPage = 1;
            LoadCommand.Execute();
        }

        #endregion

        #region Event Handlers

        private void OnAlarmTriggered(AlarmTriggeredPayload payload)
        {
            if (_isDisposed) return;

            // Refresh when new alarm triggered
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadAlarmsAsync();
            });
        }

        private void OnAlarmStatusChanged(AlarmStatusChangedPayload payload)
        {
            if (_isDisposed) return;

            // Update UI when status changed
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadAlarmsAsync();
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
