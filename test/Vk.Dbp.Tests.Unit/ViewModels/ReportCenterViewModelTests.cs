using Dabp.Infrastructure.Entities;
using FluentAssertions;
using Moq;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.DeviceModule.ViewModels;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Services.Audit;
using Xunit;

namespace Vk.Dbp.Tests.Unit.ViewModels;

public sealed class ReportCenterViewModelTests : IDisposable
{
    private readonly Mock<IAlarmService> _alarmService = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IDeviceCatalogService> _catalogService = new();
    private readonly Mock<IHistoryDataService> _historyService = new();
    private readonly Mock<IExportService> _exportService = new();

    public ReportCenterViewModelTests()
    {
        _alarmService
            .Setup(service => service.GetAlarmRecordsAsync(It.IsAny<int>(), It.IsAny<AlarmStatus?>(), It.IsAny<AlarmLevel?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<AlarmRecord>
            {
                new() { AlarmSource = "SIM-A", AlarmLevel = AlarmLevel.Critical, AlarmStatus = AlarmStatus.Active, AlarmCode = "R1", AlarmTitle = "t", TriggeredTime = DateTime.Now },
                new() { AlarmSource = "SIM-A", AlarmLevel = AlarmLevel.Warning, AlarmStatus = AlarmStatus.Resolved, AlarmCode = "R2", AlarmTitle = "t", TriggeredTime = DateTime.Now },
                new() { AlarmSource = null, AlarmLevel = AlarmLevel.Critical, AlarmStatus = AlarmStatus.Active, AlarmCode = "R3", AlarmTitle = "t", TriggeredTime = DateTime.Now }
            });
        _catalogService
            .Setup(service => service.GetDevicesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<DeviceSummary>
            {
                new() { Id = 1, Code = "SIM-A", Name = "设备A", ProtocolType = "Simulated", IsEnabled = true }
            });
        _catalogService
            .Setup(service => service.GetPointsAsync(1))
            .ReturnsAsync(new List<PointDefinition>
            {
                new() { Id = 11, DeviceId = 1, Code = "TEMP-01", Name = "温度", Address = "const(1)", IsEnabled = true }
            });
        _historyService
            .Setup(service => service.GetAggregateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new PointAggregate { PointCode = "TEMP-01", Min = 10, Max = 30, Avg = 20, Count = 5 });
        _auditLogService
            .Setup(service => service.GetLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Vk.Dbp.Services.Audit.AuditLog>
            {
                new() { Username = "admin", ActionType = AuditActionType.Create, Module = "DeviceManagement", IsSuccess = true, OperationTime = DateTime.Now }
            });
    }

    public void Dispose()
    {
        // 无 UIThread 订阅，但保持与其它 VM 测试一致的清理习惯
    }

    private TestAlarmReportViewModel CreateAlarmReport()
    {
        return new TestAlarmReportViewModel(
            new AlarmSummaryReportGenerator(_alarmService.Object),
            _exportService.Object);
    }

    [Fact]
    public void AlarmReport_TryGetWindow_DailyAndMonthly_ComputeCorrectRanges()
    {
        var viewModel = CreateAlarmReport();
        viewModel.SelectedReportKind = "日报";
        viewModel.SelectedDate = new DateTime(2026, 10, 2, 15, 30, 0);

        viewModel.TryGetWindow(out var dayStart, out var dayEnd).Should().BeTrue("已选日期应能解析时间窗");
        dayStart.Should().Be(new DateTime(2026, 10, 2, 0, 0, 0), "日报起点应为当天零点");
        dayEnd.Should().Be(new DateTime(2026, 10, 3, 0, 0, 0).AddTicks(-1), "日报终点应收口到当天末");

        viewModel.SelectedReportKind = "月报";
        viewModel.TryGetWindow(out var monthStart, out var monthEnd).Should().BeTrue("月报应能解析时间窗");
        monthStart.Should().Be(new DateTime(2026, 10, 1), "月报起点应为当月月初");
        monthEnd.Should().Be(new DateTime(2026, 11, 1).AddTicks(-1), "月报终点应为次月月初前一刻");
    }

    [Fact]
    public async Task AlarmReport_Query_FillsPreviewAndHeaderCounts()
    {
        var viewModel = CreateAlarmReport();
        viewModel.SelectedDate = DateTime.Today;

        viewModel.QueryCommand.Execute();
        await Vk.Dbp.Tests.Unit.Services.TimingTestHelper.WaitUntilAsync(
            () => viewModel.HasResult,
            diagnostics: "查询后应产出预览行");

        viewModel.Rows.Should().HaveCount(3, "3 条种子记录按来源×等级应分 3 组");
        viewModel.TotalCount.Should().Be(3, "头部合计应为全部记录数");
        viewModel.CriticalCount.Should().Be(2, "严重等级应计 2 条");
        viewModel.Messages.Should().BeEmpty("查询成功不应有错误通知");
    }

    [Fact]
    public void ShiftReport_TryGetWindow_ShiftPresets_ComputeCorrectHours()
    {
        var viewModel = new ShiftReportViewModel(
            new ShiftReportGenerator(_catalogService.Object, _historyService.Object),
            _catalogService.Object,
            _exportService.Object);
        var day = new DateTime(2026, 10, 2);

        viewModel.SelectedShift = "早班";
        viewModel.ReferenceDate = day;
        viewModel.TryGetWindow(out var start, out _).Should().BeTrue("早班应有效");
        start.Should().Be(day.AddHours(8), "早班应从 08:00 开始");

        viewModel.SelectedShift = "中班";
        viewModel.TryGetWindow(out start, out var end).Should().BeTrue("中班应有效");
        start.Should().Be(day.AddHours(16), "中班应从 16:00 开始");
        end.Should().Be(day.AddDays(1).AddTicks(-1), "中班应收口到当日 24:00");

        viewModel.SelectedShift = "夜班";
        viewModel.TryGetWindow(out start, out end).Should().BeTrue("夜班应有效");
        start.Should().Be(day, "夜班应从 00:00 开始");
        end.Should().Be(day.AddHours(8).AddTicks(-1), "夜班应收口到 08:00");

        viewModel.SelectedShift = "自定义";
        viewModel.CustomStart = day.AddHours(10);
        viewModel.CustomEnd = day.AddHours(9);
        viewModel.TryGetWindow(out _, out _).Should().BeFalse("自定义时段开始晚于结束应无效");
    }

    [Fact]
    public async Task AuditReport_Query_FiltersAndFillsPreview()
    {
        var viewModel = new TestAuditReportViewModel(
            new AuditLogReportGenerator(_auditLogService.Object),
            _exportService.Object);
        viewModel.StartDate = DateTime.Today.AddDays(-1);
        viewModel.EndDate = DateTime.Today;
        viewModel.SelectedActionType = AuditActionType.Create;

        viewModel.QueryCommand.Execute();
        await Vk.Dbp.Tests.Unit.Services.TimingTestHelper.WaitUntilAsync(
            () => viewModel.HasResult,
            diagnostics: "审计查询后应产出预览行");

        viewModel.Rows.Should().ContainSingle("Create 类型种子应保留");
        viewModel.Rows[0].ActionText.Should().Be("创建", "操作类型应映射为中文");
    }

    [Fact]
    public async Task AlarmReport_ExportExcel_WritesFileAndOpens()
    {
        var exportPath = Path.Combine(Path.GetTempPath(), $"dbp-report-{Guid.NewGuid():N}.xlsx");
        _exportService.Setup(service => service.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(exportPath);
        var viewModel = CreateAlarmReport();
        viewModel.SelectedDate = DateTime.Today;
        viewModel.QueryCommand.Execute();
        await Vk.Dbp.Tests.Unit.Services.TimingTestHelper.WaitUntilAsync(() => viewModel.HasResult, diagnostics: "等待预览结果");

        try
        {
            viewModel.ExportExcelCommand.Execute();
            await Vk.Dbp.Tests.Unit.Services.TimingTestHelper.WaitUntilAsync(
                () => File.Exists(exportPath),
                diagnostics: "导出应写出文件");

            using var stream = File.OpenRead(exportPath);
            stream.ReadByte().Should().Be('P', "xlsx 产物应为 ZIP 包（PK 头首字节）");
            _exportService.Verify(service => service.OpenExportedFileAsync(exportPath), Times.Once, "导出成功后应尝试打开文件");
            viewModel.Messages.Should().Contain(m => m.Contains("已导出"), "导出成功应发出通知");
        }
        finally
        {
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    private sealed class TestAlarmReportViewModel : AlarmReportViewModel
    {
        public TestAlarmReportViewModel(AlarmSummaryReportGenerator generator, IExportService exportService)
            : base(generator, exportService)
        {
        }

        public List<string> Messages { get; } = [];

        protected override void NotifySuccess(string message)
        {
            Messages.Add(message);
        }

        protected override void NotifyError(string message)
        {
            Messages.Add(message);
        }
    }

    private sealed class TestAuditReportViewModel : AuditReportViewModel
    {
        public TestAuditReportViewModel(AuditLogReportGenerator generator, IExportService exportService)
            : base(generator, exportService)
        {
        }
    }
}
