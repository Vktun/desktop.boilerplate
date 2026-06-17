using System;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Vk.Dbp.Services.Session;

namespace Dabp.WpfWindow.Logging
{
    /// <summary>
    /// 日志配置构建器
    /// </summary>
    public static class LoggingConfigurationBuilder
    {
        /// <summary>
        /// 构建增强的日志配置
        /// </summary>
        public static LoggerConfiguration BuildConfiguration(
            string logDirectory, 
            IUserSession? userSession = null)
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
            
            var config = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", assemblyName)
                .Enrich.WithProperty("Version", version)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("Environment", 
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
            
            // 添加用户上下文增强器（如果提供了UserSession）
            if (userSession != null)
            {
                config.Enrich.With(new UserLogEnricher(userSession));
            }
            
            // 配置最低日志级别
#if DEBUG
            config.MinimumLevel.Debug();
#else
            config.MinimumLevel.Information();
#endif
            
            // 覆盖特定命名空间的日志级别
            config.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
            config.MinimumLevel.Override("System", LogEventLevel.Warning);
            
            // 配置输出
            config.WriteTo.File(
                path: System.IO.Path.Join(logDirectory, "logs-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                retainedFileCountLimit: 30,
                encoding: System.Text.Encoding.UTF8
            );
            
            // 添加JSON日志（便于日志分析）
            config.WriteTo.File(
                path: System.IO.Path.Join(logDirectory, "logs-.json"),
                rollingInterval: RollingInterval.Day,
                formatter: new Serilog.Formatting.Json.JsonFormatter(renderMessage: true),
                fileSizeLimitBytes: 100 * 1024 * 1024,
                retainedFileCountLimit: 30
            );
            
#if DEBUG
            // Debug模式下输出到控制台
            config.WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
#endif
            
            return config;
        }
    }
}
