using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.Core.Audit.Extensions
{
    /// <summary>
    /// 审计日志扩展方法
    /// </summary>
    public static class AuditLogExtensions
    {
        /// <summary>
        /// 记录创建操作
        /// </summary>
        public static async Task<bool> LogCreateAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string entityType,
            int entityId,
            object newData,
            string description = null,
            string clientIp = null)
        {
            var newDataJson = JsonSerializer.Serialize(newData);
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Create, module,
                description ?? $"创建{entityType}",
                entityType, entityId, null, newDataJson, clientIp);
        }

        /// <summary>
        /// 记录更新操作
        /// </summary>
        public static async Task<bool> LogUpdateAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string entityType,
            int entityId,
            object oldData,
            object newData,
            string description = null,
            string clientIp = null)
        {
            var oldDataJson = JsonSerializer.Serialize(oldData);
            var newDataJson = JsonSerializer.Serialize(newData);
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Update, module,
                description ?? $"更新{entityType}",
                entityType, entityId, oldDataJson, newDataJson, clientIp);
        }

        /// <summary>
        /// 记录删除操作
        /// </summary>
        public static async Task<bool> LogDeleteAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string entityType,
            int entityId,
            object deletedData = null,
            string description = null,
            string clientIp = null)
        {
            var oldDataJson = deletedData != null ? JsonSerializer.Serialize(deletedData) : null;
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Delete, module,
                description ?? $"删除{entityType}",
                entityType, entityId, oldDataJson, null, clientIp);
        }

        /// <summary>
        /// 记录登录操作
        /// </summary>
        public static async Task<bool> LogLoginAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Login, "Account",
                $"用户登录: {username}", clientIp: clientIp);
        }

        /// <summary>
        /// 记录登出操作
        /// </summary>
        public static async Task<bool> LogLogoutAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Logout, "Account",
                $"用户登出: {username}", clientIp: clientIp);
        }

        /// <summary>
        /// 记录修改密码操作
        /// </summary>
        public static async Task<bool> LogChangePasswordAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.ChangePassword, "Account",
                $"用户修改密码: {username}", clientIp: clientIp);
        }

        /// <summary>
        /// 记录导出操作
        /// </summary>
        public static async Task<bool> LogExportAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string description,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Export, module,
                description, clientIp: clientIp);
        }

        /// <summary>
        /// 记录导入操作
        /// </summary>
        public static async Task<bool> LogImportAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string description,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Import, module,
                description, clientIp: clientIp);
        }

        /// <summary>
        /// 记录下载操作
        /// </summary>
        public static async Task<bool> LogDownloadAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string description,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.Download, module,
                description, clientIp: clientIp);
        }

        /// <summary>
        /// 记录查看详情操作
        /// </summary>
        public static async Task<bool> LogViewAsync(
            this IAuditLogService service,
            int userId,
            string username,
            string module,
            string entityType,
            int? entityId,
            string clientIp = null)
        {
            return await service.LogOperationAsync(
                userId, username, AuditActionType.View, module,
                $"查看{entityType}详情",
                entityType, entityId, clientIp: clientIp);
        }
    }
}