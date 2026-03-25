using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.Core.Audit;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 审计日志ViewModel
    /// </summary>
    public class AuditLogViewModel : BindableBase
    {
        private readonly IAuditLogService _auditLogService;

        private ObservableCollection<AuditLog> _auditLogs;
        public ObservableCollection<AuditLog> AuditLogs
        {
            get { return _auditLogs; }
            set { SetProperty(ref _auditLogs, value); }
        }

        private AuditLog _selectedLog;
        public AuditLog SelectedLog
        {
            get { return _selectedLog; }
            set { SetProperty(ref _selectedLog, value); }
        }

        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get { return _startDate; }
            set { SetProperty(ref _startDate, value); }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get { return _endDate; }
            set { SetProperty(ref _endDate, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand<AuditLog> ViewDetailsCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand SearchCommand { get; }

        public AuditLogViewModel(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            AuditLogs = new ObservableCollection<AuditLog>();

            LoadCommand = new DelegateCommand(async () => await LoadAuditLogs());
            ViewDetailsCommand = new DelegateCommand<AuditLog>(ViewDetails, CanViewDetails);
            ExportCommand = new DelegateCommand(async () => await Export());
            SearchCommand = new DelegateCommand(async () => await SearchLogs());
        }

        private async Task LoadAuditLogs()
        {
            IsLoading = true;
            try
            {
                var logs = await _auditLogService.GetAllLogsAsync();
                AuditLogs = new ObservableCollection<AuditLog>(logs);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchLogs()
        {
            IsLoading = true;
            try
            {
                var logs = await _auditLogService.GetLogsByDateRangeAsync(StartDate, EndDate);
                AuditLogs = new ObservableCollection<AuditLog>(logs);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ViewDetails(AuditLog log)
        {
            if (log == null)
                return;
            // TODO: 打开日志详情对话框
        }

        private bool CanViewDetails(AuditLog log)
        {
            return log != null;
        }

        private async Task Export()
        {
            // TODO: 导出审计日志
        }
    }
}