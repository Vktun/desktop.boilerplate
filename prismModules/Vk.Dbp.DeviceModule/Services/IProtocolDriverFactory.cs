using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 协议驱动工厂：按设备声明的协议类型创建驱动实例（每设备一个实例）。
/// 新协议接入 = 实现一个 IProtocolDriver 并在工厂注册分支。
/// </summary>
public interface IProtocolDriverFactory
{
    /// <summary>
    /// 是否支持指定协议类型
    /// </summary>
    bool CanCreate(string protocolType);

    /// <summary>
    /// 为设备创建驱动实例（不代为建连；连接由引擎生命周期管理）
    /// </summary>
    IProtocolDriver Create(DeviceConnectionInfo device);
}
