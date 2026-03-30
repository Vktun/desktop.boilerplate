using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 权限列表
    /// </summary>
    public class Permission
    {
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 显示名称
        /// </summary>
        [StringLength(100)]
        public string DisplyName { get; set; }
        /// <summary>
        /// 父级名称,默认为空
        /// </summary>
        [StringLength(100)]
        [AllowNull]
        public string ParentName { get; set; }
        /// <summary>
        /// 1.user 2.role      
        /// </summary>
        public int ProviderId { get; set; }
        /// <summary>
        /// 用户名或者权限名
        /// </summary>
              [StringLength(50)]
        public string ProviderKey { get; set; }
/// <summary>
/// 是否启用
/// </summary>
        public bool IsEnabled { get; set; }

        public DateTime CreationTime {  get; set; }

    }
}
