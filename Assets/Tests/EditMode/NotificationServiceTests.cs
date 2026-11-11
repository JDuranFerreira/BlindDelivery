using System;
using NUnit.Framework;
using BlindDelivery.Core.Events;
using BlindDelivery.Core.Notifications;
using BlindDelivery.Core.Save;
using BlindDelivery.Services.Events;
using BlindDelivery.Services.Notifications;
using BlindDelivery.Services.Save;
using BlindDelivery.Tests.EditMode.Fakes;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// The M8 acceptance gates for notifications: the OS prompt is never fired by the boot or by
    /// scheduling â€” only the single explicit ask; scheduling while denied is rejected; the pending
    /// list survives a simulated app restart (a fresh service over the same store reschedules it);
    /// entries that expired while the app was closed are pruned, not resurrected.
    /// </summary>
    public class NotificationServiceTests
    {
        StubNotificationProvider provider;
        InMemorySaveStore store;
        EventBus bus;
        Func<DateTime> clock;
        int scheduledEvents;
        int cancelledEvents;
        int permissionEvents;

        [SetUp]
        public void SetUp()
        {
            provider = new StubNotificationProvider();
            store = new InMemorySaveStore();
            bus = new EventBus();
            clock = SetClock();
            // The runner can reuse the fixture object between tests, so every counter field is
            // reset here rather than relying on a fresh instance per test.
            scheduledEvents = 0;
            cancelledEvents = 0;
            permissionEvents = 0;
            bus.Subscribe<NotificationScheduled>(_ => scheduledEvents++);
            bus.Subscribe<NotificationCancelled>(_ => cancelledEvents++);
            bus.Subscribe<NotificationPermissionChanged>(_ => permissionEvents++);
        }

        static Func<DateTime> SetClock(DateTime moment = default)
        {
            DateTime fixedMoment = moment == default ? new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc) : moment;
            return () => fixedMoment;
        }

        NotificationService Build(Func<DateTime> nowUtc = null, InMemorySaveStore backing = null)
        {
            NotificationService service = new NotificationService(
                provider,
                bus,
                new SaveService(backing ?? store, bus),
                nowUtc ?? clock);
            service.InitializeAsync().Wait();
            return service;
        }

        [Test]
        public void BootReadsPermission_WithoutAnOsPrompt()
        {
            NotificationService service = Build();

            Assert.That(service.PermissionState, Is.EqualTo(NotificationPermission.Granted));
            Assert.That(provider.RequestCalls, Is.Zero, "boot only read the state; the actual ask never fired");
        }

        [Test]
        public void Schedule_WhileDenied_IsRejectedAndSkipsTheProvider()
        {
            provider.NextPermission = NotificationPermission.Denied;
            NotificationService service = Build();

            NotificationScheduleOutcome outcome = service.ScheduleAsync(
                "n1", "T", "B", "", clock().AddHours(1)).Result;

            Assert.That(outcome, Is.EqualTo(NotificationScheduleOutcome.Rejected));
            Assert.That(service.Pending, Is.Empty);
            Assert.That(provider.Scheduled, Is.Empty);
            Assert.That(scheduledEvents, Is.Zero);
        }

        [Test]
        public void Schedule_FutureTime_SchedulesPersistsAndPublishesTheFact()
        {
            NotificationService service = Build();

            NotificationScheduleOutcome outcome = service.ScheduleAsync(
                "n1", "You won!", "Coins", "rewards", clock().AddHours(1)).Result;

            Assert.That(outcome, Is.EqualTo(NotificationScheduleOutcome.Scheduled));
            Assert.That(service.Pending, Has.Count.EqualTo(1));
            Assert.That(service.Pending[0].FireAtUtc, Is.EqualTo(clock().AddHours(1)));
            Assert.That(provider.Scheduled.ContainsKey("n1"), Is.True);
            Assert.That(scheduledEvents, Is.EqualTo(1));
        }

        [Test]
        public void Schedule_SameId_ReplacesInsteadOfDoubling()
        {
            NotificationService service = Build();
            service.ScheduleAsync("n1", "T", "B", "", clock().AddHours(1)).Wait();

            NotificationScheduleOutcome second = service.ScheduleAsync(
                "n1", "Later", "B2", "", clock().AddHours(9)).Result;

            Assert.That(second, Is.EqualTo(NotificationScheduleOutcome.Replaced));
            Assert.That(service.Pending, Has.Count.EqualTo(1));
            Assert.That(service.Pending[0].FireAtUtc, Is.EqualTo(clock().AddHours(9)));
            Assert.That(provider.Scheduled.ContainsKey("n1"), Is.True);
            Assert.That(provider.Scheduled["n1"].FireAtUtc, Is.EqualTo(clock().AddHours(9)));
        }

        [Test]
        public void Schedule_PastFireTime_IsRejected()
        {
            NotificationService service = Build();

            NotificationScheduleOutcome outcome = service.ScheduleAsync(
                "n1", "T", "B", "", clock().AddSeconds(-1)).Result;

            Assert.That(outcome, Is.EqualTo(NotificationScheduleOutcome.Rejected));
            Assert.That(service.Pending, Is.Empty);
            Assert.That(scheduledEvents, Is.Zero);
        }

        [Test]
        public void Cancel_RemovesThePendingEntry_AndPublishesTheFact()
        {
            NotificationService service = Build();
            service.ScheduleAsync("n1", "T", "B", "", clock().AddHours(1)).Wait();

            service.CancelAsync("n1").Wait();

            Assert.That(service.Pending, Is.Empty);
            Assert.That(provider.Scheduled.ContainsKey("n1"), Is.False);
            Assert.That(cancelledEvents, Is.EqualTo(1));
        }

        [Test]
        public void CancelAll_WithNothingPending_PublishesNothing()
        {
            NotificationService service = Build();

            service.CancelAllAsync().Wait();

            Assert.That(cancelledEvents, Is.Zero);
        }

        [Test]
        public void PendingList_SurvivesARestart_AndIsRescheduled()
        {
            NotificationService first = Build();
            first.ScheduleAsync("n1", "T", "B", "", clock().AddHours(1)).Wait();
            first.ScheduleAsync("n2", "T", "B", "", clock().AddHours(3)).Wait();

            // A simulated app restart: fresh provider, fresh service, same store.
            provider = new StubNotificationProvider();
            NotificationService restarted = Build();

            Assert.That(restarted.Pending, Has.Count.EqualTo(2));
            Assert.That(provider.Scheduled.ContainsKey("n1"), Is.True);
            Assert.That(provider.Scheduled.ContainsKey("n2"), Is.True);
        }

        [Test]
        public void Restart_PrunesExpiredEntries_KeepsAndReschedulesTheFuture()
        {
            DateTime boot = clock();
            NotificationService first = Build();
            first.ScheduleAsync("expired", "T", "B", "", boot.AddHours(1)).Wait();
            first.ScheduleAsync("still-live", "T", "B", "", boot.AddHours(5)).Wait();

            // Time moved on: one entry was supposed to fire while the app was closed.
            clock = SetClock(boot.AddHours(3));
            provider = new StubNotificationProvider();
            NotificationService restarted = Build(nowUtc: clock);

            Assert.That(restarted.Pending.SelectIds(), Is.EqualTo(new[] { "still-live" }));
            Assert.That(provider.Scheduled.ContainsKey("still-live"), Is.True);
            Assert.That(provider.Scheduled.ContainsKey("expired"), Is.False);
        }

        [Test]
        public void Restart_WithRevokedPermission_KeepsTheListButClearsTheProvider()
        {
            NotificationService first = Build();
            first.ScheduleAsync("n1", "T", "B", "", clock().AddHours(1)).Wait();

            provider = new StubNotificationProvider { NextPermission = NotificationPermission.Denied };
            NotificationService restarted = Build();

            Assert.That(restarted.Pending, Has.Count.EqualTo(1), "the list is kept for when permission returns");
            Assert.That(provider.Scheduled, Is.Empty, "nothing stays armed against a denied permission");
        }

        [Test]
        public void RequestAuthorization_AnswerChange_FiresThePermissionFact()
        {
            // The OS had never been asked, so the boot read answers NotRequested; the explicit
            // ask is what grants â€” the state change is the fact.
            provider.NextPermission = NotificationPermission.NotRequested;
            NotificationService service = Build();

            NotificationPermission answered =
                ServiceAwait(service.RequestAuthorizationAsync());

            Assert.That(answered, Is.EqualTo(NotificationPermission.Granted));
            Assert.That(permissionEvents, Is.EqualTo(1));
            Assert.That(provider.RequestCalls, Is.EqualTo(1), "the os prompt was asked exactly here");
        }

        [Test]
        public void RequestAuthorization_NoStateChange_PublishesNothing()
        {
            NotificationService service = Build();

            ServiceAwait(service.RequestAuthorizationAsync());
            ServiceAwait(service.RequestAuthorizationAsync());

            Assert.That(permissionEvents, Is.Zero, "the state did not change; no event fires");
        }

        static T ServiceAwait<T>(System.Threading.Tasks.Task<T> task) => task.GetAwaiter().GetResult();
    }

    internal static class NotificationTestExtensions
    {
        public static string[] SelectIds(this System.Collections.Generic.IReadOnlyList<NotificationRequest> list)
        {
            string[] ids = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                ids[i] = list[i].id;
            }

            return ids;
        }
    }
}

