using System.Text;
using System.Text.Json;
using FluentAssertions;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Tests.Common;
using Xunit;
using AuditLogEntity = Dabp.Infrastructure.Entities.AuditLog;

namespace Vk.Dbp.Tests.Unit.Services;

public class DbAuditLogServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private DbAuditLogService _service = null!;

    public DbAuditLogServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ResetDatabase();
        _service = new DbAuditLogService(_fixture.Database);
    }

    private void ResetDatabase()
    {
        _fixture.Database.Deleteable<AuditLogEntity>().ExecuteCommand();
    }

    [Fact]
    public async Task LogOperationAsync_ValidInput_PersistsSuccessEntryWithDetails()
    {
        bool result = await _service.LogOperationAsync(
            1, "alice", AuditActionType.Create, "User", "创建用户: bob",
            entityType: "User", entityId: 5, newData: "{\"name\":\"bob\"}", clientIp: "127.0.0.1");

        result.Should().BeTrue("写入有效审计日志应返回成功");
        var logs = await _service.GetLogsByUserIdAsync(1);
        logs.Should().ContainSingle("单条写入后应恰好能查到一条");
        var log = logs[0];
        log.IsSuccess.Should().BeTrue("操作审计应标记为成功");
        log.Username.Should().Be("alice", "应持久化操作者用户名");
        log.Module.Should().Be("User", "应持久化模块名");
        log.EntityType.Should().Be("User", "应持久化实体类型");
        log.EntityId.Should().Be(5, "应持久化实体 ID");
        log.NewData.Should().Be("{\"name\":\"bob\"}", "应持久化变更后数据");
        log.ClientIp.Should().Be("127.0.0.1", "应持久化客户端 IP");
    }

    [Fact]
    public async Task LogOperationAsync_BlankUsername_FallsBackToSystem()
    {
        await _service.LogOperationAsync(1, "", AuditActionType.View, "System", "系统操作");

        var logs = await _service.GetAllLogsAsync();
        logs.Should().ContainSingle("单条写入后应恰好能查到一条");
        logs[0].Username.Should().Be("system", "空白用户名应回退为 system 以保证审计可追溯");
    }

    [Fact]
    public async Task LogFailureAsync_ValidInput_PersistsFailureWithReason()
    {
        bool result = await _service.LogFailureAsync(
            2, "bob", AuditActionType.Delete, "Role", "删除角色失败",
            failureReason: "角色正被用户引用", entityType: "Role", entityId: 3);

        result.Should().BeTrue("失败审计写入应返回成功");
        var logs = await _service.GetLogsByActionTypeAsync(AuditActionType.Delete);
        logs.Should().ContainSingle("按操作类型应能查到刚写入的失败记录");
        logs[0].IsSuccess.Should().BeFalse("失败审计不应标记为成功");
        logs[0].FailureReason.Should().Be("角色正被用户引用", "失败原因必须留痕以便追查");
    }

    [Fact]
    public async Task GetLogsByModuleAsync_MixedModules_ReturnsOnlyMatchingModule()
    {
        await _service.LogOperationAsync(1, "alice", AuditActionType.View, "User", "查看用户");
        await _service.LogOperationAsync(1, "alice", AuditActionType.View, "Role", "查看角色");

        var userLogs = await _service.GetLogsByModuleAsync("User");

        userLogs.Should().ContainSingle("模块过滤只应返回对应模块的日志");
        userLogs[0].Module.Should().Be("User", "过滤结果的模块名应与查询一致");
    }

    [Fact]
    public async Task DeleteOldLogsAsync_MixedAges_RemovesOnlyExpiredEntries()
    {
        await _service.CreateAuditLogAsync(new AuditLog
        {
            UserId = 1,
            ActionType = AuditActionType.View,
            Module = "System",
            Description = "60 天前的旧日志",
            OperationTime = DateTime.Now.AddDays(-60)
        });
        await _service.CreateAuditLogAsync(new AuditLog
        {
            UserId = 1,
            ActionType = AuditActionType.View,
            Module = "System",
            Description = "昨天的日志",
            OperationTime = DateTime.Now.AddDays(-1)
        });

        bool result = await _service.DeleteOldLogsAsync(30);

        result.Should().BeTrue("清理操作应返回成功");
        var remaining = await _service.GetAllLogsAsync();
        remaining.Should().ContainSingle("保留期内日志不应被删除");
        remaining[0].Description.Should().Be("昨天的日志", "只有过期日志被清理");
    }

    [Fact]
    public async Task ExportLogsAsync_SelectedIds_ReturnsJsonForSelectedOnly()
    {
        await _service.LogOperationAsync(1, "alice", AuditActionType.View, "User", "导出目标日志");
        await _service.LogOperationAsync(2, "bob", AuditActionType.View, "Role", "未被选中的日志");
        var allLogs = await _service.GetAllLogsAsync();
        int targetId = allLogs.First(x => x.Description == "导出目标日志").Id;
        int excludedId = allLogs.First(x => x.Description == "未被选中的日志").Id;

        byte[] bytes = await _service.ExportLogsAsync(new List<int> { targetId });

        // System.Text.Json 默认把非 ASCII 转义为 \uXXXX，断言基于反序列化结果而非原始字符串
        var exported = JsonSerializer.Deserialize<List<AuditLog>>(Encoding.UTF8.GetString(bytes));
        exported.Should().ContainSingle("只应导出选中的日志");
        exported![0].Id.Should().Be(targetId, "导出的应是选中 Id 的日志");
        exported[0].Description.Should().Be("导出目标日志", "导出内容应与选中日志一致");
    }

    [Fact]
    public async Task ClearAllLogsAsync_WithExistingLogs_RemovesEverything()
    {
        await _service.LogOperationAsync(1, "alice", AuditActionType.View, "System", "待清理");
        await _service.LogOperationAsync(2, "bob", AuditActionType.View, "System", "待清理");

        bool result = await _service.ClearAllLogsAsync();

        result.Should().BeTrue("清空操作应返回成功");
        (await _service.GetAllLogsAsync()).Should().BeEmpty("清空后不应残留任何日志");
    }
}
