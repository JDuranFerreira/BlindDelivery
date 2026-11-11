using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Events;
using BlindDelivery.Core.Notifications;
using BlindDelivery.Core.Save;
using BlindDelivery.Core.Services;
using UnityEngine;

namespace BlindDelivery.Services.Notifications
{
    /// <summary>
    /// The M8 notification service (goals note system 12). Owns every decision so the provider
    /// stays mechanical:
    ///
    /// - permission is never asked on its own â€” the game shows the benefit screen first, then
    ///   calls <see cref="RequestAuthorizationAsync"/>; scheduling while permission is known
    ///   denied is rejected outright instead of silently swallowed;
    /// - every schedule/cancel is applied to the provider and persisted immediately, so a fresh
    ///   service over the same store (an app restart, a crash) boots with the same pending set
    ///   and reapplies it â€” "cancel + reschedule every launch" (architecture note, M8);
    /// - facts (scheduled / cancelled / permission change) are published on the bus.
    /// </summary>
    public sealed class NotificationService : INotificationService, IAppService
    {
        /// <summary>Save key for the pending list. The schedule metadata is not progress, but it
        /// must survive restarts as a coherent document, so it rides the save service.</summary>
        const string SaveKeyPath = "notifications";

        readonly INotificationProvider provider;
        readonly IEventBus bus;
        readonly ISaveService save;
        readonly Func<DateTime> nowUtc;

        ScheduleDocument document = new ScheduleDocument();
        NotificationPermission permission = NotificationPermission.NotRequested;

        public NotificationService(
            INotificationProvider provider,
            IEventBus bus,
            ISaveService save,
            Func<DateTime> nowUtc = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            this.nowUtc = nowUtc ?? DefaultNowUtc;
        }

        static DateTime DefaultNowUtc() => DateTime.UtcNow;

        public string Name => "Notifications";

        public NotificationPermission PermissionState => permission;

        public IReadOnlyList<NotificationRequest> Pending => document.entries;

        public async Task InitializeAsync(CancellationToken token = default)
        {
            // Fresh defaults when nothing was ever saved; otherwise the pending list the last
            // session left behind. Nothing is re-fired on the bus here â€” boot restore
            // re-fires nothing, the same contract the ads/purchase restore uses.
            if (save.Exists(SaveKeyPath))
            {
                document = save.LoadAsync<ScheduleDocument>(SaveKeyPath).GetAwaiter().GetResult();
            }

            permission = await provider.GetPermissionAsync();
            await ReschedulePendingAsync();
        }

        public async Task<NotificationPermission> RequestAuthorizationAsync()
        {
            NotificationPermission answered = await provider.RequestAuthorizationAsync();
            if (answered != permission)
            {
                permission = answered;
                bus.Publish(new NotificationPermissionChanged(answered));
            }

            return permission;
        }

        public async Task<NotificationScheduleOutcome> ScheduleAsync(
            string id, string title, string body, string channel, DateTime fireAtUtc)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A notification id is required.", nameof(id));
            }

            if (permission == NotificationPermission.Denied || permission == NotificationPermission.Unsupported)
            {
                return NotificationScheduleOutcome.Rejected;
            }

            if (fireAtUtc <= nowUtc())
            {
                return NotificationScheduleOutcome.Rejected;
            }

            NotificationRequest existing = document.entries.FirstOrDefault(e => e.id == id);
            NotificationScheduleOutcome outcome;
            if (existing != null)
            {
                existing.title = title ?? "";
                existing.body = body ?? "";
                existing.channel = channel ?? "";
                existing.fireAtUtcTicks = fireAtUtc.Ticks;
                outcome = NotificationScheduleOutcome.Replaced;
            }
            else
            {
                document.entries.Add(new NotificationRequest
                {
                    id = id,
                    title = title ?? "",
                    body = body ?? "",
                    channel = channel ?? "",
                    fireAtUtcTicks = fireAtUtc.Ticks,
                });
                outcome = NotificationScheduleOutcome.Scheduled;
            }

            Persist();
            await provider.ScheduleAsync(document.entries.First(e => e.id == id));
            bus.Publish(new NotificationScheduled(id, fireAtUtc));
            return outcome;
        }

        public async Task CancelAsync(string id)
        {
            int removed = document.entries.RemoveAll(e => e.id == id);
            if (removed == 0)
            {
                return;
            }

            Persist();
            await provider.CancelAsync(id);
            bus.Publish(new NotificationCancelled(id, all: false));
        }

        public async Task CancelAllAsync()
        {
            if (document.entries.Count == 0)
            {
                return;
            }

            document.entries.Clear();
            Persist();
            await provider.CancelAllAsync();
            bus.Publish(new NotificationCancelled(id: "", all: true));
        }

        public async Task ReschedulePendingAsync()
        {
            // Entries that were supposed to fire while the app was closed can never fire; keeping
            // them would resurrect stale notifications on every future launch (architecture note,
            // M8: cancel + reschedule every launch). They are pruned and dropped, not re-fired â€”
            // rescheduling is only for entries still in the future.
            DateTime now = nowUtc();
            int expired = document.entries.RemoveAll(e => e.FireAtUtc <= now);

            bool blocked = permission == NotificationPermission.Denied
                || permission == NotificationPermission.Unsupported;
            if (expired > 0 || blocked)
            {
                Persist();
            }

            if (blocked)
            {
                // A revoked permission may leave entries on the OS from an earlier session.
                await provider.CancelAllAsync();
                return;
            }

            List<Task> sends = new List<Task>();
            foreach (NotificationRequest entry in document.entries)
            {
                sends.Add(provider.ScheduleAsync(entry));
            }

            await Task.WhenAll(sends);
        }

        void Persist()
        {
            save.SaveAsync(SaveKeyPath, document).GetAwaiter().GetResult();
        }

        /// <summary>JsonUtility lives in the engine, so serialization stays in Services (RemoteConfig pattern).</summary>
        [Serializable]
        sealed class ScheduleDocument
        {
            public List<NotificationRequest> entries = new List<NotificationRequest>();
        }
    }
}

