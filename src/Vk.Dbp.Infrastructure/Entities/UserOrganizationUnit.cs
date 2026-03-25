using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 针对用户组织关系表
    /// </summary>
    public class UserOrganizationUnit
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int OrganizationUnitId { get; set; }
        public DateTime CreationTime { get; set; }
        public int CreatorId { get; set; }
    }
}
