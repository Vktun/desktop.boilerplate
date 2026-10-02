using Vk.Dbp.Contracts.Extensions;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services.ReportBuilding;
using Vk.Dbp.Services.Audit;
using static Vk.Dbp.Services.Audit.AuditActionType;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 审计日志报表生成器（CustomParameters 可选 "actionType"=枚举名、"module"=模块名，均支持留空）。
/// 数据口径：时间窗内全量拉回后内存过滤（审计服务无组合过滤面，报表量级可接受）。
/// </summary>
public sealed class AuditLogReportGenerator : IReportGenerator
{
    /// <summary>
    /// 报表类型标识
    /// </summary>
    public const string ReportTypeValue = "audit-log";

    private static readonly IReadOnlyDictionary<AuditActionType, string> ActionTexts =
        new Dictionary<AuditActionType, string>
        {
            [Create] = "创建",
            [Update] = "更新",
            [Delete] = "删除",
            [Login] = "登录",
            [Logout] = "登出",
            [ChangePassword] = "修改密码",
            [Export] = "导出",
            [Import] = "导入",
            [Print] = "打印",
            [Download] = "下载",
            [View] = "查看"
        };

    private readonly IAuditLogService _auditLogService;

    /// <summary>
    /// 构造审计日志报表生成器
    /// </summary>
    public AuditLogReportGenerator(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    /// <inheritdoc />
    public string ReportType => ReportTypeValue;

    /// <inheritdoc />
    public string DisplayName => "审计日志报表";

    /// <inheritdoc />
    public string Description => "指定时间窗内的操作审计记录（可按操作类型/模块过滤）";

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

        var actionType = TryGetActionType(parameters, out var parsed) ? (AuditActionType?)parsed : null;
        var module = parameters.CustomParameters.TryGetValue("module", out var moduleValue) && moduleValue is string moduleText
            ? moduleText.Trim()
            : null;

        return GenerateAsync(parameters.StartDate!.Value, parameters.EndDate!.Value, actionType, module);
    }

    /// <summary>
    /// 构建审计行（预览与导出同源；按时间倒序）
    /// </summary>
    internal async Task<List<AuditReportRow>> BuildRowsAsync(
        DateTime start,
        DateTime end,
        AuditActionType? actionType = null,
        string? module = null)
    {
        var logs = await _auditLogService.GetLogsByDateRangeAsync(start, end);

        return logs
            .Where(log => actionType is null || log.ActionType == actionType.Value)
            .Where(log => string.IsNullOrWhiteSpace(module)
                          || string.Equals(log.Module, module, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(log => log.OperationTime)
            .Select(log => new AuditReportRow
            {
                OperationTime = log.OperationTime,
                Username = log.Username ?? "--",
                ActionText = ActionTexts.GetValueOrDefault(log.ActionType, log.ActionType.ToString()),
                Module = log.Module,
                Description = log.Description,
                ResultText = log.IsSuccess ? "成功" : "失败",
                FailureReason = log.IsSuccess ? null : log.FailureReason
            })
            .ToList();
    }

    private async Task<byte[]> GenerateAsync(DateTime start, DateTime end, AuditActionType? actionType, string? module)
    {
        var title = "审计日志报表";
        var filterText = actionType is { } action ? $"，操作类型={ActionTexts.GetValueOrDefault(action, action.ToString())}" : string.Empty;
        filterText += string.IsNullOrWhiteSpace(module) ? string.Empty : $"，模块={module}";
        var periodText = $"报表期间：{start:yyyy-MM-dd HH:mm} ~ {end:yyyy-MM-dd HH:mm}{filterText}（生成于 {DateTime.Now:yyyy-MM-dd HH:mm}）";

        var rows = await BuildRowsAsync(start, end, actionType, module);
        var columns = new ReportColumn<AuditReportRow>[]
        {
            new("时间", row => row.OperationTime),
            new("操作人", row => row.Username),
            new("操作类型", row => row.ActionText),
            new("模块", row => row.Module ?? string.Empty),
            new("描述", row => row.Description ?? string.Empty),
            new("结果", row => row.ResultText),
            new("失败原因", row => row.FailureReason ?? string.Empty)
        };

        return ExcelReportBuilder.Build(title, periodText, rows, columns);
    }

    private static bool TryGetActionType(ReportParameters parameters, out AuditActionType actionType)
    {
        actionType = default;
        return parameters.CustomParameters.TryGetValue("actionType", out var value)
               && value is string text
               && Enum.TryParse<AuditActionType>(text, ignoreCase: true, out actionType);
    }
}
