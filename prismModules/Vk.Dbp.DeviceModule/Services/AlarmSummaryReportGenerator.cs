using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Extensions;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services.ReportBuilding;
using Vk.Dbp.Services.Alarm;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 告警汇总报表生成器（日报/月报共用；CustomParameters["granularity"]="day"|"month" 影响标题，
/// CustomParameters["format"]="xlsx"|"pdf" 选择输出格式，默认 xlsx）。
/// 数据口径：时间窗内全部全局告警按（来源×等级）内存分组（报表时间窗量级可接受，仓库既有风格）。
/// </summary>
public sealed class AlarmSummaryReportGenerator : IReportGenerator
{
    /// <summary>
    /// 报表类型标识
    /// </summary>
    public const string ReportTypeValue = "alarm-summary";

    private static readonly IReadOnlyDictionary<AlarmLevel, string> LevelTexts = new Dictionary<AlarmLevel, string>
    {
        [AlarmLevel.Info] = "信息",
        [AlarmLevel.Warning] = "警告",
        [AlarmLevel.Critical] = "严重"
    };

    private readonly IAlarmService _alarmService;

    /// <summary>
    /// 构造告警汇总报表生成器
    /// </summary>
    public AlarmSummaryReportGenerator(IAlarmService alarmService)
    {
        _alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
    }

    /// <inheritdoc />
    public string ReportType => ReportTypeValue;

    /// <inheritdoc />
    public string DisplayName => "告警汇总报表";

    /// <inheritdoc />
    public string Description => "按设备来源与等级汇总指定时间窗内的告警（日报/月报）";

    /// <inheritdoc />
    public ValidationResult ValidateParameters(ReportParameters parameters)
    {
        if (parameters.StartDate is null || parameters.EndDate is null)
        {
            return ValidationResult.Failure("开始与结束时间不能为空");
        }

        if (parameters.StartDate >= parameters.EndDate)
        {
            return ValidationResult.Failure("开始时间必须早于结束时间");
        }

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateReportAsync(ReportParameters parameters)
    {
        var validation = ValidateParameters(parameters);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        }

        var start = parameters.StartDate!.Value;
        var end = parameters.EndDate!.Value;
        var granularity = GetCustomParameter(parameters, "granularity") as string ?? "day";
        var format = GetCustomParameter(parameters, "format") as string ?? "xlsx";

        return GenerateAsync(start, end, granularity, format);
    }

    /// <summary>
    /// 构建汇总行（预览与导出同源；internal 供报表中心页复用）
    /// </summary>
    internal async Task<List<AlarmSummaryRow>> BuildRowsAsync(DateTime start, DateTime end)
    {
        // UserId=0 取全部全局告警；时间过滤按 TriggeredTime（含端点，调用方窗口自行收口）
        var records = await _alarmService.GetAlarmRecordsAsync(0, null, null, start, end);
        if (records.Count == 0)
        {
            return [];
        }

        var total = records.Count;
        return records
            .GroupBy(record => new
            {
                Source = record.AlarmSource ?? "未知来源",
                record.AlarmLevel
            })
            .Select(group => new AlarmSummaryRow
            {
                Source = group.Key.Source,
                LevelText = LevelTexts.GetValueOrDefault(group.Key.AlarmLevel, group.Key.AlarmLevel.ToString()),
                Count = group.Count(),
                Percent = Math.Round(group.Count() * 100.0 / total, 1),
                ActiveCount = group.Count(record => record.AlarmStatus == AlarmStatus.Active),
                ResolvedCount = group.Count(record => record.AlarmStatus == AlarmStatus.Resolved)
            })
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Source)
            .ToList();
    }

    private async Task<byte[]> GenerateAsync(DateTime start, DateTime end, string granularity, string format)
    {
        var kindText = granularity.Equals("month", StringComparison.OrdinalIgnoreCase) ? "月报" : "日报";
        var title = $"告警{kindText}";
        var periodText = $"报表期间：{start:yyyy-MM-dd HH:mm} ~ {end:yyyy-MM-dd HH:mm}（生成于 {DateTime.Now:yyyy-MM-dd HH:mm}）";

        var rows = await BuildRowsAsync(start, end);
        if (rows.Count == 0)
        {
            rows.Add(new AlarmSummaryRow { Source = "（无告警）", LevelText = "-", Percent = 0 });
        }
        else
        {
            rows.Add(new AlarmSummaryRow
            {
                Source = "总计",
                LevelText = "-",
                Count = rows.Sum(row => row.Count),
                Percent = 100,
                ActiveCount = rows.Sum(row => row.ActiveCount),
                ResolvedCount = rows.Sum(row => row.ResolvedCount)
            });
        }

        var columns = new ReportColumn<AlarmSummaryRow>[]
        {
            new("告警来源", row => row.Source),
            new("等级", row => row.LevelText),
            new("数量", row => row.Count),
            new("占比(%)", row => row.Percent),
            new("活跃", row => row.ActiveCount),
            new("已解决", row => row.ResolvedCount)
        };

        return format.Equals("pdf", StringComparison.OrdinalIgnoreCase)
            ? PdfReportBuilder.Build(title, periodText, rows, columns)
            : ExcelReportBuilder.Build(title, periodText, rows, columns);
    }

    private static object? GetCustomParameter(ReportParameters parameters, string key)
    {
        return parameters.CustomParameters.TryGetValue(key, out var value) ? value : null;
    }
}
