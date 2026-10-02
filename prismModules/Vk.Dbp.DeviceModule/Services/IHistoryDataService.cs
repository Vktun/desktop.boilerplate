using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 点位历史数据契约：批量缓冲 → 定时/定量落盘 → 时间范围查询（降采样）→ 保留策略清理。
/// </summary>
public interface IHistoryDataService
{
    /// <summary>
    /// 批量入缓冲（仅 Good 质量样本会被落库；缓冲超硬顶时丢最旧并记日志）
    /// </summary>
    void Enqueue(IReadOnlyList<PointValueSnapshot> samples);

    /// <summary>
    /// 立即刷盘（显式调用：引擎停止终刷、测试用）；返回写入行数
    /// </summary>
    Task<int> FlushAsync();

    /// <summary>
    /// 到期才刷盘（维护循环周期调用）：定量（≥批量阈值）或定时（≥间隔，连续失败时按退避拉长）触发；返回写入行数
    /// </summary>
    Task<int> FlushIfDueAsync();

    /// <summary>
    /// 按点位编码与时间范围查询（升序）；结果超过 maxPoints 时等距降采样并保留首尾
    /// </summary>
    Task<List<HistoryPoint>> QueryAsync(string pointCode, DateTime startTime, DateTime endTime, int maxPoints);

    /// <summary>
    /// 服务端聚合指定时间窗内的 min/max/avg/count（班报统计用；窗口无样本返回 null，统计跳过非数值样本）
    /// </summary>
    Task<PointAggregate?> GetAggregateAsync(string pointCode, DateTime startTime, DateTime endTime);

    /// <summary>
    /// 清理超过保留天数的历史数据；返回删除行数
    /// </summary>
    Task<int> PurgeExpiredAsync(int retentionDays);
}
