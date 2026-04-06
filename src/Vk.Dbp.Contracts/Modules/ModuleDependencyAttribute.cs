using System;

namespace Vk.Dbp.Contracts.Modules
{
    /// <summary>
    /// 模块依赖声明特性 - 用于标记模块的依赖关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModuleDependencyAttribute : Attribute
    {
        /// <summary>
        /// 依赖的模块名称列表
        /// </summary>
        public string[] DependencyModules { get; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dependencyModules">依赖的模块名称</param>
        public ModuleDependencyAttribute(params string[] dependencyModules)
        {
            DependencyModules = dependencyModules ?? Array.Empty<string>();
        }
    }
}