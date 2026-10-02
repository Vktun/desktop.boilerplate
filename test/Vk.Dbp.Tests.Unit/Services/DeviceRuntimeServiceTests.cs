using Dabp.Infrastructure.Entities;
using FluentAssertions;
using Moq;
using Prism.Events;
using SqlSugar;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.Services.Alarm;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

/// <summary>
/// 引擎时序测试：真实组件 + 30ms 短轮询 + WaitUntilAsync 条件等待（不用固定 Sleep 猜时长）。
/// </summary>
public sealed class DeviceRuntimeServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly ISqlSugarClient _db;
    private readonly RealtimeDataStore _store = new();
    private readonly EventAggregator _eventAggregator = new();
    private readonly Mock<IAlarmService> _alarmService = new();
    private readonly Mock<ISystemConfigService> _systemConfigService = new();
    private readonly Mock<IContainerProvider> _containerProvider = new();

    public DeviceRuntimeServiceTests(TestDatabaseFixture fixture)
    {
        _db = fixture.Database;
        ResetDatabase();

        _alarmService
            .Setup(service => service.CreateAlarmAsync(It.IsAny<AlarmRecord>()))
            .ReturnsAsync(true);
        _systemConfigService
            .Setup(service => service.GetIntConfigAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string _, int defaultValue) => defaultValue);
        _systemConfigService
            .Setup(service => service.GetBoolConfigAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string _, bool defaultValue) => defaultValue);
        _containerProvider
            .Setup(provider => provider.Resolve(typeof(IAlarmService)))
            .Returns(_alarmService.Object);
        _containerProvider
            .Setup(provider => provider.Resolve(typeof(ISystemConfigService)))
            .Returns(_systemConfigService.Object);
    }

    private void ResetDatabase()
    {
        _db.Deleteable<DeviceCommand>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<DevicePoint>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<PointHistory>().Where(_ => true).ExecuteCommand();
        _db.Deleteable<Device>().Where(_ => true).ExecuteCommand();
    }

    private DeviceRuntimeService CreateEngine(IProtocolDriverFactory? driverFactory = null)
    {
        var history = new HistoryDataService(_db, TimeProvider.System, batchSize: 500, flushInterval: TimeSpan.FromSeconds(5));
        var publisher = new SnapshotEventPublisher(_eventAggregator, TimeSpan.FromMilliseconds(50), TimeProvider.System);

        return new DeviceRuntimeService(
            _db,
            driverFactory ?? new ProtocolDriverFactory(TimeProvider.System, new IotCollectorHost()),
            _store,
            history,
            publisher,
            _eventAggregator,
            _containerProvider.Object,
            TimeProvider.System);
    }

    private async Task<int> SeedDeviceAsync(string code, string protocolType = "Simulated")
    {
        var device = new Device
        {
            Code = code,
            Name = code,
            ProtocolType = protocolType,
            ConnectionConfig = "{\"pollIntervalMs\":30}",
            IsEnabled = true,
            CreatedAt = DateTime.Now
        };
        device.Id = await _db.Insertable(device).ExecuteReturnIdentityAsync();
        return device.Id;
    }

    private async Task SeedPointAsync(
        int deviceId,
        string code,
        string address,
        decimal? alarmHigh = null,
        decimal? alarmLow = null,
        PointDataType dataType = PointDataType.Double)
    {
        await _db.Insertable(new DevicePoint
        {
            DeviceId = deviceId,
            Code = code,
            Name = code,
            DataType = dataType,
            Address = address,
            AlarmHigh = alarmHigh,
            AlarmLow = alarmLow,
            IsEnabled = true,
            CreatedAt = DateTime.Now
        }).ExecuteCommandAsync();
    }

    [Fact]
    public async Task StartAsync_EnabledDevice_PopulatesRealtimeStore()
    {
        int deviceId = await SeedDeviceAsync("SIM-A");
        await SeedPointAsync(deviceId, "TEMP-A", "const(42)");

        var engine = CreateEngine();
        try
        {
            await engine.StartAsync();

            await TimingTestHelper.WaitUntilAsync(
                () => _store.GetSnapshot("TEMP-A") is not null,
                diagnostics: "引擎启动后实时仓应出现点位快照");

            var snapshot = _store.GetSnapshot("TEMP-A");
            snapshot!.Value.Should().Be(42, "const 波形应返回设定值");
            snapshot.Quality.Should().Be(DataQuality.Good, "模拟驱动采集应为 Good 质量");
            engine.GetDeviceStatus(deviceId).Should().Be(DeviceRuntimeStatus.Running, "正常采集的设备应处于运行状态");
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task StartAsync_ThresholdCrossing_CreatesSingleAlarmAndPublishesEvent()
    {
        int deviceId = await SeedDeviceAsync("SIM-B");
        await SeedPointAsync(deviceId, "TEMP-B", "const(50)", alarmHigh: 40m);

        var triggeredPayloads = new List<AlarmTriggeredPayload>();
        _eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(triggeredPayloads.Add);

        var engine = CreateEngine();
        try
        {
            await engine.StartAsync();

            await TimingTestHelper.WaitUntilAsync(
                () => _store.GetTrendWindow("TEMP-B", 100).Count >= 3,
                diagnostics: "等待若干轮询周期让边沿检测与告警联动执行");

            _alarmService.Verify(
                service => service.CreateAlarmAsync(It.Is<AlarmRecord>(record =>
                    record.AlarmCode.StartsWith("SIM-B.TEMP-B.HIGH.", StringComparison.Ordinal))),
                Times.Once,
                "持续超限只应在上沿触发一次告警");

            triggeredPayloads.Should().ContainSingle(
                payload => payload.AlarmCode.StartsWith("SIM-B.TEMP-B.HIGH.", StringComparison.Ordinal),
                "告警创建成功后引擎应发布一次 AlarmTriggeredEvent");

            var payload = triggeredPayloads.Single(payload => payload.AlarmCode.Contains("TEMP-B"));
            payload.Level.Should().Be(AlarmLevel.Critical, "高限告警应为 Critical 等级");
            payload.UserId.Should().Be(0, "引擎告警应为全局告警（UserId=0）");
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task StartAsync_FailingDevice_MarksFaultedWhileOthersKeepRunning()
    {
        int okDeviceId = await SeedDeviceAsync("OK-DEV");
        await SeedPointAsync(okDeviceId, "TEMP-OK", "const(11)");
        int faultDeviceId = await SeedDeviceAsync("FAULT-DEV");
        await SeedPointAsync(faultDeviceId, "TEMP-F", "const(1)");

        var factory = new DelegateDriverFactory(info => info.Code == "FAULT-DEV"
            ? new ThrowingDriver()
            : new SimulatedDriver(TimeProvider.System, info.Code));

        var engine = CreateEngine(factory);
        try
        {
            await engine.StartAsync();

            await TimingTestHelper.WaitUntilAsync(
                () => _store.GetSnapshot("TEMP-OK") is not null,
                diagnostics: "正常设备应持续采集");
            await TimingTestHelper.WaitUntilAsync(
                () => engine.GetDeviceStatus(faultDeviceId) == DeviceRuntimeStatus.Faulted,
                diagnostics: "故障设备应进入 Faulted 状态");

            engine.GetDeviceStatus(okDeviceId).Should().Be(DeviceRuntimeStatus.Running, "故障设备不应拖垮其他设备");

            await TimingTestHelper.WaitUntilAsync(
                () => _store.GetTrendWindow("TEMP-OK", 100).Count >= 2,
                diagnostics: "正常设备应持续多周期更新");

            _alarmService.Verify(
                service => service.CreateAlarmAsync(It.Is<AlarmRecord>(record =>
                    record.AlarmCode.StartsWith("FAULT-DEV.FAULT.", StringComparison.Ordinal))),
                Times.Once,
                "一次连续故障期只应创建一条设备故障告警");
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task StopAsync_ClearsStoreAndFlushesHistory()
    {
        int deviceId = await SeedDeviceAsync("SIM-C");
        await SeedPointAsync(deviceId, "TEMP-C", "const(7)");

        var engine = CreateEngine();
        await engine.StartAsync();

        await TimingTestHelper.WaitUntilAsync(
            () => _store.GetTrendWindow("TEMP-C", 100).Count >= 2,
            diagnostics: "等待多周期样本进入历史缓冲");

        await engine.StopAsync();

        _store.GetSnapshot("TEMP-C").Should().BeNull("停止后实时仓应清空");
        engine.GetDeviceStatus(deviceId).Should().Be(DeviceRuntimeStatus.Stopped, "停止后设备应为 Stopped 状态");

        var historyRows = await _db.Queryable<PointHistory>().CountAsync();
        historyRows.Should().BeGreaterThanOrEqualTo(2, "停止时应终刷历史缓冲到数据库");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WritesThroughDriverAndUpdatesStore()
    {
        int deviceId = await SeedDeviceAsync("SIM-D");
        await SeedPointAsync(deviceId, "VALVE-D", "pulse(45)", dataType: PointDataType.Boolean);
        var command = new DeviceCommand
        {
            DeviceId = deviceId,
            Code = "CMD-VALVE-ON",
            Name = "打开阀门",
            TargetPointCode = "VALVE-D",
            WriteValue = "1",
            IsEnabled = true,
            CreatedAt = DateTime.Now
        };
        command.Id = await _db.Insertable(command).ExecuteReturnIdentityAsync();

        var engine = CreateEngine();
        try
        {
            await engine.StartAsync();

            await TimingTestHelper.WaitUntilAsync(
                () => _store.GetSnapshot("VALVE-D") is not null,
                diagnostics: "等待引擎进入运行状态");

            var result = await engine.ExecuteCommandAsync(command.Id);

            result.Success.Should().BeTrue("有效命令应执行成功：{0}", result.Message);
            _store.GetSnapshot("VALVE-D")!.Value.Should().Be(1, "命令回写后实时仓应反映覆盖值");
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task ExecuteCommandAsync_UnknownCommand_ReturnsFailure()
    {
        var engine = CreateEngine();

        var result = await engine.ExecuteCommandAsync(99999);

        result.Success.Should().BeFalse("不存在的命令应返回失败");
    }

    [Fact]
    public async Task StartAsync_NoDevices_CompletesWithoutThrowing()
    {
        var engine = CreateEngine();

        var act = () => engine.StartAsync();
        await act.Should().NotThrowAsync("空设备库是合法情况，引擎空转");

        engine.GetAllDeviceStatuses().Should().BeEmpty("无设备时状态表应为空");
        await engine.StopAsync();
    }

    private sealed class DelegateDriverFactory(Func<DeviceConnectionInfo, IProtocolDriver> create) : IProtocolDriverFactory
    {
        public bool CanCreate(string protocolType)
        {
            return true;
        }

        public IProtocolDriver Create(DeviceConnectionInfo device)
        {
            return create(device);
        }
    }

    private sealed class ThrowingDriver : IProtocolDriver
    {
        public string ProtocolType => "Throwing";

        public bool IsConnected => true;

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PointValueSnapshot>> ReadPointsAsync(
            IReadOnlyList<PointReadRequest> requests,
            CancellationToken cancellationToken)
        {
            throw new IOException("模拟通讯故障");
        }

        public Task<PointValueSnapshot> WritePointAsync(PointWriteRequest request, CancellationToken cancellationToken)
        {
            throw new IOException("模拟写失败");
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
