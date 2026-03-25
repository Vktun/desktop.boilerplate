using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dabp.Infrastructure.Entities
{
    public class OrganizationUnit
    {
        [Key]
        public int Id { get; set; }
        
        [StringLength(100)]
        public string DisplyName { get; set; }
        [StringLength(60)]
        public string Code { get; set; }

        public int ParentId { get; set; }
        public DateTime CreationTime { get; set; }
        public int CreatorId { get; set; }
        /// <summary>
        /// 最后修改时间
        /// </summary>
        [AllowNull]
        public DateTime? LastModificationTime { get; set; }
        /// <summary>
        /// 最后修改人ID
        /// </summary>
        [AllowNull]
        public int? LastModifierId { get; set; }
    }
}
