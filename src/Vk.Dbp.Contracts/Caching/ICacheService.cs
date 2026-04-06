using System;
using System.Threading.Tasks;

namespace Vk.Dbp.Contracts.Caching
{
    /// <summary>
    /// 缓存服务接口 - 提供内存缓存功能
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取或创建缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">缓存项工厂方法</param>
        /// <param name="expiry">过期时间（可选）</param>
        /// <returns>缓存项</returns>
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiry = null);
        
        /// <summary>
        /// 异步获取或创建缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">缓存项工厂方法</param>
        /// <param name="expiry">过期时间（可选）</param>
        /// <returns>缓存项</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
        
        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存项，如果不存在则返回default</returns>
        T? Get<T>(string key);
        
        /// <summary>
        /// 设置缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expiry">过期时间（可选）</param>
        void Set<T>(string key, T value, TimeSpan? expiry = null);
        
        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        void Remove(string key);
        
        /// <summary>
        /// 移除匹配模式的所有缓存项
        /// </summary>
        /// <param name="pattern">缓存键模式（如 "user:*"）</param>
        void RemoveByPattern(string pattern);
        
        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        bool Exists(string key);
    }
}