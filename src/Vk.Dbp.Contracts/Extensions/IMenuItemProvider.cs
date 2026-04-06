using System;
using System.Collections.Generic;

namespace Vk.Dbp.Contracts.Extensions
{
    /// <summary>
    /// 菜单项提供者接口 - 允许模块添加自定义菜单项
    /// </summary>
    public interface IMenuItemProvider
    {
        /// <summary>
        /// 获取菜单项列表
        /// </summary>
        /// <returns>菜单项集合</returns>
        IEnumerable<MenuItemInfo> GetMenuItems();
    }
    
    /// <summary>
    /// 菜单项信息
    /// </summary>
    public class MenuItemInfo
    {
        /// <summary>
        /// 菜单项唯一标识
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// 图标路径（如 pack://application:,,,/Resources/Icons/icon.png）
        /// </summary>
        public string? Icon { get; set; }
        
        /// <summary>
        /// 导航目标视图名称
        /// </summary>
        public string NavigateTo { get; set; } = string.Empty;
        
        /// <summary>
        /// 所需权限代码（可选）
        /// </summary>
        public string? RequiredPermission { get; set; }
        
        /// <summary>
        /// 排序顺序（数字越小越靠前）
        /// </summary>
        public int Order { get; set; }
        
        /// <summary>
        /// 父菜单名称（用于子菜单）
        /// </summary>
        public string? ParentMenu { get; set; }
        
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}