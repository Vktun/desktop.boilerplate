using Dabp.Infrastructure.Entities;
using Dabp.Infrastructure.Repositories;
using FluentAssertions;
using Moq;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public class AlarmServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUserSession> _userSession = new();
    private AlarmService _service = null!;

    public AlarmServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        SetupAuditLogService();
        SetupUserSession();
        ResetDatabase();
        _service = CreateService();
    }

    private AlarmService CreateService() => new(
        _fixture.Database,
        new SqlSugarRepository<AlarmRecord>(_fixture.Database),
        _auditLogService.Object,
        _userSession.Object);

    private void SetupAuditLogService()
    {
        _auditLogService
            .Setup(x => x.LogOperationAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        _auditLogService
            .Setup(x => x.LogFailureAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private void SetupUserSession()
    {
        _userSession.SetupGet(x => x.IsLoggedIn).Returns(true);
        _userSession.SetupGet(x => x.UserId).Returns(1);
        _userSession.SetupGet(x => x.Username).Returns("operator");
    }

    private void ResetDatabase()
    {
        _fixture.Database.Deleteable<AlarmRecord>().ExecuteCommand();
    }

    private static AlarmRecord NewAlarm(
        string title,
        AlarmLevel level = AlarmLevel.Info,
        int userId = 1) =>
        new()
        {
            AlarmCode = $"AC-{Guid.NewGuid():N}",
            AlarmTitle = title,
            AlarmLevel = level,
            UserId = userId
        };

    [Fact]
    public async Task CreateAlarmAsync_NewRecord_ForcesActiveStatusAndPersists()
    {
        var record = NewAlarm("温度超限");

        bool result = await _service.CreateAlarmAsync(record);

        result.Should().BeTrue("创建有效告警应返回成功");
        var stored = await _service.GetAlarmByIdAsync(record.Id);
        stored.Should().NotBeNull("创建后应能按 ID 查询到");
        stored!.AlarmStatus.Should().Be(AlarmStatus.Active, "新建告警必须强制为活跃状态");
        stored.TriggeredTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5), "触发时间应由服务端生成");
    }

    [Fact]
    public async Task GetAlarmRecordsAsync_StatusFilter_ReturnsOwnAndGlobalActiveOnly()
    {
        var own = NewAlarm("用户告警");
        var toResolve = NewAlarm("待解决");
        var global = NewAlarm("全局告警", userId: 0);
        var otherUser = NewAlarm("他人告警", userId: 2);
        await _service.CreateAlarmAsync(own);
        await _service.CreateAlarmAsync(toResolve);
        await _service.CreateAlarmAsync(global);
        await _service.CreateAlarmAsync(otherUser);
        await _service.ResolveAlarmAsync(toResolve.Id, 1);

        var records = await _service.GetAlarmRecordsAsync(1, status: AlarmStatus.Active);

        records.Should().OnlyContain(a => a.AlarmStatus == AlarmStatus.Active, "状态过滤后只应包含活跃告警");
        records.Select(a => a.AlarmTitle).Should().BeEquivalentTo(
            new[] { "用户告警", "全局告警" },
            "应包含自己的活跃告警与全局告警（UserId=0），排除他人与已解决告警");
    }

    [Fact]
    public async Task AcknowledgeAlarmAsync_ActiveAlarm_MarksAcknowledgedAndAudits()
    {
        var alarm = NewAlarm("待确认");
        await _service.CreateAlarmAsync(alarm);

        bool result = await _service.AcknowledgeAlarmAsync(alarm.Id, 1);

        result.Should().BeTrue("确认活跃告警应返回成功");
        var stored = await _service.GetAlarmByIdAsync(alarm.Id);
        stored!.AlarmStatus.Should().Be(AlarmStatus.Acknowledged, "确认后状态应变为已确认");
        stored.AcknowledgedBy.Should().Be(1, "应记录确认人");
        stored.AcknowledgedTime.Should().NotBeNull("应记录确认时间");
        _auditLogService.Verify(
            x => x.LogOperationAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once,
            "确认成功应记录操作审计");
    }

    [Fact]
    public async Task AcknowledgeAlarmAsync_AlarmNotFound_ReturnsFalseAndLogsFailure()
    {
        bool result = await _service.AcknowledgeAlarmAsync(9999, 1);

        result.Should().BeFalse("确认不存在的告警应返回失败");
        _auditLogService.Verify(
            x => x.LogFailureAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()),
            Times.Once,
            "确认失败应记录失败审计并留痕原因");
    }

    [Fact]
    public async Task ResolveAlarmAsync_AlreadyResolved_ReturnsFalseAndLogsFailure()
    {
        var alarm = NewAlarm("已解决告警");
        await _service.CreateAlarmAsync(alarm);
        (await _service.ResolveAlarmAsync(alarm.Id, 1)).Should().BeTrue("首次解决应成功");

        bool result = await _service.ResolveAlarmAsync(alarm.Id, 1);

        result.Should().BeFalse("重复解决同一告警应返回失败");
        _auditLogService.Verify(
            x => x.LogFailureAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()),
            Times.Once,
            "重复解决应记录失败审计");
    }

    [Fact]
    public async Task IgnoreAlarmAsync_ActiveAlarm_MarksIgnored()
    {
        var alarm = NewAlarm("待忽略");
        await _service.CreateAlarmAsync(alarm);

        bool result = await _service.IgnoreAlarmAsync(alarm.Id, 1);

        result.Should().BeTrue("忽略活跃告警应返回成功");
        var stored = await _service.GetAlarmByIdAsync(alarm.Id);
        stored!.AlarmStatus.Should().Be(AlarmStatus.Ignored, "忽略后状态应变为已忽略");
        stored.ResolvedBy.Should().Be(1, "应记录忽略操作人");
    }

    [Fact]
    public async Task AcknowledgeAllAsync_MixedActiveAlarms_AcknowledgesVisibleOnesOnly()
    {
        await _service.CreateAlarmAsync(NewAlarm("自己的告警 1"));
        await _service.CreateAlarmAsync(NewAlarm("自己的告警 2"));
        await _service.CreateAlarmAsync(NewAlarm("全局告警", userId: 0));
        var otherUserAlarm = NewAlarm("他人告警", userId: 2);
        await _service.CreateAlarmAsync(otherUserAlarm);

        int count = await _service.AcknowledgeAllAsync(1);

        count.Should().Be(3, "应确认自己的 2 条与全局 1 条，不含他人告警");
        (await _service.GetAlarmByIdAsync(otherUserAlarm.Id))!.AlarmStatus
            .Should().Be(AlarmStatus.Active, "他人告警不应被批量确认");
        _auditLogService.Verify(
            x => x.LogOperationAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Exactly(4),
            "3 条逐条确认 + 1 条批量汇总，共 4 次操作审计");
    }

    [Fact]
    public async Task GetCriticalAlarmCountAsync_MixedStatusAndLevel_CountsActiveCriticalOnly()
    {
        await _service.CreateAlarmAsync(NewAlarm("严重-活跃", AlarmLevel.Critical));
        await _service.CreateAlarmAsync(NewAlarm("一般-活跃"));
        var ignoredCritical = NewAlarm("严重-已忽略", AlarmLevel.Critical);
        await _service.CreateAlarmAsync(ignoredCritical);
        await _service.IgnoreAlarmAsync(ignoredCritical.Id, 1);

        int criticalCount = await _service.GetCriticalAlarmCountAsync(1);
        int activeCount = await _service.GetActiveAlarmCountAsync(1);

        criticalCount.Should().Be(1, "严重告警计数只应统计活跃的 Critical 级别");
        activeCount.Should().Be(2, "活跃计数应包含所有级别的活跃告警");
    }

    [Fact]
    public async Task GetAlarmRecordsPageAsync_MoreRecordsThanPageSize_ReturnsSliceAndTotal()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreateAlarmAsync(NewAlarm($"告警 {i}"));
        }

        var (list, total) = await _service.GetAlarmRecordsPageAsync(1, pageIndex: 2, pageSize: 2);

        list.Should().HaveCount(2, "分页应只返回当前页记录");
        total.Should().Be(5, "总数应反映全部匹配记录");
    }
}
