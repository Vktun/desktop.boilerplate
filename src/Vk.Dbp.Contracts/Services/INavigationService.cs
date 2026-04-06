using System;
using System.ComponentModel;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// 导航服务接口 - 提供模块间导航功能
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数（可选）</param>
        void NavigateTo(string viewName, object? parameters = null);
        
        /// <summary>
        /// 导航到指定视图（泛型版本）
        /// </summary>
        /// <typeparam name="TView">视图类型</typeparam>
        /// <param name="parameters">导航参数（可选）</param>
        void NavigateTo<TView>(object? parameters = null);
        
        /// <summary>
        /// 返回上一视图
        /// </summary>
        void GoBack();
        
        /// <summary>
        /// 导航完成事件
        /// </summary>
        event EventHandler<NavigationEventArgs>? NavigationCompleted;
    }
    
    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        /// <summary>
        /// 目标视图名称
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// 导航参数
        /// </summary>
        public object? Parameters { get; set; }
        
        /// <summary>
        /// 导航是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 错误信息（如果导航失败）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}