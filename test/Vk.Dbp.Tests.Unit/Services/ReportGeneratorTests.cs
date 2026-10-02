using Dabp.Infrastructure.Entities;
using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Extensions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Tests.Common;
using Xunit;
using static Vk.Dbp.Services.Audit.AuditActionType;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class ReportGeneratorTests : IClassFixture<TestDatabaseFixture>
{
    private static readonly DateTime ReportDay = new(2026, 10, 2, 8, 0, 0);

    private readonly ISqlSugarClient _db;
    private readonly Mock<IAuditLogService> _auditLogService = new();

    public ReportGeneratorTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();
    }

    private void ResetDatabase()
    {
        _db.Deleteable<DeviceCommand>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<DevicePoint>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<PointHistory>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Device>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<AlarmRecord>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Dabp.Infrastructure.Entities.AuditLog>().Where(_ => true).ExecuteCommand();
    }

    private AlarmSummaryReportGenerator CreateAlarmGenerator()
    {
        // 告警查询走真实服务（fixture 库），保证分组口径与生产一致
        var alarmService = new AlarmService(
            _db,
            new Dabp.Infrastructure.Repositories.SqlSugarRepository<AlarmRecord>(_db),
            Mock.Of<IAuditLogService>(),
            Mock.Of<Vk.Dbp.Services.Session.IUserSession>());
        return new AlarmSummaryReportGenerator(alarmService);
    }

    private ShiftReportGenerator CreateShiftGenerator()
    {
        var catalog = new DeviceCatalogService(_db);
        var history = new HistoryDataService(_db, TimeProvider.System);
        return new ShiftReportGenerator(catalog, history);
    }

    private async Task SeedAlarmAsync(string source, AlarmLevel level, AlarmStatus status, DateTime triggeredTime)
    {
        await _db.Insertable(new AlarmRecord
        {
            AlarmCode = $"GEN-{Guid.NewGuid():N}",
            AlarmTitle = "测试告警",
            AlarmSource = source,
            AlarmLevel = level,
            AlarmStatus = status,
            AlarmType = AlarmType.Threshold,
            TriggeredTime = triggeredTime,
            CreatedAt = triggeredTime
        }).ExecuteCommandAsync();
    }

    private async Task<int> SeedDeviceWithPointsAsync(string deviceCode, params (string Code, string Address)[] points)
    {
        var device = new Device { Code = deviceCode, Name = deviceCode, ProtocolType = ProtocolTypes.Simulated, IsEnabled = true, CreatedAt = DateTime.Now };
        device.Id = await _db.Insertable(device).ExecuteReturnIdentityAsync();
        foreach (var (code, address) in points)
        {
            await _db.Insertable(new DevicePoint
            {
                DeviceId = device.Id,
                Code = code,
                Name = code,
                Address = address,
                IsEnabled = true,
                CreatedAt = DateTime.Now
            }).ExecuteCommandAsync();
        }

        return device.Id;
    }

    [Fact]
    public async Task AlarmSummary_BuildRows_GroupsBySourceAndLevelWithinWindow()
    {
        await SeedAlarmAsync("SIM-A", AlarmLevel.Critical, AlarmStatus.Active, ReportDay.AddHours(1));
        await SeedAlarmAsync("SIM-A", AlarmLevel.Critical, AlarmStatus.Resolved, ReportDay.AddHours(2));
        await SeedAlarmAsync("SIM-A", AlarmLevel.Warning, AlarmStatus.Active, ReportDay.AddHours(3));
        await SeedAlarmAsync("SIM-B", AlarmLevel.Info, AlarmStatus.Active, ReportDay.AddHours(4));
        await SeedAlarmAsync(null, AlarmLevel.Warning, AlarmStatus.Active, ReportDay.AddHours(5));
        await SeedAlarmAsync("SIM-A", AlarmLevel.Critical, AlarmStatus.Active, ReportDay.AddDays(-1)); // 窗外

        var rows = await CreateAlarmGenerator().BuildRowsAsync(ReportDay, ReportDay.AddDays(1).AddTicks(-1));

        rows.Should().HaveCount(4, "窗口内 5 条记录按来源×等级分 4 组");
        var criticalA = rows.Should().ContainSingle(row => row.Source == "SIM-A" && row.LevelText == "严重", "同来源同等级应合并").Subject;
        criticalA.Count.Should().Be(2, "SIM-A 严重告警窗口内有 2 条（窗外那条不计入）");
        criticalA.ResolvedCount.Should().Be(1, "其中 1 条已解决");
        criticalA.Percent.Should().Be(40, "2/5 占比应为 40%");
        rows.Should().ContainSingle(row => row.Source == "未知来源", "无来源告警应归入未知来源");
    }

    [Fact]
    public async Task AlarmSummary_GenerateReportAsync_ProducesValidExcelAndPdf()
    {
        await SeedAlarmAsync("SIM-R", AlarmLevel.Warning, AlarmStatus.Active, ReportDay.AddHours(1));
        var generator = CreateAlarmGenerator();
        var parameters = new ReportParameters
        {
            StartDate = ReportDay,
            EndDate = ReportDay.AddDays(1),
            CustomParameters = { ["granularity"] = "day" }
        };

        var excel = await generator.GenerateReportAsync(parameters);
        var pdfParameters = new ReportParameters
        {
            StartDate = ReportDay,
            EndDate = ReportDay.AddDays(1),
            CustomParameters = { ["granularity"] = "day", ["format"] = "pdf" }
        };
        var pdf = await generator.GenerateReportAsync(pdfParameters);

        excel.Take(2).Select(item => (char)item).Should().BeEquivalentTo(
            new[] { 'P', 'K' }, options => options.WithStrictOrdering(), "xlsx 产物应为 ZIP 包（PK 头）");
        pdf.Take(4).Select(item => (char)item).Should().BeEquivalentTo(
            new[] { '%', 'P', 'D', 'F' }, options => options.WithStrictOrdering(), "pdf 产物应为 PDF 文件头");
    }

    [Fact]
    public void AlarmSummary_ValidateParameters_MissingRange_Fails()
    {
        var result = CreateAlarmGenerator().ValidateParameters(new ReportParameters());

        result.IsValid.Should().BeFalse("缺少时间范围应校验失败");
        result.Errors.Should().NotBeEmpty("应给出错误说明");
    }

    [Fact]
    public async Task ShiftReport_BuildRows_ComputesPerPointAggregates()
    {
        await SeedDeviceWithPointsAsync(
            "SIM-SHIFT",
            ("TEMP-S", "const(1)"),
            ("EMPTY-S", "const(2)"));
        var baseTime = ReportDay.AddHours(1);
        await _db.Insertable(new[]
        {
            new PointHistory { PointId = 1, PointCode = "TEMP-S", Value = 10, Quality = DataQuality.Good, Timestamp = baseTime.AddMinutes(1) },
            new PointHistory { PointId = 1, PointCode = "TEMP-S", Value = 20, Quality = DataQuality.Good, Timestamp = baseTime.AddMinutes(2) },
            new PointHistory { PointId = 1, PointCode = "TEMP-S", Value = 30, Quality = DataQuality.Good, Timestamp = baseTime.AddMinutes(3) },
            new PointHistory { PointId = 1, PointCode = "TEMP-S", Value = 999, Quality = DataQuality.Good, Timestamp = baseTime.AddDays(-1) } // 窗外
        }).ExecuteCommandAsync();

        var rows = await CreateShiftGenerator().BuildRowsAsync("SIM-SHIFT", baseTime, baseTime.AddHours(4));

        rows.Should().HaveCount(2, "两个启用点位各一行");
        var temp = rows.Should().ContainSingle(row => row.PointCode == "TEMP-S").Subject;
        temp.Min.Should().Be(10, "班报最小值应为窗口内精确最小");
        temp.Max.Should().Be(20 + 10, "班报最大值应为窗口内精确最大");
        temp.Avg.Should().BeApproximately(20, 0.0001, "班报均值应为窗口内精确均值");
        temp.Count.Should().Be(3, "窗外样本不计入");
        var empty = rows.Should().ContainSingle(row => row.PointCode == "EMPTY-S").Subject;
        empty.Count.Should().Be(0, "无样本点位样本数为 0");
        empty.Min.Should().BeNull("无样本点位统计应为空");
    }

    [Fact]
    public async Task AuditReport_BuildRows_FiltersAndOrdersWithFailureText()
    {
        _auditLogService
            .Setup(service => service.GetLogsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Vk.Dbp.Services.Audit.AuditLog>
            {
                new() { Username = "admin", ActionType = Create, Module = "DeviceManagement", Description = "创建设备", IsSuccess = true, OperationTime = ReportDay.AddHours(1) },
                new() { Username = "op", ActionType = Delete, Module = "DeviceManagement", Description = "删除角色", IsSuccess = false, FailureReason = "权限不足", OperationTime = ReportDay.AddHours(3) },
                new() { Username = "admin", ActionType = Export, Module = "Alarm", Description = "导出告警", IsSuccess = true, OperationTime = ReportDay.AddHours(2) }
            });

        var generator = new AuditLogReportGenerator(_auditLogService.Object);
        var rows = await generator.BuildRowsAsync(ReportDay, ReportDay.AddDays(1));
        var filtered = await generator.BuildRowsAsync(ReportDay, ReportDay.AddDays(1), Delete, null);

        rows.Should().HaveCount(3, "无过滤时应返回全部");
        rows[0].Username.Should().Be("op", "行应按时间倒序");
        rows.Should().ContainSingle(row => row.ResultText == "失败" && row.FailureReason == "权限不足", "失败记录应带原因");
        rows.Should().ContainSingle(row => row.ActionText == "创建", "操作类型应有中文映射");
        filtered.Should().ContainSingle("按操作类型过滤后应只剩一条");
        filtered[0].ActionText.Should().Be("删除", "过滤应保留 Delete 类型");
    }
}
