using BlindDelivery.Core.Events;

namespace BlindDelivery.Core.Notifications
{
    /// <summary>Past-tense fact: a local notification was scheduled (or replaced an old entry).</summary>
    public readonly struct NotificationScheduled : IEvent
    {
        public NotificationScheduled(string id, System.DateTime fireAtUtc)
        {
            this.id = id;
            this.fireAtUtc = fireAtUtc;
        }

        public readonly string id;

        /// <summary>Target fire time, UTC ticks (events are structs; a DateTime is fine inside one).</summary>
        public readonly System.DateTime fireAtUtc;
    }

    /// <summary>Past-tense fact: a scheduled notification was cancelled. <see cref="all"/> marks the cancel-everything sweep.</summary>
    public readonly struct NotificationCancelled : IEvent
    {
        public NotificationCancelled(string id, bool all)
        {
            this.id = id;
            this.all = all;
        }

        public readonly string id;

        public readonly bool all;
    }

    /// <summary>Past-tense fact: the notification permission state changed (never fired before the first explicit ask).</summary>
    public readonly struct NotificationPermissionChanged : IEvent
    {
        public NotificationPermissionChanged(NotificationPermission permission)
        {
            this.permission = permission;
        }

        public readonly NotificationPermission permission;
    }
}

