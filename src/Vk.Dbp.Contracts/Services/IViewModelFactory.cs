using System;

namespace Vk.Dbp.Contracts.Services
{
    /// <summary>
    /// ViewModel工厂接口 - 用于创建和解析ViewModel实例
    /// </summary>
    public interface IViewModelFactory
    {
        /// <summary>
        /// 创建指定类型的ViewModel实例
        /// </summary>
        /// <typeparam name="T">ViewModel类型</typeparam>
        /// <param name="args">构造函数参数（可选）</param>
        /// <returns>ViewModel实例</returns>
        T Create<T>(params object[] args) where T : class;
        
        /// <summary>
        /// 从容器解析ViewModel实例
        /// </summary>
        /// <typeparam name="T">ViewModel类型</typeparam>
        /// <returns>ViewModel实例</returns>
        T Resolve<T>() where T : class;
        
        /// <summary>
        /// 注册ViewModel与View的关联
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        /// <typeparam name="TView">View类型</typeparam>
        void RegisterViewModel<TViewModel, TView>();
    }
}