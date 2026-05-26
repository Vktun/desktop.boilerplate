using System;
using System.ComponentModel.DataAnnotations;

namespace Dabp.Infrastructure.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedTime { get; set; }

        public int UserId { get; set; }
    }
}
