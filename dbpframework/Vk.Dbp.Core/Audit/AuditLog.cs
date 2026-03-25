using System;

namespace Vk.Dbp.Core.Audit
{
    /// <summary>
    /// 审计日志模型
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 操作用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 操作用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public AuditActionType ActionType { get; set; }

        /// <summary>
        /// 操作模块
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 操作的实体类型
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// 操作的实体ID
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// 旧数据（JSON格式）
        /// </summary>
        public string OldData { get; set; }

        /// <summary>
        /// 新数据（JSON格式）
        /// </summary>
        public string NewData { get; set; }

        /// <summary>
        /// 操作结果（成功/失败）
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailureReason { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作耗时（毫秒）
        /// </summary>
        public long ExecutionTime { get; set; }
    }
}