using SqlSugar;
using System;
using Vk.Dbp.Contracts.Industrial;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 点位历史表（工业运行时内核）。高频追加型大表：引擎批量写入，按点位+时间范围查询。
    /// 刻意不设 IsEnabled/CreatedAt/UpdatedAt 审计列——本表是采集日志数据而非业务实体，
    /// 生命周期由保留策略（超期清理）管理，与 AlarmRecord 等业务实体范本的差异是设计取舍。
    /// </summary>
    [SugarTable("PointHistories")]
    [SugarIndex("IX_PointHistories_PointId_Timestamp", nameof(PointId), OrderByType.Asc, nameof(Timestamp), OrderByType.Asc)]
    public class PointHistory
    {
        /// <summary>
        /// 历史记录ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 点位ID（冗余 PointCode 便于点位删除后历史仍可按编码检索）
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 点位编码（冗余列，查询主入口）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string PointCode { get; set; } = string.Empty;

        /// <summary>
        /// 数值采样值（非数值点位为 null）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public double? Value { get; set; }

        /// <summary>
        /// 文本采样值（Boolean 显示"开/关"、String 点位原文、Bad 质量的错误描述）
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string? ValueText { get; set; }

        /// <summary>
        /// 数据质量（0=Good, 1=Uncertain, 2=Bad；仅 Good 质量样本落历史库）
        /// </summary>
        public DataQuality Quality { get; set; } = DataQuality.Good;

        /// <summary>
        /// 采样时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
