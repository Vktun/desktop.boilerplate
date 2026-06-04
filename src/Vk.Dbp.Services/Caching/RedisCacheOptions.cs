using System;
using System.Reflection;

namespace Dabp.Services.Caching;

/// <summary>
/// Redis 缓存配置。
/// </summary>
public sealed class RedisCacheOptions
{
    /// <summary>
    /// 是否启用 Redis 缓存。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Redis 连接字符串。
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 缓存键前缀，用于隔离不同应用的缓存空间。
    /// </summary>
    public string InstanceName { get; set; } = GetDefaultInstanceName();

    private static string GetDefaultInstanceName()
    {
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "DabpDesktopBoilerplate";
        return appName;
    }
}
