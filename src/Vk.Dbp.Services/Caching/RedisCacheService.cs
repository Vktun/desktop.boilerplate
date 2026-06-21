using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using Vk.Dbp.Contracts.Caching;

namespace Dabp.Services.Caching;

/// <summary>
/// Redis 缓存服务实现。
/// </summary>
public sealed class RedisCacheService : ICacheService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database;
    private readonly string _instanceName;
    private readonly ConnectionMultiplexer _multiplexer;

    /// <summary>
    /// 初始化 Redis 缓存服务。
    /// </summary>
    /// <param name="options">Redis 配置。</param>
    public RedisCacheService(RedisCacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new ArgumentException("Redis connection string is required when Redis cache is enabled.", nameof(options));
        }

        _instanceName = NormalizeInstanceName(options.InstanceName);
        _multiplexer = ConnectionMultiplexer.Connect(options.ConnectionString);
        _database = _multiplexer.GetDatabase();
    }

    /// <summary>
    /// 获取或创建缓存项。
    /// </summary>
    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiry = null)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (TryGetValue(key, out T? cachedValue))
        {
            return cachedValue!;
        }

        T createdValue = factory();
        Set(key, createdValue, expiry);
        return createdValue;
    }

    /// <summary>
    /// 异步获取或创建缓存项。
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cachedResult = await TryGetValueAsync<T>(key).ConfigureAwait(false);
        if (cachedResult.found)
        {
            return cachedResult.value!;
        }

        T createdValue = await factory().ConfigureAwait(false);
        await SetAsync(key, createdValue, expiry).ConfigureAwait(false);
        return createdValue;
    }

    /// <summary>
    /// 获取缓存项。
    /// </summary>
    public T? Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return default;
        }

        return TryGetValue(key, out T? value) ? value : default;
    }

    /// <summary>
    /// 设置缓存项。
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        ValidateKey(key);
        string serializedValue = JsonSerializer.Serialize(value, JsonOptions);
        _database.StringSet(GetRedisKey(key), serializedValue, expiry);
    }

    /// <summary>
    /// 移除缓存项。
    /// </summary>
    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _database.KeyDelete(GetRedisKey(key));
    }

    /// <summary>
    /// 按模式移除缓存项。
    /// </summary>
    public void RemoveByPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        DeleteKeys(BuildPattern(pattern));
    }

    /// <summary>
    /// 清空当前实例前缀下的所有缓存。
    /// </summary>
    public void Clear()
    {
        DeleteKeys(BuildPattern("*"));
    }

    /// <summary>
    /// 检查缓存项是否存在。
    /// </summary>
    public bool Exists(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _database.KeyExists(GetRedisKey(key));
    }

    /// <summary>
    /// 释放 Redis 连接。
    /// </summary>
    public void Dispose()
    {
        _multiplexer.Dispose();
    }

    private void DeleteKeys(string pattern)
    {
        RedisKey[] keysToDelete = _multiplexer.GetEndPoints()
            .Select(endpoint => _multiplexer.GetServer(endpoint))
            .Where(server => server.IsConnected)
            .SelectMany(server => server.Keys(_database.Database, pattern: pattern))
            .Distinct()
            .ToArray();

        if (keysToDelete.Length > 0)
        {
            _database.KeyDelete(keysToDelete);
        }
    }

    private RedisKey GetRedisKey(string key)
    {
        return $"{_instanceName}{key}";
    }

    private string BuildPattern(string pattern)
    {
        return $"{_instanceName}{pattern}";
    }

    private static string NormalizeInstanceName(string? instanceName)
    {
        string normalized = string.IsNullOrWhiteSpace(instanceName)
            ? "DabpDesktopBoilerplate"
            : instanceName.Trim();

        return normalized.EndsWith(':')
            ? normalized
            : $"{normalized}:";
    }

    private bool TryGetValue<T>(string key, out T? value)
    {
        RedisValue redisValue = _database.StringGet(GetRedisKey(key));
        if (redisValue.IsNull)
        {
            value = default;
            return false;
        }

        value = JsonSerializer.Deserialize<T>(redisValue.ToString(), JsonOptions);
        return true;
    }

    private async Task<(bool found, T? value)> TryGetValueAsync<T>(string key)
    {
        RedisValue redisValue = await _database.StringGetAsync(GetRedisKey(key)).ConfigureAwait(false);
        if (redisValue.IsNull)
        {
            return (false, default);
        }

        T? value = JsonSerializer.Deserialize<T>(redisValue.ToString(), JsonOptions);
        return (true, value);
    }

    private Task SetAsync<T>(string key, T value, TimeSpan? expiry)
    {
        string serializedValue = JsonSerializer.Serialize(value, JsonOptions);
        return _database.StringSetAsync(GetRedisKey(key), serializedValue, expiry);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("缓存键不能为空", nameof(key));
        }
    }
}
