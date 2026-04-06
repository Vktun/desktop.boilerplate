using System;
using System.Diagnostics;
using Serilog;

namespace Dabp.Utils.Logging
{
    /// <summary>
    /// 性能日志记录器 - 自动记录操作耗时
    /// </summary>
    public class PerformanceLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly string? _additionalInfo;
        private bool _disposed;
        
        /// <summary>
        /// 创建性能日志记录器
        /// </summary>
        /// <param name="logger">Serilog logger实例</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="additionalInfo">附加信息（可选）</param>
        public PerformanceLogger(ILogger logger, string operationName, string? additionalInfo = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _additionalInfo = additionalInfo;
            _stopwatch = Stopwatch.StartNew();
            
            _logger.Debug("开始执行操作: {OperationName} {AdditionalInfo}", 
                _operationName, _additionalInfo ?? "");
        }
        
        /// <summary>
        /// 停止计时并记录耗时
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _stopwatch.Stop();
            var elapsedMs = _stopwatch.ElapsedMilliseconds;
            
            // 根据耗时选择不同的日志级别
            if (elapsedMs < 100)
            {
                _logger.Debug("操作完成: {OperationName} 耗时 {ElapsedMs}ms {AdditionalInfo}",
                    _operationName, elapsedMs, _additionalInfo ?? "");
            }
            else if (elapsedMs < 500)
            {
                _logger.Information("操作完成: {OperationName} 耗时 {ElapsedMs}ms {AdditionalInfo}",
                    _operationName, elapsedMs, _additionalInfo ?? "");
            }
            else if (elapsedMs < 2000)
            {
                _logger.Warning("操作较慢: {OperationName} 耗时 {ElapsedMs}ms {AdditionalInfo}",
                    _operationName, elapsedMs, _additionalInfo ?? "");
            }
            else
            {
                _logger.Warning("操作耗时过长: {OperationName} 耗时 {ElapsedMs}ms {AdditionalInfo}",
                    _operationName, elapsedMs, _additionalInfo ?? "");
            }
            
            _disposed = true;
        }
        
        /// <summary>
        /// 记录中间步骤
        /// </summary>
        public void LogStep(string stepName)
        {
            var elapsedMs = _stopwatch.ElapsedMilliseconds;
            _logger.Debug("操作步骤: {OperationName} -> {StepName} 累计耗时 {ElapsedMs}ms",
                _operationName, stepName, elapsedMs);
        }
    }
    
    /// <summary>
    /// 性能日志扩展方法
    /// </summary>
    public static class PerformanceLoggerExtensions
    {
        /// <summary>
        /// 开始性能监控
        /// </summary>
        /// <example>
        /// using (_logger.BeginPerformance("LoadUsers", "Count: 100"))
        /// {
        ///     var users = await _userService.GetAllUsersAsync();
        /// }
        /// </example>
        public static PerformanceLogger BeginPerformance(
            this ILogger logger, 
            string operationName, 
            string? additionalInfo = null)
        {
            return new PerformanceLogger(logger, operationName, additionalInfo);
        }
    }
}