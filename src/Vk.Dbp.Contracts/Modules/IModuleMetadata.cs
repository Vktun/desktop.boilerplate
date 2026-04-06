using System;

namespace Vk.Dbp.Contracts.Modules
{
    /// <summary>
    /// 模块元数据接口 - 定义模块的基本信息和依赖关系
    /// </summary>
    public interface IModuleMetadata
    {
        /// <summary>
        /// 模块名称（唯一标识）
        /// </summary>
        string ModuleName { get; }
        
        /// <summary>
        /// 模块版本号
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// 模块描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 模块依赖列表（模块名称数组）
        /// </summary>
        string[] Dependencies { get; }
        
        /// <summary>
        /// 模块提供的服务列表（服务接口全名）
        /// </summary>
        string[] ProvidedServices { get; }
        
        /// <summary>
        /// 模块加载完成时调用
        /// </summary>
        void OnModuleLoaded();
        
        /// <summary>
        /// 模块卸载时调用（如支持热插拔）
        /// </summary>
        void OnModuleUnloading();
    }
}