using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 设备目录读取契约（设备/点位/命令定义的数据源；监控页 VM 只依赖契约，不触碰实体）
/// </summary>
public interface IDeviceCatalogService
{
    /// <summary>
    /// 查询设备清单（默认仅启用设备）
    /// </summary>
    Task<List<DeviceSummary>> GetDevicesAsync(bool includeDisabled = false);

    /// <summary>
    /// 查询设备下的全部点位定义
    /// </summary>
    Task<List<PointDefinition>> GetPointsAsync(int deviceId);

    /// <summary>
    /// 查询设备下的全部命令定义
    /// </summary>
    Task<List<CommandDefinition>> GetCommandsAsync(int deviceId);
}
