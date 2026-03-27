using System;

namespace Vk.Dbp.AccountModule.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedTime { get; set; }

        public int UserId { get; set; }
    }
}
