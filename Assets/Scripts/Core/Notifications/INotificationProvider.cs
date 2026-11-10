using System;
using System.Threading.Tasks;

namespace Template.Core.Notifications
{
    /// <summary>OS permission state for local notifications.</summary>
    public enum NotificationPermission
    {
        /// <summary>Nobody asked yet — the prompt has not been shown to the player.</summary>
        NotRequested,

        /// <summary>Notifications are allowed.</summary>
        Granted,

        /// <summary>The player said no; scheduling is a no-op until they change it in system settings.</summary>
        Denied,

        /// <summary>The platform cannot schedule notifications (web, some sims).</summary>
        Unsupported,
    }

    /// <summary>
    /// One local notification. A plain pure-C# class so Core stays engine-free; the serializable
    /// field layout doubles as the JSON payload the service persists, so a game restart can
    /// reschedule exactly what was pending (architecture note, M8 acceptance).
    /// </summary>
    [Serializable]
    public sealed class NotificationRequest
    {
        /// <summary>Stable game-chosen id; scheduling the same id replaces the old entry.</summary>
        public string id = "";

        public string title = "";

        public string body = "";

        /// <summary>Android channel / iOS emphasis bucket. Empty means the provider's default channel.</summary>
        public string channel = "";

        /// <summary>Target fire time. Serialized as ticks so JsonUtility round-trips it losslessly.</summary>
        public long fireAtUtcTicks;

        public System.DateTime FireAtUtc => new System.DateTime(fireAtUtcTicks, System.DateTimeKind.Utc);
    }

    /// <summary>
    /// The low-level seam the engine SDK plugs into (Unity Mobile Notifications, Firebase, a
    /// sim). Mechanics only: one prompt, schedule/cancel, read the OS permission state. Every
    /// decision — when to ask, which entries exist, rescheduling each launch — lives in
    /// <see cref="INotificationService"/> so it is testable with a stub.
    /// </summary>
    public interface INotificationProvider
    {
        /// <summary>Current OS state. Asking the OS is the engine call, not the service's job.</summary>
        Task<NotificationPermission> GetPermissionAsync();

        /// <summary>Shows the OS permission prompt. Called only through the service's explicit ask.</summary>
        Task<NotificationPermission> RequestAuthorizationAsync();

        Task ScheduleAsync(NotificationRequest request);

        Task CancelAsync(string id);

        Task CancelAllAsync();
    }
}
