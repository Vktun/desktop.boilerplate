using SqlSugar;
using System;
using Vk.Dbp.Contracts.Industrial;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 设备点位表（工业运行时内核）。点位 Code 全局唯一，是实时快照与历史查询的主键语义。
    /// </summary>
    [SugarTable("DevicePoints")]
    [SugarIndex("UX_DevicePoints_Code", nameof(Code), OrderByType.Asc, true)]
    [SugarIndex("IX_DevicePoints_DeviceId", nameof(DeviceId), OrderByType.Asc)]
    public class DevicePoint
    {
        /// <summary>
        /// 点位ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 所属设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位编码（全局唯一，如 SIM-DEMO-01.TEMP-01 的短码 TEMP-01）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 点位名称（界面显示用）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型（0=Boolean, 1=Int32, 2=Float, 3=Double, 4=String）
        /// </summary>
        public PointDataType DataType { get; set; } = PointDataType.Double;

        /// <summary>
        /// 工程单位（如 ℃、MPa、rpm）
        /// </summary>
        [SugarColumn(Length = 20, IsNullable = true)]
        public string? Unit { get; set; }

        /// <summary>
        /// 驱动地址（协议语义：模拟驱动为波形微语法如 sine(20,80,60)，Modbus 为寄存器地址等）
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = false)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 高限告警阈值（null=不检测）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public decimal? AlarmHigh { get; set; }

        /// <summary>
        /// 低限告警阈值（null=不检测）
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public decimal? AlarmLow { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用（禁用的点位不参与采集）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? UpdatedAt { get; set; }
    }
}
