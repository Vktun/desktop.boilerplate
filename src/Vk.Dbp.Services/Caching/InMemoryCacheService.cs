using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vk.Dbp.Contracts.Caching;

namespace Dabp.Services.Caching
{
    /// <summary>
    /// 内存缓存服务实现 - 使用ConcurrentDictionary提供线程安全的缓存
    /// </summary>
    public class InMemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly object _cleanupLock = new();
        private readonly SemaphoreSlim _asyncFactoryLock = new(1, 1);
        private DateTime _lastCleanup = DateTime.MinValue;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// 获取或创建缓存项
        /// </summary>
        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("缓存键不能为空", nameof(key));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            // 定期清理过期缓存
            TryCleanup();
            
            var entry = _cache.GetOrAdd(key, k =>
            {
                var value = factory();
                return new CacheEntry(value, expiry);
            });
            
            // 如果缓存已过期，重新创建
            if (entry.IsExpired)
            {
                var newEntry = new CacheEntry(factory(), expiry);
                _cache.TryUpdate(key, newEntry, entry);
                return (T)newEntry.Value!;
            }
            
            return (T)entry.Value!;
        }
        
        /// <summary>
        /// 异步获取或创建缓存项
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("缓存键不能为空", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            TryCleanup();

            if (_cache.TryGetValue(key, out var existingEntry) && !existingEntry.IsExpired)
            {
                return (T)existingEntry.Value!;
            }

            await _asyncFactoryLock.WaitAsync();
            try
            {
                if (_cache.TryGetValue(key, out existingEntry) && !existingEntry.IsExpired)
                {
                    return (T)existingEntry.Value!;
                }

                T value = await factory();
                var newEntry = new CacheEntry(value, expiry);
                _cache.AddOrUpdate(key, newEntry, (_, _) => newEntry);
                return value;
            }
            finally
            {
                _asyncFactoryLock.Release();
            }
        }
        /// <summary>
        /// 获取缓存项
        /// </summary>
        public T? Get<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;
            
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                return (T?)entry.Value;
            }
            
            return default;
        }
        
        /// <summary>
        /// 设置缓存项
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("缓存键不能为空", nameof(key));
            
            var entry = new CacheEntry(value, expiry);
            _cache.AddOrUpdate(key, entry, (k, e) => entry);
        }
        
        /// <summary>
        /// 移除缓存项
        /// </summary>
        public void Remove(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _cache.TryRemove(key, out _);
            }
        }
        
        /// <summary>
        /// 移除匹配模式的所有缓存项
        /// </summary>
        public void RemoveByPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;
            
            // 将通配符模式转换为正则表达式
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();
            
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }
        
        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
        
        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        public bool Exists(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            
            return _cache.TryGetValue(key, out var entry) && !entry.IsExpired;
        }
        
        /// <summary>
        /// 尝试清理过期缓存
        /// </summary>
        private void TryCleanup()
        {
            if (DateTime.Now - _lastCleanup < _cleanupInterval)
                return;
            
            lock (_cleanupLock)
            {
                if (DateTime.Now - _lastCleanup < _cleanupInterval)
                    return;
                
                var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }
                
                _lastCleanup = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 缓存条目
        /// </summary>
        private class CacheEntry
        {
            public object? Value { get; }
            public DateTime? ExpiryTime { get; }
            
            public CacheEntry(object? value, TimeSpan? expiry)
            {
                Value = value;
                ExpiryTime = expiry.HasValue ? DateTime.Now.Add(expiry.Value) : null;
            }
            
            public bool IsExpired => ExpiryTime.HasValue && DateTime.Now > ExpiryTime.Value;
        }
    }
}
