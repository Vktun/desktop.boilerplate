using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vk.Dbp.Core.Audit.Interfaces
{
    /// <summary>
    /// 审计日志服务接口
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// 获取所有审计日志
        /// </summary>
        Task<List<AuditLog>> GetAllLogsAsync();

        /// <summary>
        /// 按ID获取审计日志
        /// </summary>
        Task<AuditLog> GetLogByIdAsync(int id);

        /// <summary>
        /// 根据用户ID获取审计日志
        /// </summary>
        Task<List<AuditLog>> GetLogsByUserIdAsync(int userId);

        /// <summary>
        /// 根据操作类型获取审计日志
        /// </summary>
        Task<List<AuditLog>> GetLogsByActionTypeAsync(AuditActionType actionType);

        /// <summary>
        /// 根据日期范围获取审计日志
        /// </summary>
        Task<List<AuditLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 根据模块获取审计日志
        /// </summary>
        Task<List<AuditLog>> GetLogsByModuleAsync(string module);

        /// <summary>
        /// 创建审计日志
        /// </summary>
        Task<bool> CreateAuditLogAsync(AuditLog log);

        /// <summary>
        /// 删除旧的审计日志（保留天数）
        /// </summary>
        Task<bool> DeleteOldLogsAsync(int retentionDays);

        /// <summary>
        /// 记录用户操作
        /// </summary>
        Task<bool> LogOperationAsync(
            int userId,
            string username,
            AuditActionType actionType,
            string module,
            string description,
            string entityType = null,
            int? entityId = null,
            string oldData = null,
            string newData = null,
            string clientIp = null);

        /// <summary>
        /// 记录失败的操作
        /// </summary>
        Task<bool> LogFailureAsync(
            int userId,
            string username,
            AuditActionType actionType,
            string module,
            string description,
            string failureReason,
            string entityType = null,
            int? entityId = null,
            string clientIp = null);

        /// <summary>
        /// 导出审计日志
        /// </summary>
        Task<byte[]> ExportLogsAsync(List<int> logIds);

        /// <summary>
        /// 清空所有日志（谨慎使用）
        /// </summary>
        Task<bool> ClearAllLogsAsync();
    }
}