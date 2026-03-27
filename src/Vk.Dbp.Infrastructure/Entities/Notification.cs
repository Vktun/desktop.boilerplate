using System;
using System.ComponentModel.DataAnnotations;

namespace Dabp.Infrastructure.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(2000)]
        public string Content { get; set; }

        [StringLength(50)]
        public string Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedTime { get; set; }

        public int UserId { get; set; }
    }
}
