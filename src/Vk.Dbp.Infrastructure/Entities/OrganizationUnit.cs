using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SqlSugar;

namespace Dabp.Infrastructure.Entities
{
    public class OrganizationUnit
    {
        [Key]
        public int Id { get; set; }
        
        [StringLength(100)]
        public string DisplyName { get; set; } = string.Empty;
        [StringLength(60)]
        public string Code { get; set; } = string.Empty;

        public int ParentId { get; set; }
        public DateTime CreationTime { get; set; }
        public int CreatorId { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        [AllowNull]
        [SugarColumn(IsNullable = true)]
        public DateTime? LastModificationTime { get; set; }
        /// <summary>
        /// 最后修改人ID
        /// </summary>
        [AllowNull]
        [SugarColumn(IsNullable = true)]
        public int? LastModifierId { get; set; }
    }
}
