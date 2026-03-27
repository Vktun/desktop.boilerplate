using System;
using System.ComponentModel.DataAnnotations;

namespace Dabp.Infrastructure.Entities
{
    public class RolePermission
    {
        [Key]
        public int RoleId { get; set; }

        [Key]
        public int PermissionId { get; set; }

        public DateTime CreationTime { get; set; }

        public int CreatorId { get; set; }
    }
}
