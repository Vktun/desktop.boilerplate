using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using SqlSugar;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 审计日志
    /// </summary>
    public class AuditLog
    {
        // [Key] 无自增语义：CodeFirst 建出的 Id 列不带自增，所有行 Id=0，按 Id 的查询/导出全部失效。
        // long 在 SQLite 下无法 AUTOINCREMENT（仅允许 INTEGER PRIMARY KEY），与 AlarmRecord 对齐用 int。
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        /// <summary>
        /// 模块名称
        /// </summary>
        [StringLength(100)]

        public string ModuleName { get; set; } = string.Empty;
        /// <summary>
        /// 服务名称
        /// </summary>
        [StringLength(100)]
        public string ServiceName { get; set; } = string.Empty;
        /// <summary>
        /// 方法名称
        /// </summary>
        [StringLength(200)]
        public string MethodName { get; set; } = string.Empty;
        /// <summary>
        /// 操作类型
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 参数
        /// </summary>
        [StringLength(5000)]
        [AllowNull]
        public string Parameters { get; set; }
        /// <summary>
        /// 操作人
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 操作人名称
        /// </summary>
        [StringLength(100)]

        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime ExecutionTime { get; set; }
        /// <summary>
        /// 执行时长
        /// </summary>
        public long ExecutionDuration { get; set; }
        /// <summary>
        /// 异常信息
        /// </summary>
        [AllowNull]
        [StringLength(5000)]
        public string Exceptions { get; set; }

    }
}
