using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 协议驱动工厂默认实现：内置模拟驱动 + 基于 vktun.iot.connector 的 Modbus TCP/RTU 真实驱动；
/// OPC UA 等后续协议在工厂分支注册（新协议 = 实现一个 IProtocolDriver + 一个分支）。
/// </summary>
public sealed class ProtocolDriverFactory : IProtocolDriverFactory
{
    private readonly TimeProvider _timeProvider;
    private readonly IotCollectorHost _collectorHost;

    /// <summary>
    /// 构造驱动工厂
    /// </summary>
    public ProtocolDriverFactory(TimeProvider timeProvider, IotCollectorHost collectorHost)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _collectorHost = collectorHost ?? throw new ArgumentNullException(nameof(collectorHost));
    }

    /// <inheritdoc />
    public bool CanCreate(string protocolType)
    {
        return IsSimulated(protocolType) || IsModbus(protocolType);
    }

    /// <inheritdoc />
    public IProtocolDriver Create(DeviceConnectionInfo device)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        if (IsSimulated(device.ProtocolType))
        {
            return new SimulatedDriver(_timeProvider, device.Code);
        }

        if (IsModbus(device.ProtocolType))
        {
            return new IotConnectorDriver(_collectorHost, device);
        }

        throw new NotSupportedException($"暂不支持的协议类型: {device.ProtocolType}");
    }

    private static bool IsSimulated(string protocolType)
    {
        return string.Equals(protocolType, ProtocolTypes.Simulated, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsModbus(string protocolType)
    {
        return string.Equals(protocolType, ProtocolTypes.ModbusTcp, StringComparison.OrdinalIgnoreCase)
               || string.Equals(protocolType, ProtocolTypes.ModbusRtu, StringComparison.OrdinalIgnoreCase);
    }
}
