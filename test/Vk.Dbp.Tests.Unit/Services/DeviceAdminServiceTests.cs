using Dabp.Infrastructure.Entities;
using FluentAssertions;
using Moq;
using SqlSugar;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;
using static Vk.Dbp.Services.Audit.AuditActionType;

namespace Vk.Dbp.Tests.Unit.Services;

public sealed class DeviceAdminServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUserSession> _userSession = new();
    private readonly DeviceAdminService _service;

    public DeviceAdminServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();
        SetupAuditLogService();
        SetupUserSession();

        _service = new DeviceAdminService(_db, _auditLogService.Object, _userSession.Object);
    }

    private void ResetDatabase()
    {
        _db.Deleteable<DeviceCommand>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<DevicePoint>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<PointHistory>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Device>().Where(_ => true).ExecuteCommand();
    }

    private void SetupAuditLogService()
    {
        _auditLogService
            .Setup(service => service.LogOperationAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        _auditLogService
            .Setup(service => service.LogFailureAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private void SetupUserSession()
    {
        _userSession.SetupGet(session => session.IsLoggedIn).Returns(true);
        _userSession.SetupGet(session => session.UserId).Returns(1);
        _userSession.SetupGet(session => session.Username).Returns("admin");
    }

    private async Task<int> SeedDeviceAsync(string code, bool isEnabled = true)
    {
        var device = new Device
        {
            Code = code,
            Name = code,
            ProtocolType = ProtocolTypes.Simulated,
            IsEnabled = isEnabled,
            CreatedAt = DateTime.Now
        };
        device.Id = await _db.Insertable(device).ExecuteReturnIdentityAsync();
        return device.Id;
    }

    private async Task<int> SeedPointAsync(int deviceId, string code, string address = "const(42)")
    {
        var point = new DevicePoint
        {
            DeviceId = deviceId,
            Code = code,
            Name = code,
            DataType = PointDataType.Double,
            Address = address,
            IsEnabled = true,
            CreatedAt = DateTime.Now
        };
        point.Id = await _db.Insertable(point).ExecuteReturnIdentityAsync();
        return point.Id;
    }

    private async Task<int> SeedCommandAsync(int deviceId, string code, string targetPointCode)
    {
        var command = new DeviceCommand
        {
            DeviceId = deviceId,
            Code = code,
            Name = code,
            TargetPointCode = targetPointCode,
            WriteValue = "1",
            IsEnabled = true,
            CreatedAt = DateTime.Now
        };
        command.Id = await _db.Insertable(command).ExecuteReturnIdentityAsync();
        return command.Id;
    }

    [Fact]
    public async Task SaveDeviceAsync_NewDevice_InsertsAndReturnsSuccess()
    {
        var result = await _service.SaveDeviceAsync(new DeviceEditModel
        {
            Code = "SIM-NEW-01",
            Name = "新建设备",
            ProtocolType = ProtocolTypes.Simulated,
            ConnectionConfig = "{\"pollIntervalMs\":1000}"
        });

        result.Success.Should().BeTrue("有效设备应保存成功：{0}", result.Message);
        (await _db.Queryable<Device>().AnyAsync(device => device.Code == "SIM-NEW-01"))
            .Should().BeTrue("保存后应能查询到设备");
    }

    [Fact]
    public async Task SaveDeviceAsync_DuplicateCode_ReturnsFailureWithoutInsert()
    {
        await SeedDeviceAsync("SIM-DUP");

        var result = await _service.SaveDeviceAsync(new DeviceEditModel
        {
            Code = "SIM-DUP",
            Name = "重复设备"
        });

        result.Success.Should().BeFalse("重复编码应被拒绝");
        result.Message.Should().Contain("已存在", "失败消息应说明编码冲突");
        (await _db.Queryable<Device>().CountAsync()).Should().Be(1, "拒绝后不应新增行");
    }

    [Fact]
    public async Task SaveDeviceAsync_Update_PersistsChangesAndSetsUpdatedAt()
    {
        int deviceId = await SeedDeviceAsync("SIM-UPD");

        var result = await _service.SaveDeviceAsync(new DeviceEditModel
        {
            Id = deviceId,
            Code = "SIM-UPD",
            Name = "改名后的设备",
            ProtocolType = ProtocolTypes.Simulated,
            IsEnabled = true
        });

        result.Success.Should().BeTrue("更新应成功：{0}", result.Message);
        var device = await _db.Queryable<Device>().FirstAsync(entity => entity.Id == deviceId);
        device.Name.Should().Be("改名后的设备", "更新后名称应持久化");
        device.UpdatedAt.Should().NotBeNull("更新应写入 UpdatedAt");
    }

    [Fact]
    public async Task SaveDeviceAsync_InvalidConnectionJson_ReturnsFailure()
    {
        var result = await _service.SaveDeviceAsync(new DeviceEditModel
        {
            Code = "SIM-BADJSON",
            Name = "坏配置",
            ConnectionConfig = "{\"pollIntervalMs\":"
        });

        result.Success.Should().BeFalse("非法 JSON 应被拒绝");
        result.Message.Should().Contain("JSON", "失败消息应指出 JSON 问题");
        (await _db.Queryable<Device>().AnyAsync()).Should().BeFalse("拒绝后不应落库");
    }

    [Fact]
    public async Task SavePointAsync_NewPoint_Inserts()
    {
        int deviceId = await SeedDeviceAsync("SIM-PT");

        var result = await _service.SavePointAsync(new PointEditModel
        {
            DeviceId = deviceId,
            Code = "TEMP-NEW",
            Name = "温度",
            Address = "sine(20,80,60)",
            AlarmHigh = 75m,
            AlarmLow = 25m
        });

        result.Success.Should().BeTrue("有效点位应保存成功：{0}", result.Message);
        var point = await _db.Queryable<DevicePoint>().FirstAsync(entity => entity.Code == "TEMP-NEW");
        point.AlarmHigh.Should().Be(75m, "阈值应持久化");
    }

    [Fact]
    public async Task SavePointAsync_DuplicateCodeAcrossDevices_ReturnsFailure()
    {
        int deviceA = await SeedDeviceAsync("SIM-A");
        int deviceB = await SeedDeviceAsync("SIM-B");
        await SeedPointAsync(deviceA, "TEMP-X");

        var result = await _service.SavePointAsync(new PointEditModel
        {
            DeviceId = deviceB,
            Code = "TEMP-X",
            Name = "设备B的同码点",
            Address = "const(1)"
        });

        result.Success.Should().BeFalse("点位编码全局唯一，跨设备同码应被拒绝");
        result.Message.Should().Contain("全局唯一", "失败消息应说明全局唯一语义");
    }

    [Fact]
    public async Task DeleteDeviceAsync_CascadesPointsAndCommands_KeepsHistory()
    {
        int deviceId = await SeedDeviceAsync("SIM-DEL");
        await SeedPointAsync(deviceId, "TEMP-DEL");
        await SeedPointAsync(deviceId, "LEVEL-DEL");
        await SeedCommandAsync(deviceId, "CMD-DEL", "TEMP-DEL");
        await SeedCommandAsync(deviceId, "CMD-DEL-2", "LEVEL-DEL");
        await _db.Insertable(new PointHistory
        {
            PointId = 1,
            PointCode = "TEMP-DEL",
            Value = 42,
            Quality = DataQuality.Good,
            Timestamp = DateTime.Now
        }).ExecuteCommandAsync();

        var result = await _service.DeleteDeviceAsync(deviceId);

        result.Success.Should().BeTrue("删除应成功：{0}", result.Message);
        (await _db.Queryable<DevicePoint>().CountAsync(point => point.DeviceId == deviceId))
            .Should().Be(0, "设备点位应级联删除");
        (await _db.Queryable<DeviceCommand>().CountAsync(command => command.DeviceId == deviceId))
            .Should().Be(0, "设备命令应级联删除");
        (await _db.Queryable<Device>().CountAsync(device => device.Id == deviceId))
            .Should().Be(0, "设备本身应被删除");
        (await _db.Queryable<PointHistory>().CountAsync(history => history.PointCode == "TEMP-DEL"))
            .Should().Be(1, "历史数据应保留（PointCode 冗余列仍可检索）");
    }

    [Fact]
    public async Task DeletePointAsync_DeletesReferencingCommands()
    {
        int deviceId = await SeedDeviceAsync("SIM-DPT");
        await SeedPointAsync(deviceId, "VALVE-X");
        int commandId = await SeedCommandAsync(deviceId, "CMD-VALVE-X", "VALVE-X");

        var result = await _service.DeletePointAsync(
            (await _db.Queryable<DevicePoint>().FirstAsync(point => point.Code == "VALVE-X")).Id);

        result.Success.Should().BeTrue("删除点位应成功：{0}", result.Message);
        (await _db.Queryable<DeviceCommand>().AnyAsync(command => command.Id == commandId))
            .Should().BeFalse("引用该点位的命令应级联删除");
    }

    [Fact]
    public async Task SaveCommandAsync_TargetPointNotInDevice_ReturnsFailure()
    {
        int deviceId = await SeedDeviceAsync("SIM-CMD");
        int otherDeviceId = await SeedDeviceAsync("SIM-CMD-OTHER");
        await SeedPointAsync(otherDeviceId, "FOREIGN-PT");

        var result = await _service.SaveCommandAsync(new CommandEditModel
        {
            DeviceId = deviceId,
            Code = "CMD-BAD",
            Name = "跨设备目标",
            TargetPointCode = "FOREIGN-PT",
            WriteValue = "1"
        });

        result.Success.Should().BeFalse("目标点位不属于该设备应被拒绝");
        result.Message.Should().Contain("不属于", "失败消息应说明归属校验");
    }

    [Fact]
    public async Task SaveCommandAsync_DuplicateCodeInSameDevice_ReturnsFailureButCrossDeviceAllowed()
    {
        int deviceA = await SeedDeviceAsync("SIM-CA");
        int deviceB = await SeedDeviceAsync("SIM-CB");
        await SeedPointAsync(deviceA, "PT-CA");
        await SeedPointAsync(deviceB, "PT-CB");
        await SeedCommandAsync(deviceA, "CMD-SAME", "PT-CA");

        var sameDevice = await _service.SaveCommandAsync(new CommandEditModel
        {
            DeviceId = deviceA,
            Code = "CMD-SAME",
            Name = "设备内重复",
            TargetPointCode = "PT-CA",
            WriteValue = "1"
        });
        var crossDevice = await _service.SaveCommandAsync(new CommandEditModel
        {
            DeviceId = deviceB,
            Code = "CMD-SAME",
            Name = "跨设备同码",
            TargetPointCode = "PT-CB",
            WriteValue = "1"
        });

        sameDevice.Success.Should().BeFalse("同设备内命令编码重复应被拒绝");
        crossDevice.Success.Should().BeTrue("不同设备允许相同命令编码：{0}", crossDevice.Message);
    }

    [Fact]
    public async Task SaveDeviceAsync_Success_LogsCreateAudit()
    {
        await _service.SaveDeviceAsync(new DeviceEditModel
        {
            Code = "SIM-AUDIT",
            Name = "审计验证"
        });

        _auditLogService.Verify(
            service => service.LogOperationAsync(
                It.IsAny<int>(), It.IsAny<string>(), Create, "DeviceManagement",
                It.IsAny<string>(),
                It.Is<string?>(entityType => entityType == "Device"), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Once,
            "设备创建成功应记录一次 Create 审计");
    }

    [Fact]
    public async Task DeleteCommandAsync_RepositoryFailure_LogsFailureAudit()
    {
        // 掉表制造仓储异常（SqlSugar/DbException 在过滤器覆盖内）。
        // 选 DeleteCommandAsync：其首个 DB 访问就在 try 块内，掉表必然走"失败审计"路径而非裸抛。
        try
        {
            _db.DbMaintenance.DropTable("DeviceCommands");

            var result = await _service.DeleteCommandAsync(123);

            result.Success.Should().BeFalse("仓储异常应返回失败");
            _auditLogService.Verify(
                service => service.LogFailureAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(), "DeviceManagement",
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.Is<string?>(entityType => entityType == "DeviceCommand"), It.IsAny<int?>(), It.IsAny<string?>()),
                Times.Once,
                "仓储异常应记录一次失败审计");
        }
        finally
        {
            // 无论断言成败都重建表，避免污染同夹具的其他测试
            _db.CodeFirst.InitTables(typeof(DeviceCommand));
        }
    }
}
