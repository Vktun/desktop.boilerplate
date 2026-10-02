using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 协议驱动工厂默认实现。当前内置模拟驱动；
/// Modbus TCP/RTU、OPC UA 等真实协议在阶段二于工厂分支注册。
/// </summary>
public sealed class ProtocolDriverFactory : IProtocolDriverFactory
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 构造驱动工厂
    /// </summary>
    public ProtocolDriverFactory(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public bool CanCreate(string protocolType)
    {
        return string.Equals(protocolType, ProtocolTypes.Simulated, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IProtocolDriver Create(DeviceConnectionInfo device)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        if (string.Equals(device.ProtocolType, ProtocolTypes.Simulated, StringComparison.OrdinalIgnoreCase))
        {
            return new SimulatedDriver(_timeProvider, device.Code);
        }

        throw new NotSupportedException($"暂不支持的协议类型: {device.ProtocolType}");
    }
}
