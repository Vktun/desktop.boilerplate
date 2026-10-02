using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 协议驱动契约。一台设备一个驱动实例，由 <see cref="IProtocolDriverFactory"/> 创建。
/// 形态按 Modbus/OPC UA 等真实协议预留：连接 → 批量读点 → 单点回写。
/// </summary>
public interface IProtocolDriver : IAsyncDisposable
{
    /// <summary>
    /// 协议类型标识（见 Vk.Dbp.Contracts.Industrial.ProtocolTypes）
    /// </summary>
    string ProtocolType { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 建立连接（模拟驱动瞬时完成；真实协议按连接参数拨号）
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 批量读点：每个请求返回一个快照（含质量码，读失败不抛异常而是返回 Bad 质量）
    /// </summary>
    Task<IReadOnlyList<PointValueSnapshot>> ReadPointsAsync(
        IReadOnlyList<PointReadRequest> requests,
        CancellationToken cancellationToken);

    /// <summary>
    /// 单点回写（命令回写），返回写入后的快照
    /// </summary>
    Task<PointValueSnapshot> WritePointAsync(PointWriteRequest request, CancellationToken cancellationToken);
}
