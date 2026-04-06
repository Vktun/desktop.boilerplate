using System;
using System.Collections.Generic;

namespace Vk.Dbp.Contracts.Extensions
{
    /// <summary>
    /// 仪表盘小组件提供者接口 - 允许模块添加自定义仪表盘组件
    /// </summary>
    public interface IDashboardWidgetProvider
    {
        /// <summary>
        /// 获取小组件列表
        /// </summary>
        /// <returns>小组件集合</returns>
        IEnumerable<DashboardWidget> GetWidgets();
    }
    
    /// <summary>
    /// 仪表盘小组件
    /// </summary>
    public class DashboardWidget
    {
        /// <summary>
        /// 小组件唯一标识
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// 小组件标题
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 小组件视图名称
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// 小组件宽度（单位：列数，1-12）
        /// </summary>
        public int Width { get; set; } = 4;
        
        /// <summary>
        /// 小组件高度（单位：行数）
        /// </summary>
        public int Height { get; set; } = 2;
        
        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order { get; set; }
        
        /// <summary>
        /// 所需权限代码（可选）
        /// </summary>
        public string? RequiredPermission { get; set; }
        
        /// <summary>
        /// 小组件分类
        /// </summary>
        public string Category { get; set; } = "General";
        
        /// <summary>
        /// 小组件图标
        /// </summary>
        public string? Icon { get; set; }
    }
}