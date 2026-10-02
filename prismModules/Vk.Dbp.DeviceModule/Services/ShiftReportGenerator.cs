using Vk.Dbp.Contracts.Extensions;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services.ReportBuilding;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 点位历史班报生成器（CustomParameters["deviceCode"] 必填；StartDate/EndDate 为班次时段）。
/// 各点位统计走 <see cref="IHistoryDataService.GetAggregateAsync"/> 服务端聚合，保证 min/max/avg 精确。
/// </summary>
public sealed class ShiftReportGenerator : IReportGenerator
{
    /// <summary>
    /// 报表类型标识
    /// </summary>
    public const string ReportTypeValue = "point-shift";

    private readonly IDeviceCatalogService _catalogService;
    private readonly IHistoryDataService _historyService;

    /// <summary>
    /// 构造班报生成器
    /// </summary>
    public ShiftReportGenerator(IDeviceCatalogService catalogService, IHistoryDataService historyService)
    {
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
    }

    /// <inheritdoc />
    public string ReportType => ReportTypeValue;

    /// <inheritdoc />
    public string DisplayName => "点位历史班报";

    /// <inheritdoc />
    public string Description => "指定时段内设备各点位的最小/最大/均值统计";

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

        if (parameters.CustomParameters.TryGetValue("deviceCode", out var value)
            && value is string deviceCode && !string.IsNullOrWhiteSpace(deviceCode))
        {
            return ValidationResult.Success();
        }

        return ValidationResult.Failure("必须指定设备编码（deviceCode）");
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateReportAsync(ReportParameters parameters)
    {
        var validation = ValidateParameters(parameters);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", validation.Errors));
        }

        var deviceCode = (parameters.CustomParameters["deviceCode"] as string)!;
        return GenerateAsync(deviceCode, parameters.StartDate!.Value, parameters.EndDate!.Value);
    }

    /// <summary>
    /// 构建班报行（预览与导出同源）
    /// </summary>
    internal async Task<List<ShiftReportRow>> BuildRowsAsync(string deviceCode, DateTime start, DateTime end)
    {
        var device = (await _catalogService.GetDevicesAsync(includeDisabled: true))
            .FirstOrDefault(candidate => string.Equals(candidate.Code, deviceCode, StringComparison.OrdinalIgnoreCase));
        if (device is null)
        {
            throw new InvalidOperationException($"设备不存在: {deviceCode}");
        }

        var points = await _catalogService.GetPointsAsync(device.Id);
        var rows = new List<ShiftReportRow>();
        foreach (var point in points.Where(point => point.IsEnabled))
        {
            var aggregate = await _historyService.GetAggregateAsync(point.Code, start, end);
            rows.Add(new ShiftReportRow
            {
                DeviceCode = deviceCode,
                PointCode = point.Code,
                PointName = point.Name,
                Unit = point.Unit,
                Min = aggregate?.Min,
                Max = aggregate?.Max,
                Avg = aggregate?.Avg,
                Count = aggregate?.Count ?? 0
            });
        }

        return rows;
    }

    private async Task<byte[]> GenerateAsync(string deviceCode, DateTime start, DateTime end)
    {
        var title = $"点位历史班报 · {deviceCode}";
        var periodText = $"班次时段：{start:yyyy-MM-dd HH:mm} ~ {end:yyyy-MM-dd HH:mm}（生成于 {DateTime.Now:yyyy-MM-dd HH:mm}）";

        var rows = await BuildRowsAsync(deviceCode, start, end);
        var columns = new ReportColumn<ShiftReportRow>[]
        {
            new("设备", row => row.DeviceCode),
            new("点位编码", row => row.PointCode),
            new("点位名称", row => row.PointName),
            new("单位", row => row.Unit ?? string.Empty),
            new("最小值", row => row.Min),
            new("最大值", row => row.Max),
            new("平均值", row => row.Avg),
            new("样本数", row => row.Count)
        };

        return ExcelReportBuilder.Build(title, periodText, rows, columns);
    }
}
