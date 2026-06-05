using System;
using Prism.Events;

namespace Vk.Dbp.Contracts.Events
{
    public class UserLoggedInPayload
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; }

        public string? IpAddress { get; set; }
    }

    public class UserLoggedInEvent : PubSubEvent<UserLoggedInPayload> { }
}
