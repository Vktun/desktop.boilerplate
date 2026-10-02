using System.Collections.ObjectModel;
using Prism.Commands;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 告警报表子页：日报/月报选择 → 汇总预览 → Excel/PDF 导出。
/// </summary>
public class AlarmReportViewModel : ReportTabViewModelBase
{
    private readonly AlarmSummaryReportGenerator _generator;

    private string _selectedReportKind = "日报";
    private DateTime? _selectedDate = DateTime.Today;
    private bool _isBusy;
    private bool _hasResult;
    private int _totalCount;
    private int _criticalCount;

    /// <summary>
    /// 构造告警报表子页 VM
    /// </summary>
    public AlarmReportViewModel(AlarmSummaryReportGenerator generator, IExportService exportService)
        : base(exportService)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));

        QueryCommand = new DelegateCommand(async () => await QueryAsync(), () => SelectedDate.HasValue && !IsBusy)
            .ObservesProperty(() => SelectedDate)
            .ObservesProperty(() => IsBusy);
        ExportExcelCommand = new DelegateCommand(async () => await ExportAsync("xlsx"), () => HasResult && !IsBusy)
            .ObservesProperty(() => HasResult)
            .ObservesProperty(() => IsBusy);
        ExportPdfCommand = new DelegateCommand(async () => await ExportAsync("pdf"), () => HasResult && !IsBusy)
            .ObservesProperty(() => HasResult)
            .ObservesProperty(() => IsBusy);
    }

    /// <summary>
    /// 报表种类（日报/月报）
    /// </summary>
    public ObservableCollection<string> ReportKinds { get; } = new(["日报", "月报"]);

    /// <summary>
    /// 选中的报表种类
    /// </summary>
    public string SelectedReportKind
    {
        get => _selectedReportKind;
        set => SetProperty(ref _selectedReportKind, value);
    }

    /// <summary>
    /// 报表日期（日报=当天；月报=所选日期所在月）
    /// </summary>
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
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
    /// 是否已有结果（导出开关）
    /// </summary>
    public bool HasResult
    {
        get => _hasResult;
        private set => SetProperty(ref _hasResult, value);
    }

    /// <summary>
    /// 告警总数
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    /// <summary>
    /// 严重告警数
    /// </summary>
    public int CriticalCount
    {
        get => _criticalCount;
        private set => SetProperty(ref _criticalCount, value);
    }

    /// <summary>
    /// 汇总预览行
    /// </summary>
    public ObservableCollection<AlarmSummaryRow> Rows { get; } = [];

    /// <summary>
    /// 查询命令
    /// </summary>
    public DelegateCommand QueryCommand { get; }

    /// <summary>
    /// 导出 Excel
    /// </summary>
    public DelegateCommand ExportExcelCommand { get; }

    /// <summary>
    /// 导出 PDF
    /// </summary>
    public DelegateCommand ExportPdfCommand { get; }

    /// <summary>
    /// 解析报表时间窗（internal 供测试；含端点收口为 [start, end) 语义的闭区间末尾）
    /// </summary>
    internal bool TryGetWindow(out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        if (SelectedDate is not { } date)
        {
            return false;
        }

        if (SelectedReportKind == "月报")
        {
            var monthStart = new DateTime(date.Year, date.Month, 1);
            start = monthStart;
            end = monthStart.AddMonths(1).AddTicks(-1);
        }
        else
        {
            start = date.Date;
            end = date.Date.AddDays(1).AddTicks(-1);
        }

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
            var rows = await _generator.BuildRowsAsync(start, end);
            Rows.Clear();
            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            // 头部统计只看非空行（BuildRows 空窗返回空表）
            TotalCount = rows.Sum(row => row.Count);
            CriticalCount = rows.Where(row => row.LevelText == "严重").Sum(row => row.Count);
            HasResult = rows.Count > 0;
        }
        catch (Exception ex) when (Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            NotifyError($"告警报表生成失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync(string format)
    {
        if (!TryGetWindow(out var start, out var end))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var granularity = SelectedReportKind == "月报" ? "month" : "day";
            var parameters = new Vk.Dbp.Contracts.Extensions.ReportParameters
            {
                StartDate = start,
                EndDate = end,
                CustomParameters =
                {
                    ["granularity"] = granularity,
                    ["format"] = format
                }
            };

            var bytes = await _generator.GenerateReportAsync(parameters);
            var defaultName = $"告警{SelectedReportKind}-{start:yyyyMMdd}";
            await ExportBytesAsync(bytes, defaultName, format == "pdf" ? ".pdf" : ".xlsx");
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
