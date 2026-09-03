using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dabp.Utils.Exceptions;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels;

/// <summary>
/// 审计日志ViewModel。
/// </summary>
public class AuditLogViewModel : BindableBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly IExportService _exportService;
    private readonly IUserSession _userSession;

    private ObservableCollection<AuditLog> _auditLogs = new();
    private AuditLog? _selectedLog;
    private DateTime _startDate = DateTime.Now.AddMonths(-1);
    private DateTime _endDate = DateTime.Now;
    private string _moduleFilter = string.Empty;
    private string _selectedActionType = "全部";
    private string _selectedResult = "全部";
    private bool _isLoading;
    private string _detailsText = "请选择一条审计日志";

    public ObservableCollection<AuditLog> AuditLogs
    {
        get => _auditLogs;
        set => SetProperty(ref _auditLogs, value);
    }

    public AuditLog? SelectedLog
    {
        get => _selectedLog;
        set
        {
            if (SetProperty(ref _selectedLog, value))
            {
                DetailsText = BuildDetailsText(value);
                ViewDetailsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public string ModuleFilter
    {
        get => _moduleFilter;
        set => SetProperty(ref _moduleFilter, value);
    }

    public ObservableCollection<string> ActionTypes { get; } = new(
        new[] { "全部" }.Concat(Enum.GetNames<AuditActionType>()));

    public string SelectedActionType
    {
        get => _selectedActionType;
        set => SetProperty(ref _selectedActionType, value);
    }

    public ObservableCollection<string> ResultOptions { get; } = new() { "全部", "成功", "失败" };

    public string SelectedResult
    {
        get => _selectedResult;
        set => SetProperty(ref _selectedResult, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string DetailsText
    {
        get => _detailsText;
        set => SetProperty(ref _detailsText, value);
    }

    public DelegateCommand LoadCommand { get; }

    public DelegateCommand ViewDetailsCommand { get; }

    public DelegateCommand ExportCommand { get; }

    public DelegateCommand SearchCommand { get; }

    public AuditLogViewModel(IAuditLogService auditLogService, IExportService exportService, IUserSession userSession)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

        LoadCommand = new DelegateCommand(async () => await LoadAuditLogs());
        SearchCommand = new DelegateCommand(async () => await SearchLogs());
        ViewDetailsCommand = new DelegateCommand(ViewDetails, CanViewDetails);
        ExportCommand = new DelegateCommand(async () => await Export());
    }

    private async Task LoadAuditLogs()
    {
        await LoadLogsAsync(() => _auditLogService.GetAllLogsAsync());
    }

    private async Task SearchLogs()
    {
        await LoadLogsAsync(() => _auditLogService.GetLogsByDateRangeAsync(StartDate.Date, EndDate.Date.AddDays(1).AddTicks(-1)));
    }

    private async Task LoadLogsAsync(Func<Task<List<AuditLog>>> loader)
    {
        IsLoading = true;
        try
        {
            List<AuditLog> logs = await loader();
            logs = ApplyFilters(logs);
            AuditLogs = new ObservableCollection<AuditLog>(logs);
            SelectedLog = AuditLogs.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private List<AuditLog> ApplyFilters(List<AuditLog> logs)
    {
        IEnumerable<AuditLog> filteredLogs = logs;

        if (!string.IsNullOrWhiteSpace(ModuleFilter))
        {
            string moduleFilter = ModuleFilter.Trim();
            filteredLogs = filteredLogs.Where(log => (log.Module ?? string.Empty).Contains(moduleFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (Enum.TryParse(SelectedActionType, out AuditActionType actionType))
        {
            filteredLogs = filteredLogs.Where(log => log.ActionType == actionType);
        }

        filteredLogs = SelectedResult switch
        {
            "成功" => filteredLogs.Where(log => log.IsSuccess),
            "失败" => filteredLogs.Where(log => !log.IsSuccess),
            _ => filteredLogs
        };

        return filteredLogs
            .OrderByDescending(log => log.OperationTime)
            .ToList();
    }

    private void ViewDetails()
    {
        DetailsText = BuildDetailsText(SelectedLog);
    }

    private bool CanViewDetails()
    {
        return SelectedLog is not null;
    }

    private async Task Export()
    {
        if (AuditLogs.Count == 0)
        {
            System.Windows.MessageBox.Show("没有可导出的审计日志数据", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        IsLoading = true;
        try
        {
            var options = new ExcelExportOptions
            {
                Title = "审计日志",
                ColumnDisplayNames = new Dictionary<string, string>
                {
                    { nameof(AuditLogExportRow.Id), "ID" },
                    { nameof(AuditLogExportRow.Module), "模块" },
                    { nameof(AuditLogExportRow.ActionType), "操作类型" },
                    { nameof(AuditLogExportRow.Description), "描述" },
                    { nameof(AuditLogExportRow.IsSuccess), "是否成功" },
                    { nameof(AuditLogExportRow.UserId), "用户ID" },
                    { nameof(AuditLogExportRow.Username), "用户名" },
                    { nameof(AuditLogExportRow.EntityType), "实体类型" },
                    { nameof(AuditLogExportRow.EntityId), "实体ID" },
                    { nameof(AuditLogExportRow.OperationTime), "操作时间" },
                    { nameof(AuditLogExportRow.ExecutionTime), "耗时(ms)" },
                    { nameof(AuditLogExportRow.FailureReason), "失败原因" },
                    { nameof(AuditLogExportRow.ClientIp), "客户端IP" }
                }
            };

            List<AuditLogExportRow> exportRows = AuditLogs.Select(AuditLogExportRow.FromAuditLog).ToList();
            string fileName = $"审计日志_{DateTime.Now:yyyyMMdd_HHmmss}";
            string filePath = await _exportService.ExportToExcelAsync(exportRows, fileName, options);
            await _auditLogService.LogExportAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "Audit",
                $"导出审计日志: {exportRows.Count} 条");

            var result = System.Windows.MessageBox.Show(
                $"导出成功，文件已保存到：\n{filePath}\n\n是否立即打开文件？",
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
            System.Diagnostics.Debug.WriteLine("Export audit logs was canceled.");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                AuditActionType.Export,
                "Audit",
                "导出审计日志失败",
                ex.Message,
                "AuditLog");
            System.Windows.MessageBox.Show($"导出失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine($"Export audit logs error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildDetailsText(AuditLog? log)
    {
        if (log is null)
        {
            return "请选择一条审计日志";
        }

        return string.Join(Environment.NewLine, new[]
        {
            $"ID: {log.Id}",
            $"时间: {log.OperationTime:yyyy-MM-dd HH:mm:ss}",
            $"用户: {log.Username} ({log.UserId})",
            $"模块: {log.Module}",
            $"操作: {log.ActionType}",
            $"结果: {(log.IsSuccess ? "成功" : "失败")}",
            $"描述: {log.Description}",
            $"实体: {log.EntityType} #{log.EntityId}",
            $"旧数据: {log.OldData}",
            $"新数据: {log.NewData}",
            $"失败原因: {log.FailureReason}",
            $"客户端IP: {log.ClientIp}",
            $"耗时: {log.ExecutionTime} ms"
        });
    }

    private sealed class AuditLogExportRow
    {
        public int Id { get; init; }

        public string? Module { get; init; }

        public string ActionType { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsSuccess { get; init; }

        public int UserId { get; init; }

        public string? Username { get; init; }

        public string? EntityType { get; init; }

        public int? EntityId { get; init; }

        public DateTime OperationTime { get; init; }

        public long ExecutionTime { get; init; }

        public string? FailureReason { get; init; }

        public string? ClientIp { get; init; }

        public static AuditLogExportRow FromAuditLog(AuditLog log)
        {
            return new AuditLogExportRow
            {
                Id = log.Id,
                Module = log.Module,
                ActionType = log.ActionType.ToString(),
                Description = log.Description,
                IsSuccess = log.IsSuccess,
                UserId = log.UserId,
                Username = log.Username,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                OperationTime = log.OperationTime,
                ExecutionTime = log.ExecutionTime,
                FailureReason = log.FailureReason,
                ClientIp = log.ClientIp
            };
        }
    }
}
