using System.Collections.Generic;
using System.Threading.Tasks;
using BlindDelivery.Core.Notifications;

namespace BlindDelivery.Services.Notifications
{
    /// <summary>
    /// The BlindDelivery's default provider (M6/M7 pattern: stubs at the SDK seam, zero package
    /// dependency). It keeps the last permission answer and the currently-scheduled set in
    /// memory, so the service's logic can be exercised â€” a game installs
    /// com.unity.mobile.notifications and passes its own provider at this seam.
    /// </summary>
    public sealed class StubNotificationProvider : INotificationProvider
    {
        public NotificationPermission NextPermission = NotificationPermission.Granted;

        /// <summary>What the OS prompt answers when explicitly asked â€” separate from the
        /// passive permission read above, matching a real device (read: NotRequested, ask: Granted).</summary>
        public NotificationPermission NextRequestAnswer = NotificationPermission.Granted;

        /// <summary>What the OS prompt was asked (incremented only by RequestAuthorizationAsync).</summary>
        public int RequestCalls;

        readonly Dictionary<string, NotificationRequest> scheduled = new Dictionary<string, NotificationRequest>();

        /// <summary>For inspection: what the provider currently believes is scheduled.</summary>
        public IReadOnlyDictionary<string, NotificationRequest> Scheduled => scheduled;

        public Task<NotificationPermission> GetPermissionAsync()
        {
            return Task.FromResult(NextPermission);
        }

        public Task<NotificationPermission> RequestAuthorizationAsync()
        {
            RequestCalls++;
            return Task.FromResult(NextRequestAnswer);
        }

        public Task ScheduleAsync(NotificationRequest request)
        {
            scheduled[request.id] = request;
            return Task.CompletedTask;
        }

        public Task CancelAsync(string id)
        {
            scheduled.Remove(id);
            return Task.CompletedTask;
        }

        public Task CancelAllAsync()
        {
            scheduled.Clear();
            return Task.CompletedTask;
        }
    }
}


