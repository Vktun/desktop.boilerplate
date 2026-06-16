using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Utils.Exceptions;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Audit;

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 审计日志ViewModel
    /// </summary>
    public class AuditLogViewModel : BindableBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IExportService _exportService;

        private ObservableCollection<AuditLog> _auditLogs = new();
        public ObservableCollection<AuditLog> AuditLogs
        {
            get { return _auditLogs; }
            set { SetProperty(ref _auditLogs, value); }
        }

        private AuditLog? _selectedLog;
        public AuditLog? SelectedLog
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
        public DelegateCommand<AuditLog?> ViewDetailsCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand SearchCommand { get; }

        public AuditLogViewModel(IAuditLogService auditLogService, IExportService exportService)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            AuditLogs = new ObservableCollection<AuditLog>();

            LoadCommand = new DelegateCommand(async () => await LoadAuditLogs());
            ViewDetailsCommand = new DelegateCommand<AuditLog?>(ViewDetails, CanViewDetails);
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

        private void ViewDetails(AuditLog? log)
        {
            if (log == null)
                return;
            // TODO: 打开日志详情对话框
        }

        private bool CanViewDetails(AuditLog? log)
        {
            return log != null;
        }

        private async Task Export()
        {
            if (AuditLogs == null || AuditLogs.Count == 0)
            {
                System.Windows.MessageBox.Show("没有可导出的审计日志数据", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            try
            {
                // 配置导出选项
                var options = new ExcelExportOptions
                {
                    Title = "审计日志",
                    ColumnDisplayNames = new Dictionary<string, string>
                    {
                        { "Id", "ID" },
                        { "ModuleName", "模块名称" },
                        { "ServiceName", "服务名称" },
                        { "MethodName", "方法名称" },
                        { "IsSuccess", "是否成功" },
                        { "Parameters", "参数" },
                        { "UserId", "用户ID" },
                        { "UserName", "用户名" },
                        { "ExecutionTime", "执行时间" },
                        { "ExecutionDuration", "执行时长(ms)" },
                        { "Exceptions", "异常信息" }
                    }
                };

                // 生成带时间戳的文件名
                var fileName = $"审计日志_{DateTime.Now:yyyyMMdd_HHmmss}";
                
                var filePath = await _exportService.ExportToExcelAsync(AuditLogs.ToList(), fileName, options);
                
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
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                System.Windows.MessageBox.Show($"导出失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Export audit logs error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
