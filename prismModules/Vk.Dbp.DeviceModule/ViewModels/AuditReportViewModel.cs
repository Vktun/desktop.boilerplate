using System.Collections.ObjectModel;
using Prism.Commands;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.Services.Audit;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 审计报表子页：时间范围 + 操作类型/模块过滤 → 明细预览 → Excel 导出。
/// </summary>
public class AuditReportViewModel : ReportTabViewModelBase
{
    private readonly AuditLogReportGenerator _generator;

    private DateTime? _startDate = DateTime.Today.AddDays(-1);
    private DateTime? _endDate = DateTime.Today;
    private AuditActionType? _selectedActionType;
    private string _moduleText = string.Empty;
    private bool _isBusy;
    private bool _hasResult;

    /// <summary>
    /// 构造审计报表子页 VM
    /// </summary>
    public AuditReportViewModel(AuditLogReportGenerator generator, IExportService exportService)
        : base(exportService)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));

        QueryCommand = new DelegateCommand(async () => await QueryAsync(), () => StartDate.HasValue && EndDate.HasValue && StartDate < EndDate && !IsBusy)
            .ObservesProperty(() => StartDate)
            .ObservesProperty(() => EndDate)
            .ObservesProperty(() => IsBusy);
        ExportExcelCommand = new DelegateCommand(async () => await ExportAsync(), () => HasResult && !IsBusy)
            .ObservesProperty(() => HasResult)
            .ObservesProperty(() => IsBusy);
    }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    /// <summary>
    /// 操作类型过滤选项（null = 全部）
    /// </summary>
    public ObservableCollection<AuditActionType?> ActionTypeOptions { get; } = new(
    [
        null,
        AuditActionType.Create,
        AuditActionType.Update,
        AuditActionType.Delete,
        AuditActionType.Login,
        AuditActionType.Logout,
        AuditActionType.ChangePassword,
        AuditActionType.Export,
        AuditActionType.Import,
        AuditActionType.Print,
        AuditActionType.Download,
        AuditActionType.View
    ]);

    /// <summary>
    /// 选中的操作类型（null=全部）
    /// </summary>
    public AuditActionType? SelectedActionType
    {
        get => _selectedActionType;
        set => SetProperty(ref _selectedActionType, value);
    }

    /// <summary>
    /// 模块过滤（可空）
    /// </summary>
    public string ModuleText
    {
        get => _moduleText;
        set => SetProperty(ref _moduleText, value);
    }

    /// <summary>
    /// 是否正在生成
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// 是否已有结果
    /// </summary>
    public bool HasResult
    {
        get => _hasResult;
        private set => SetProperty(ref _hasResult, value);
    }

    /// <summary>
    /// 明细预览行
    /// </summary>
    public ObservableCollection<AuditReportRow> Rows { get; } = [];

    /// <summary>
    /// 查询命令
    /// </summary>
    public DelegateCommand QueryCommand { get; }

    /// <summary>
    /// 导出 Excel
    /// </summary>
    public DelegateCommand ExportExcelCommand { get; }

    /// <summary>
    /// 解析时间窗（internal 供测试； DatePicker 只有日期粒度，结束收口到当天末）
    /// </summary>
    internal bool TryGetWindow(out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        if (StartDate is not { } from || EndDate is not { } to || from > to)
        {
            return false;
        }

        start = from.Date;
        end = to.Date.AddDays(1).AddTicks(-1);
        return true;
    }

    private async Task QueryAsync()
    {
        if (!TryGetWindow(out var start, out var end))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var module = string.IsNullOrWhiteSpace(ModuleText) ? null : ModuleText.Trim();
            var rows = await _generator.BuildRowsAsync(start, end, SelectedActionType, module);
            Rows.Clear();
            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            HasResult = rows.Count > 0;
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            NotifyError($"审计报表生成失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        if (!TryGetWindow(out var start, out var end))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var parameters = new Vk.Dbp.Contracts.Extensions.ReportParameters
            {
                StartDate = start,
                EndDate = end,
                CustomParameters =
                {
                    ["actionType"] = SelectedActionType?.ToString() ?? string.Empty,
                    ["module"] = string.IsNullOrWhiteSpace(ModuleText) ? string.Empty : ModuleText.Trim()
                }
            };

            var bytes = await _generator.GenerateReportAsync(parameters);
            var defaultName = $"审计日志报表-{start:yyyyMMdd}-{end:yyyyMMdd}";
            await ExportBytesAsync(bytes, defaultName, ".xlsx");
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex)
                                    || Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex))
        {
            NotifyError($"导出失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
