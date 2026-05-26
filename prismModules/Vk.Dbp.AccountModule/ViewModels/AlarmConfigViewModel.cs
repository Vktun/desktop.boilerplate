using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 告警配置管理ViewModel
    /// </summary>
    public class AlarmConfigViewModel : BindableBase
    {
        private readonly IAlarmConfigService _alarmConfigService;
        private readonly IUserSession _userSession;

        #region Properties

        private ObservableCollection<AlarmConfig> _alarmConfigs = new();
        public ObservableCollection<AlarmConfig> AlarmConfigs
        {
            get { return _alarmConfigs; }
            set { SetProperty(ref _alarmConfigs, value); }
        }

        private AlarmConfig? _selectedConfig;
        public AlarmConfig? SelectedConfig
        {
            get { return _selectedConfig; }
            set
            {
                SetProperty(ref _selectedConfig, value);
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        private AlarmConfig? _editingConfig;
        public AlarmConfig? EditingConfig
        {
            get { return _editingConfig; }
            set { SetProperty(ref _editingConfig, value); }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                SetProperty(ref _isEditing, value);
                RaisePropertyChanged(nameof(IsNotEditing));
            }
        }

        public bool IsNotEditing => !IsEditing;

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _dialogMessage = string.Empty;
        public string DialogMessage
        {
            get { return _dialogMessage; }
            set { SetProperty(ref _dialogMessage, value); }
        }

        private bool _showDialog;
        public bool ShowDialog
        {
            get { return _showDialog; }
            set { SetProperty(ref _showDialog, value); }
        }

        // Comparison type options for UI
        public ObservableCollection<string> ComparisonTypes { get; } = new ObservableCollection<string>
        {
            "GreaterThan",
            "LessThan",
            "InRange",
            "OutOfRange",
            "Equal",
            "NotEqual"
        };

        #endregion

        #region Commands

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand CloseDialogCommand { get; }

        #endregion

        public AlarmConfigViewModel(
            IAlarmConfigService alarmConfigService,
            IUserSession userSession)
        {
            _alarmConfigService = alarmConfigService ?? throw new ArgumentNullException(nameof(alarmConfigService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            AlarmConfigs = new ObservableCollection<AlarmConfig>();

            LoadCommand = new DelegateCommand(async () => await LoadConfigsAsync());
            AddCommand = new DelegateCommand(AddConfig);
            EditCommand = new DelegateCommand(EditConfig, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveConfigAsync(), CanSave);
            DeleteCommand = new DelegateCommand(async () => await DeleteConfigAsync(), CanDelete);
            CancelCommand = new DelegateCommand(CancelEdit);
            CloseDialogCommand = new DelegateCommand(() => ShowDialog = false);

            // Initial load
            LoadCommand.Execute();
        }

        #region Data Loading

        private async Task LoadConfigsAsync()
        {
            IsLoading = true;
            try
            {
                var configs = await _alarmConfigService.GetAlarmConfigsAsync();
                AlarmConfigs = new ObservableCollection<AlarmConfig>(configs);
            }
            catch (Exception ex)
            {
                ShowMessage($"加载配置失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region CRUD Operations

        private void AddConfig()
        {
            EditingConfig = new AlarmConfig
            {
                AlarmCode = "",
                AlarmName = "",
                IsEnabled = true,
                EnablePopup = true,
                AutoAcknowledge = false,
                Priority = 0,
                AcknowledgeTimeout = 30
            };
            IsEditing = true;
        }

        private bool CanEdit()
        {
            return SelectedConfig != null;
        }

        private void EditConfig()
        {
            if (SelectedConfig == null) return;

            // Create a copy for editing
            EditingConfig = new AlarmConfig
            {
                Id = SelectedConfig.Id,
                AlarmCode = SelectedConfig.AlarmCode,
                AlarmName = SelectedConfig.AlarmName,
                Description = SelectedConfig.Description,
                ThresholdMin = SelectedConfig.ThresholdMin,
                ThresholdMax = SelectedConfig.ThresholdMax,
                ThresholdUnit = SelectedConfig.ThresholdUnit,
                ComparisonType = SelectedConfig.ComparisonType,
                EnablePopup = SelectedConfig.EnablePopup,
                EnableSound = SelectedConfig.EnableSound,
                SoundFilePath = SelectedConfig.SoundFilePath,
                AutoAcknowledge = SelectedConfig.AutoAcknowledge,
                AcknowledgeTimeout = SelectedConfig.AcknowledgeTimeout,
                DisplayColor = SelectedConfig.DisplayColor,
                Priority = SelectedConfig.Priority,
                IsEnabled = SelectedConfig.IsEnabled,
                CreatedAt = SelectedConfig.CreatedAt
            };
            IsEditing = true;
        }

        private bool CanSave()
        {
            return EditingConfig != null && !string.IsNullOrWhiteSpace(EditingConfig.AlarmCode) && !string.IsNullOrWhiteSpace(EditingConfig.AlarmName);
        }

        private async Task SaveConfigAsync()
        {
            if (EditingConfig == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmConfigService.SaveAlarmConfigAsync(EditingConfig);
                if (success)
                {
                    IsEditing = false;
                    EditingConfig = null;
                    await LoadConfigsAsync();
                    ShowMessage("配置保存成功");
                }
                else
                {
                    ShowMessage("配置保存失败");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"保存失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanDelete()
        {
            return SelectedConfig != null && SelectedConfig.Id > 0;
        }

        private async Task DeleteConfigAsync()
        {
            if (SelectedConfig == null) return;

            IsLoading = true;
            try
            {
                var success = await _alarmConfigService.DeleteAlarmConfigAsync(SelectedConfig.Id);
                if (success)
                {
                    SelectedConfig = null;
                    await LoadConfigsAsync();
                    ShowMessage("配置删除成功");
                }
                else
                {
                    ShowMessage("配置删除失败");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"删除失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            EditingConfig = null;
        }

        #endregion

        #region Helpers

        private void ShowMessage(string message)
        {
            DialogMessage = message;
            ShowDialog = true;
        }

        #endregion
    }
}
