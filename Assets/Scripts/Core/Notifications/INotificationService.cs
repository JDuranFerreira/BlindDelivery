using System.Collections.Generic;
using System.Threading.Tasks;

namespace Template.Core.Notifications
{
    /// <summary>What happened to a schedule call.</summary>
    public enum NotificationScheduleOutcome
    {
        /// <summary>The provider accepted a new entry.</summary>
        Scheduled,

        /// <summary>An entry with the same id already existed; the new content replaced it.</summary>
        Replaced,

        /// <summary>Nothing was scheduled: fire time in the past, or permission denied.</summary>
        Rejected,
    }

    /// <summary>
    /// Local notification decisions (goals note system 12): what is pending, permission asked
    /// *only* when the game says the moment is right — the benefit screen first, then the ask —
    /// and the pending list is reapplied to the provider on every launch, so notifications
    /// survive an app restart. Ends in tick-accuracy facts on the bus; no strings leave Core.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Last observed OS permission. NotRequested until someone explicitly asks.</summary>
        NotificationPermission PermissionState { get; }

        /// <summary>What the service believes is currently scheduled (the persisted pending list).</summary>
        IReadOnlyList<NotificationRequest> Pending { get; }

        /// <summary>
        /// The one-way door to the OS prompt. The game calls it after the benefit screen; calling
        /// it anywhere on app start is the anti-pattern this seam exists to prevent.
        /// </summary>
        Task<NotificationPermission> RequestAuthorizationAsync();

        Task<NotificationScheduleOutcome> ScheduleAsync(
            string id, string title, string body, string channel, System.DateTime fireAtUtc);

        Task CancelAsync(string id);

        Task CancelAllAsync();

        /// <summary>Reapplies the persisted pending list to the provider. Called each launch.</summary>
        Task ReschedulePendingAsync();
    }
}
