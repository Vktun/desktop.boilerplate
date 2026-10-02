using SqlSugar;
using System;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 采集设备表（工业运行时内核）。一台设备 = 一个协议驱动连接实例 + 一组点位。
    /// </summary>
    [SugarTable("Devices")]
    [SugarIndex("UX_Devices_Code", nameof(Code), OrderByType.Asc, true)]
    public class Device
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 设备编码（全局唯一，如 SIM-DEMO-01）
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 协议类型（取值见 Vk.Dbp.Contracts.Industrial.ProtocolTypes 常量；存字符串便于第三方驱动扩展）
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false)]
        public string ProtocolType { get; set; } = string.Empty;

        /// <summary>
        /// 连接配置（协议相关的 JSON 串，如 {"pollIntervalMs":1000}、Modbus 的 host/port/unitId）
        /// </summary>
        [SugarColumn(Length = 1000, IsNullable = true)]
        public string? ConnectionConfig { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用（禁用的设备不参与引擎调度）
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
