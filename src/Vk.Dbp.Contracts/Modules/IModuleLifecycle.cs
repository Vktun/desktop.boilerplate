using System;

namespace Vk.Dbp.Contracts.Modules
{
    /// <summary>
    /// 模块生命周期接口 - 提供模块加载和卸载的生命周期钩子
    /// </summary>
    public interface IModuleLifecycle
    {
        /// <summary>
        /// 模块初始化 - 在模块注册完成后调用
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 模块激活 - 当模块的所有依赖都已加载时调用
        /// </summary>
        void Activate();
        
        /// <summary>
        /// 模块停用 - 当应用关闭或模块卸载时调用
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// 获取模块状态
        /// </summary>
        ModuleState State { get; }
    }
    
    /// <summary>
    /// 模块状态枚举
    /// </summary>
    public enum ModuleState
    {
        /// <summary>
        /// 未初始化
        /// </summary>
        NotInitialized,
        
        /// <summary>
        /// 已初始化
        /// </summary>
        Initialized,
        
        /// <summary>
        /// 已激活
        /// </summary>
        Activated,
        
        /// <summary>
        /// 已停用
        /// </summary>
        Deactivated
    }
}