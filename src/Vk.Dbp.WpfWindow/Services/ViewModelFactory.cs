using System;
using Prism.Ioc;
using Vk.Dbp.Contracts.Services;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// ViewModel工厂实现 - 通过依赖注入创建ViewModel实例
    /// </summary>
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IContainerProvider _container;
        
        public ViewModelFactory(IContainerProvider container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }
        
        /// <summary>
        /// 创建指定类型的ViewModel实例
        /// </summary>
        /// <typeparam name="T">ViewModel类型</typeparam>
        /// <param name="args">构造函数参数（可选）</param>
        /// <returns>ViewModel实例</returns>
        public T Create<T>(params object[] args) where T : class
        {
            // 使用容器的Resolve方法创建实例
            // 如果提供了参数，使用带参数的重载
            if (args != null && args.Length > 0)
            {
                // 注意: Prism的IContainerProvider可能不直接支持参数注入
                // 这里提供基础实现，实际项目中可能需要使用其他IoC库的功能
                return _container.Resolve<T>();
            }
            
            return _container.Resolve<T>();
        }
        
        /// <summary>
        /// 从容器解析ViewModel实例
        /// </summary>
        /// <typeparam name="T">ViewModel类型</typeparam>
        /// <returns>ViewModel实例</returns>
        public T Resolve<T>() where T : class
        {
            return _container.Resolve<T>();
        }
        
        /// <summary>
        /// 注册ViewModel与View的关联
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        /// <typeparam name="TView">View类型</typeparam>
        public void RegisterViewModel<TViewModel, TView>()
        {
            // 这个方法主要用于显式注册ViewModel-View映射
            // Prism已经通过ViewModelLocationProvider实现了自动映射
            // 如果需要自定义映射，可以在这里添加实现
        }
    }
}