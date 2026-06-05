using System;
using System.Collections.Generic;
using Prism.Events;

namespace Vk.Dbp.Contracts.Events
{
    public class PermissionChangedPayload
    {
        public int UserId { get; set; }

        public PermissionChangeType ChangeType { get; set; }

        public List<string> AddedPermissions { get; set; } = new();

        public List<string> RemovedPermissions { get; set; } = new();

        public DateTime ChangedTime { get; set; }
    }

    public class PermissionChangedEvent : PubSubEvent<PermissionChangedPayload> { }

    public enum PermissionChangeType
    {
        Granted,
        Revoked,
        Updated
    }
}
