using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using BlindDelivery.Core.GameServices;
using BlindDelivery.Services.GameServices;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// The M7 acceptance gates for game services: a denied sign-in leaves the game fully playable
    /// (reports queue, no leaderboard UI), a signed-in state flushes the queue through the mapped
    /// platform ids, an unmapped key is dropped rather than guessed, and failed sends stay queued.
    /// </summary>
    public class GameServicesServiceTests
    {
        StubGameServicesProvider provider;
        PlatformIdMap platformIds;
        GameServicesService services;

        PlatformIdMap Map(bool withBoard = true)
        {
            Dictionary<string, string> keys = new Dictionary<string, string>();
            if (withBoard)
            {
                keys["daily_best"] = "cb_daily";
            }

            return new PlatformIdMap().WithPlatform("android", keys);
        }

        [SetUp]
        public void SetUp()
        {
            provider = new StubGameServicesProvider();
            platformIds = Map();
            services = new GameServicesService(provider, platformIds);
            services.InitializeAsync().Wait();
        }

        [Test]
        public void DeniedSignIn_KeepsTheGamePlayable_QueueIntact_LeaderboardSilent()
        {
            provider.WithSignIn(() => false);

            services.ReportScore("daily_best", 120);
            services.UnlockAchievement("first_win");
            services.ShowLeaderboard("daily_best");
            bool signedIn = services.SignInAsync().Result;
            int flushed = services.FlushQueueAsync().Result;

            Assert.That(signedIn, Is.False);
            Assert.That(provider.Sent, Is.Empty, "denied sign-in sends nothing");
            Assert.That(provider.LeaderboardsShown, Is.Empty, "the platform UI cannot open offline");
            Assert.That(services.PendingQueueCount, Is.EqualTo(2), "reports keep waiting for the next sign-in");
            Assert.That(flushed, Is.EqualTo(0));
        }

        [Test]
        public void SignIn_FlushesQueue_ThroughMappedPlatformIds()
        {
            services.ReportScore("daily_best", 120);
            services.UnlockAchievement("first_win"); // not in the platform map: dropped at flush

            Assert.That(services.SignInAsync().Result, Is.True);

            Assert.That(provider.Sent.Count, Is.EqualTo(1));
            Assert.That(provider.LastSentItem.Key, Is.EqualTo("cb_daily"), "the map translated the stable key");
            Assert.That(provider.LastSentItem.Value, Is.EqualTo(120L));
            Assert.That(services.PendingQueueCount, Is.EqualTo(0), "the unmapped key was dropped, not guessed");
        }

        [Test]
        public void FailedSend_StaysQueued_ForTheNextFlush()
        {
            provider.WithSendResult(false);

            Assert.That(services.SignInAsync().Result, Is.True);
            services.ReportScore("daily_best", 120);
            Assert.That(services.FlushQueueAsync().Result, Is.EqualTo(0));
            Assert.That(services.PendingQueueCount, Is.EqualTo(1));
            Assert.That(provider.Sent, Is.Empty);
        }

        [Test]
        public void SignedInDirectReport_SendsImmediately_WhenMapped()
        {
            Assert.That(services.SignInAsync().Result, Is.True);

            services.UnlockAchievement("first_win"); // still unmapped: skipped by config
            services.ReportScore("daily_best", 55);

            Assert.That(provider.Sent.Count, Is.EqualTo(1));
            Assert.That(provider.Sent[0].Key, Is.EqualTo("cb_daily"));
            Assert.That(services.PendingQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void SignedInLeaderboard_UsesPlatformId_AndPassesTheEmptyKeyThrough()
        {
            Assert.That(services.SignInAsync().Result, Is.True);

            services.ShowLeaderboard("daily_best");
            services.ShowLeaderboard();
            services.ShowLeaderboard("unmapped_board");

            Assert.That(provider.LeaderboardsShown, Is.EqualTo(new[] { "cb_daily", "" }));
        }

        [Test]
        public void NoProvider_FullyPlayable_ReportingQueuesNotThrows()
        {
            GameServicesService providerless = new GameServicesService(null, new PlatformIdMap());
            providerless.InitializeAsync().Wait();

            providerless.ReportScore("daily_best", 120);
            providerless.UnlockAchievement("first_win");
            Task<bool> t = providerless.SignInAsync();
            t.Wait();
            bool signedIn = t.Result;
            Task<int> f = providerless.FlushQueueAsync();
            f.Wait();
            int sent = f.Result;

            Assert.That(signedIn, Is.False, "no provider is a platform quality, not an error");
            Assert.That(sent, Is.EqualTo(0));
            Assert.That(providerless.PendingQueueCount, Is.EqualTo(2), "reports wait for a wired provider");
        }
    }
}
