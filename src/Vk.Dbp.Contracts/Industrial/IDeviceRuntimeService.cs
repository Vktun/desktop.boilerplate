namespace Vk.Dbp.Contracts.Industrial;

/// <summary>
/// 命令执行结果（命令回写的统一返回）
/// </summary>
public sealed record CommandResult(bool Success, string Message);

/// <summary>
/// 设备运行时引擎契约。
/// 这是 shell 启动接线消费的最小公共面（AppStartupService 启动引擎）；
/// 引擎内部协作契约（驱动/实时仓/历史/目录）位于 Vk.Dbp.DeviceModule.Services，
/// 待其他模块需要消费时再按惯例提升到本层。
/// </summary>
public interface IDeviceRuntimeService
{
    /// <summary>
    /// 启动引擎：加载启用设备并启动轮询（幂等，已运行直接返回）
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止引擎：取消全部轮询、终刷历史缓冲并释放驱动（幂等）
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 重启单台设备的轮询（应用新的设备配置时使用）
    /// </summary>
    Task RestartDeviceAsync(int deviceId);

    /// <summary>
    /// 查询单台设备运行状态；设备不存在或未加载时返回 Stopped
    /// </summary>
    DeviceRuntimeStatus GetDeviceStatus(int deviceId);

    /// <summary>
    /// 查询全部已加载设备的运行状态
    /// </summary>
    IReadOnlyDictionary<int, DeviceRuntimeStatus> GetAllDeviceStatuses();

    /// <summary>
    /// 按预定义命令 ID 执行点位回写
    /// </summary>
    Task<CommandResult> ExecuteCommandAsync(int commandId);
}
