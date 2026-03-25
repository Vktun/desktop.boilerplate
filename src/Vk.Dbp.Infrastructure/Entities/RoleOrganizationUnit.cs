using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dabp.Infrastructure.Entities
{
    public class RoleOrganizationUnit
    {
        [Key]
        public int RoleId { get; set; }
        [Key]
        public int OrganizationUnitId { get; set; }
        
        public DateTime CreationTime { get; set; }
        public int CreatorId { get; set; }

    }
}
