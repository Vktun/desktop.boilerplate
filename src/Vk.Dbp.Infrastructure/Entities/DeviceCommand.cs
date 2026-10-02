using SqlSugar;
using System;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 设备命令表（工业运行时内核）。命令 = 预定义的点位回写（命令回写），供运行时服务按 ID 执行。
    /// </summary>
    [SugarTable("DeviceCommands")]
    [SugarIndex("IX_DeviceCommands_DeviceId", nameof(DeviceId), OrderByType.Asc)]
    public class DeviceCommand
    {
        /// <summary>
        /// 命令ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 所属设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 命令编码（设备内唯一）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 命令名称（界面显示用）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 目标点位编码（回写落点）
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string TargetPointCode { get; set; } = string.Empty;

        /// <summary>
        /// 写入值（字符串形式，由驱动按点位数据类型解析）
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string? WriteValue { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用
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
