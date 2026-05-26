using System;

namespace Vk.Dbp.AccountModule.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedTime { get; set; }

        public int UserId { get; set; }
    }
}
