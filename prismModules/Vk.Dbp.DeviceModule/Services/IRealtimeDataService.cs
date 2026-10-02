using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 实时数据只读契约（消费方：监控页 VM、未来的看板/联动模块）。
/// 写侧（RegisterPoints/Update/Clear）是引擎内部协作面，暴露在 RealtimeDataStore 具体类上，不进契约。
/// </summary>
public interface IRealtimeDataService
{
    /// <summary>
    /// 取点位当前快照；未注册或尚无数据返回 null
    /// </summary>
    PointValueSnapshot? GetSnapshot(string pointCode);

    /// <summary>
    /// 取某设备全部点位的当前快照
    /// </summary>
    IReadOnlyList<PointValueSnapshot> GetDeviceSnapshots(int deviceId);

    /// <summary>
    /// 取全部点位当前快照
    /// </summary>
    IReadOnlyList<PointValueSnapshot> GetAllSnapshots();

    /// <summary>
    /// 取点位最近 maxCount 条滚动窗口（趋势图数据源，时间升序）
    /// </summary>
    IReadOnlyList<PointValueSnapshot> GetTrendWindow(string pointCode, int maxCount);
}
