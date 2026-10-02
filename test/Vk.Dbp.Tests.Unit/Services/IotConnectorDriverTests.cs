using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vktun.IoT.Connector;
using Vktun.IoT.Connector.Core.Enums;
using Vktun.IoT.Connector.Core.Interfaces;
using Vktun.IoT.Connector.Core.Models;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

/// <summary>
/// vktun.iot.connector 驱动的端到端验证：进程内启动 SDK 自带的 Modbus TCP 从站模拟器，
/// 驱动以客户端方式连接并读写——无需任何硬件即可覆盖真实协议链路（建连/模板/采集/回写）。
/// </summary>
public sealed class IotConnectorDriverTests : IAsyncLifetime
{
    private ServiceProvider? _sdkProvider;
    private IModbusSlaveServer? _slaveServer;
    private IotCollectorHost? _collectorHost;
    private int _port;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddVktunIoTConnector();
        _sdkProvider = services.BuildServiceProvider();
        _slaveServer = _sdkProvider.GetRequiredService<IModbusSlaveServer>();
        _collectorHost = new IotCollectorHost();
        _port = GetFreeTcpPort();

        var dataStore = new ModbusSlaveDataStore(coilCount: 100, discreteInputCount: 100, inputRegisterCount: 100, holdingRegisterCount: 100);
        dataStore.SetHoldingRegister(0, 1234);
        dataStore.SetHoldingRegister(1, 567);
        dataStore.SetCoil(0, true);

        var startResult = await _slaveServer.StartAsync(
            new ModbusSlaveOptions { ListenAddress = "127.0.0.1", Port = _port, SlaveId = 1 },
            dataStore);
        startResult.Success.Should().BeTrue("从站模拟器应能在本地端口启动：{0}", startResult.ErrorMessage);
    }

    public async Task DisposeAsync()
    {
        if (_slaveServer is not null)
        {
            await _slaveServer.StopAsync();
            await _slaveServer.DisposeAsync();
        }

        if (_sdkProvider is not null)
        {
            await _sdkProvider.DisposeAsync();
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private DeviceConnectionInfo CreateDeviceInfo(params PointDefinition[] points)
    {
        return new DeviceConnectionInfo
        {
            DeviceId = 1,
            Code = "MOD-TEST-1",
            ProtocolType = ProtocolTypes.ModbusTcp,
            ConnectionConfig = $"{{\"ip\":\"127.0.0.1\",\"port\":{_port},\"slaveId\":1}}",
            Points = points
        };
    }

    private static PointDefinition Point(string code, string address)
    {
        return new PointDefinition
        {
            Id = 1,
            DeviceId = 1,
            Code = code,
            Name = code,
            DataType = PointDataType.Int32,
            Address = address,
            IsEnabled = true
        };
    }

    private static PointReadRequest ReadRequest(string code, string address)
    {
        return new PointReadRequest { PointId = 1, PointCode = code, DataType = PointDataType.Int32, Address = address };
    }

    [Fact]
    public async Task ReadPointsAsync_ModbusTcpHoldingRegister_ReturnsSeededValue()
    {
        await using var driver = new IotConnectorDriver(_collectorHost!, CreateDeviceInfo(Point("REG-01", "HR:0:UInt16")));
        await driver.ConnectAsync(CancellationToken.None);

        var snapshots = await driver.ReadPointsAsync(new[] { ReadRequest("REG-01", "HR:0:UInt16") }, CancellationToken.None);

        driver.IsConnected.Should().BeTrue("连接成功后驱动应处于已连接状态");
        snapshots.Should().HaveCount(1, "每次读点请求应返回等量快照");
        snapshots[0].Quality.Should().Be(DataQuality.Good, "从站正常响应应为 Good 质量：{0}", snapshots[0].ValueText);
        snapshots[0].Value.Should().Be(1234, "保持寄存器 0 应读到种子值");
        snapshots[0].DeviceCode.Should().Be("MOD-TEST-1", "快照应携带设备编码");
    }

    [Fact]
    public async Task ReadPointsAsync_MultipleRegisters_ReturnsAllValues()
    {
        await using var driver = new IotConnectorDriver(
            _collectorHost!,
            CreateDeviceInfo(Point("REG-01", "HR:0:UInt16"), Point("REG-02", "HR:1:UInt16")));
        await driver.ConnectAsync(CancellationToken.None);

        var snapshots = await driver.ReadPointsAsync(
            new[] { ReadRequest("REG-01", "HR:0:UInt16"), ReadRequest("REG-02", "HR:1:UInt16") },
            CancellationToken.None);

        snapshots.Should().OnlyContain(snapshot => snapshot.Quality == DataQuality.Good, "两个寄存器都应读到 Good 数据");
        snapshots.Should().Contain(snapshot => snapshot.PointCode == "REG-01" && snapshot.Value == 1234, "寄存器 0 的值应匹配种子");
        snapshots.Should().Contain(snapshot => snapshot.PointCode == "REG-02" && snapshot.Value == 567, "寄存器 1 的值应匹配种子");
    }

    [Fact]
    public async Task WritePointAsync_HoldingRegister_PersistsToSlave()
    {
        await using var driver = new IotConnectorDriver(_collectorHost!, CreateDeviceInfo(Point("REG-09", "HR:9:UInt16")));
        await driver.ConnectAsync(CancellationToken.None);

        var snapshot = await driver.WritePointAsync(
            new PointWriteRequest { PointCode = "REG-09", Address = "HR:9:UInt16", DataType = PointDataType.Int32, Value = "7788" },
            CancellationToken.None);

        snapshot.Quality.Should().Be(DataQuality.Good, "写入成功应返回 Good：{0}", snapshot.ValueText);
        _slaveServer!.DataStore.GetHoldingRegister(9).Should().Be((ushort)7788, "写入值应到达从站寄存器");
    }

    [Fact]
    public async Task WritePointAsync_Coil_TogglesSlaveCoil()
    {
        await using var driver = new IotConnectorDriver(_collectorHost!, CreateDeviceInfo(Point("COIL-01", "C:1")));
        await driver.ConnectAsync(CancellationToken.None);

        var snapshot = await driver.WritePointAsync(
            new PointWriteRequest { PointCode = "COIL-01", Address = "C:1", DataType = PointDataType.Boolean, Value = "1" },
            CancellationToken.None);

        snapshot.Quality.Should().Be(DataQuality.Good, "线圈写入应成功：{0}", snapshot.ValueText);
        _slaveServer!.DataStore.GetCoil(1).Should().BeTrue("线圈 1 应被置位");
    }

    [Fact]
    public async Task ReadPointsAsync_InvalidAddressInTemplate_ReturnsBadOrUncertainQuality()
    {
        await using var driver = new IotConnectorDriver(
            _collectorHost!,
            CreateDeviceInfo(Point("BAD-01", "HR:0:UInt16"), Point("BAD-02", "not-a-modbus-address")));
        await driver.ConnectAsync(CancellationToken.None);

        var snapshots = await driver.ReadPointsAsync(
            new[] { ReadRequest("BAD-01", "HR:0:UInt16"), ReadRequest("BAD-02", "not-a-modbus-address") },
            CancellationToken.None);

        snapshots.Should().Contain(snapshot => snapshot.PointCode == "BAD-02" && snapshot.Quality != DataQuality.Good,
            "地址非法的点位不应返回 Good 质量");
    }
}
