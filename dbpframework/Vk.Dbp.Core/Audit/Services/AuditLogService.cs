using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.Core.Audit.Services
{
    /// <summary>
    /// 审计日志服务实现（内存实现，可扩展为数据库实现）
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly List<AuditLog> _logs = new();
        private int _nextLogId = 1;
        private readonly object _lockObject = new();

        /// <summary>
        /// 获取所有审计日志
        /// </summary>
        public async Task<List<AuditLog>> GetAllLogsAsync()
        {
            lock (_lockObject)
            {
                return _logs.OrderByDescending(l => l.OperationTime).ToList();
            }
        }

        /// <summary>
        /// 按ID获取审计日志
        /// </summary>
        public async Task<AuditLog> GetLogByIdAsync(int id)
        {
            lock (_lockObject)
            {
                return _logs.FirstOrDefault(l => l.Id == id);
            }
        }

        /// <summary>
        /// 根据用户ID获取审计日志
        /// </summary>
        public async Task<List<AuditLog>> GetLogsByUserIdAsync(int userId)
        {
            lock (_lockObject)
            {
                return _logs
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.OperationTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 根据操作类型获取审计日志
        /// </summary>
        public async Task<List<AuditLog>> GetLogsByActionTypeAsync(AuditActionType actionType)
        {
            lock (_lockObject)
            {
                return _logs
                    .Where(l => l.ActionType == actionType)
                    .OrderByDescending(l => l.OperationTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 根据日期范围获取审计日志
        /// </summary>
        public async Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            lock (_lockObject)
            {
                return _logs
                    .Where(l => l.OperationTime >= startDate && l.OperationTime <= endDate)
                    .OrderByDescending(l => l.OperationTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 根据模块获取审计日志
        /// </summary>
        public async Task<List<AuditLog>> GetLogsByModuleAsync(string module)
        {
            lock (_lockObject)
            {
                return _logs
                    .Where(l => l.Module == module)
                    .OrderByDescending(l => l.OperationTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 创建审计日志
        /// </summary>
        public async Task<bool> CreateAuditLogAsync(AuditLog log)
        {
            try
            {
                lock (_lockObject)
                {
                    log.Id = _nextLogId++;
                    _logs.Add(log);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建审计日志异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除旧的审计日志
        /// </summary>
        public async Task<bool> DeleteOldLogsAsync(int retentionDays)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                lock (_lockObject)
                {
                    var logsToDelete = _logs.Where(l => l.OperationTime < cutoffDate).ToList();
                    foreach (var log in logsToDelete)
                    {
                        _logs.Remove(log);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除旧日志异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 记录用户操作
        /// </summary>
        public async Task<bool> LogOperationAsync(
            int userId,
            string username,
            AuditActionType actionType,
            string module,
            string description,
            string entityType = null,
            int? entityId = null,
            string oldData = null,
            string newData = null,
            string clientIp = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var log = new AuditLog
                {
                    UserId = userId,
                    Username = username ?? "Unknown",
                    ActionType = actionType,
                    Module = module,
                    Description = description,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldData = oldData,
                    NewData = newData,
                    ClientIp = clientIp ?? GetLocalIpAddress(),
                    OperationTime = DateTime.Now,
                    IsSuccess = true
                };

                sw.Stop();
                log.ExecutionTime = sw.ElapsedMilliseconds;

                return await CreateAuditLogAsync(log);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录审计日志异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 记录失败的操作
        /// </summary>
        public async Task<bool> LogFailureAsync(
            int userId,
            string username,
            AuditActionType actionType,
            string module,
            string description,
            string failureReason,
            string entityType = null,
            int? entityId = null,
            string clientIp = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var log = new AuditLog
                {
                    UserId = userId,
                    Username = username ?? "Unknown",
                    ActionType = actionType,
                    Module = module,
                    Description = description,
                    EntityType = entityType,
                    EntityId = entityId,
                    ClientIp = clientIp ?? GetLocalIpAddress(),
                    OperationTime = DateTime.Now,
                    IsSuccess = false,
                    FailureReason = failureReason
                };

                sw.Stop();
                log.ExecutionTime = sw.ElapsedMilliseconds;

                return await CreateAuditLogAsync(log);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录审计日志异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 导出审计日志
        /// </summary>
        public async Task<byte[]> ExportLogsAsync(List<int> logIds)
        {
            try
            {
                lock (_lockObject)
                {
                    var logsToExport = _logs.Where(l => logIds.Contains(l.Id)).ToList();
                    var json = JsonSerializer.Serialize(logsToExport);
                    return System.Text.Encoding.UTF8.GetBytes(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"导出审计日志异常: {ex.Message}");
                return new byte[0];
            }
        }

        /// <summary>
        /// 清空所有日志
        /// </summary>
        public async Task<bool> ClearAllLogsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _logs.Clear();
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清空日志异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取本地IP地址
        /// </summary>
        private string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ipv4 = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                return ipv4?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}