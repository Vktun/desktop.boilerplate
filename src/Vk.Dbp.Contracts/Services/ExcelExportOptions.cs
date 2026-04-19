using System;
using System.Collections.Generic;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// Excel导出配置选项
    /// </summary>
    public class ExcelExportOptions
    {
        /// <summary>
        /// 工作表标题（可选）
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 列名映射字典 - 属性名 -> 显示名称
        /// 例如: { "AlarmCode", "告警代码" }
        /// </summary>
        public Dictionary<string, string>? ColumnDisplayNames { get; set; }

        /// <summary>
        /// 需要排除的列名列表
        /// </summary>
        public List<string>? ExcludedColumns { get; set; }

        /// <summary>
        /// 列格式化字典 - 属性名 -> 格式字符串
        /// 例如: { "CreateTime", "yyyy-MM-dd HH:mm:ss" }
        /// </summary>
        public Dictionary<string, string>? ColumnFormats { get; set; }

        /// <summary>
        /// 是否启用自动筛选（默认true）
        /// </summary>
        public bool AutoFilter { get; set; } = true;

        /// <summary>
        /// 是否冻结标题行（默认true）
        /// </summary>
        public bool FreezeHeader { get; set; } = true;

        /// <summary>
        /// 枚举值映射字典 - 枚举类型全名 到 枚举值映射的字典
        /// 例如: { "Vk.Dbp.Domain.AlarmLevel", { [AlarmLevel.Critical] = "严重" } }
        /// </summary>
        public Dictionary<string, Dictionary<object, string>>? EnumMappings { get; set; }
    }
}