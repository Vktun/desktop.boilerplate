using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dabp.Infrastructure.Entities
{
    public class UserRole
    {
        [Key]
        public int UserId { get; set; }
        [Key]
        public int RoleId { get; set; }

    }
}
