using System;
using NUnit.Framework;
using BlindDelivery.Core.Ads;
using BlindDelivery.Core.Save;
using BlindDelivery.Services.Ads;
using BlindDelivery.Services.Events;
using BlindDelivery.Services.Save;
using BlindDelivery.Tests.EditMode.Fakes;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// The M7 acceptance gates for ads: no-fill handled gracefully, a shown full-screen ad ducks
    /// audio and publishes its start/finish facts, the reward is granted only through the reward
    /// callback, the policy throttles interstitials, and remove-ads persists (a fresh service over
    /// the same store re-applies it without re-firing the event).
    /// </summary>
    public class AdServiceTests
    {
        AdPolicy policy;
        StubAdProvider provider;
        EventBus bus;
        SaveService save;
        FakeAudioService audio;
        int startedCount;
        int finishedCount;
        string lastFinishedFact;

        [SetUp]
        public void SetUp()
        {
            policy = new AdPolicy();
            provider = new StubAdProvider();
            bus = new EventBus();
            save = new SaveService(new InMemorySaveStore(), bus);
            audio = new FakeAudioService();
            // The runner can reuse the fixture object between tests, so every counter field is
            // reset here rather than relying on a fresh instance per test.
            startedCount = 0;
            finishedCount = 0;
            lastFinishedFact = null;
            bus.Subscribe<FullScreenAdStarted>(_ => startedCount++);
            bus.Subscribe<FullScreenAdFinished>(e =>
            {
                finishedCount++;
                lastFinishedFact = $"{e.Type}:{e.WasShown}";
            });
        }

        AdService BuildServer(Func<DateTime> clock = null)
        {
            AdService service = new AdService(provider, policy, bus, save, audio, clock);
            service.InitializeAsync().Wait();
            return service;
        }

        [Test]
        public void NoFill_IsGracefulResult_NotAnError()
        {
            // The provider is a stub and no SDK stepped in: everything reads NoFill, nothing is
            // announced, no sound path is touched.
            AdService ads = BuildServer();

            Assert.That(ads.ShowRewarded("extra_life", _ => { }), Is.EqualTo(AdShowResult.NoFill));
            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.NoFill));
            Assert.That(startedCount, Is.EqualTo(0));
            Assert.That(finishedCount, Is.EqualTo(0));
            Assert.That(audio.DuckCalls, Is.Empty);
        }

        [Test]
        public void RewardedShow_DucksAudio_PublishesFacts_AndGrantsRewardOnlyViaCallback()
        {
            provider.WithReady(type => type == AdType.Rewarded)
                .WithOutcome(_ => new AdShowOutcome(true, true));
            AdService ads = BuildServer();

            AdReward received = default;
            AdShowResult result = ads.ShowRewarded("extra_life", reward => received = reward);

            Assert.That(result, Is.EqualTo(AdShowResult.Shown));
            Assert.That(received.EarnedReward, Is.True, "the reward lives only in the callback");
            Assert.That(received.Tag, Is.EqualTo("extra_life"));
            Assert.That(audio.DuckCalls, Is.EqualTo(new[] { "0:600", "1:0" }));
            Assert.That(startedCount, Is.EqualTo(1));
            Assert.That(finishedCount, Is.EqualTo(1));
            Assert.That(lastFinishedFact, Is.EqualTo("Rewarded:True"));
        }

        [Test]
        public void RewardedNotShown_CallbackStillFires_WithNoReward()
        {
            provider.WithReady(_ => true)
                .WithOutcome(_ => new AdShowOutcome(false, false));
            AdService ads = BuildServer();

            int callbacks = 0;
            bool earned = true;
            ads.ShowRewarded("extra_life", reward =>
            {
                callbacks++;
                earned = reward.EarnedReward;
            });

            Assert.That(callbacks, Is.EqualTo(1), "the game hears the attempt ended and grants nothing");
            Assert.That(earned, Is.False);
            Assert.That(lastFinishedFact, Is.EqualTo("Rewarded:False"));
        }

        [Test]
        public void InterstitialCooldown_BlocksSecondInsideInterval_AllowsAfter()
        {
            policy.InterstitialMinIntervalSeconds = 60;
            provider.WithReady(type => type == AdType.Interstitial)
                .WithOutcome(_ => new AdShowOutcome(true, false));

            DateTime baseUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            double elapsedSeconds = 0;
            AdService ads = BuildServer(() => baseUtc.AddSeconds(elapsedSeconds));

            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.Shown));
            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.BlockedByPolicy));

            elapsedSeconds = 61;
            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.Shown));
        }

        [Test]
        public void InterstitialMaxPerSession_BlocksRegardlessOfCooldown()
        {
            policy.InterstitialMinIntervalSeconds = 0;
            policy.InterstitialMaxPerSession = 1;
            provider.WithReady(type => type == AdType.Interstitial)
                .WithOutcome(_ => new AdShowOutcome(true, false));
            AdService ads = BuildServer();

            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.Shown));
            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.BlockedByPolicy));
        }

        [Test]
        public void RemoveAds_PersistsImmediately_SurvivesFreshService_AndBlocksInterstitials()
        {
            provider.WithReady(_ => true).WithOutcome(_ => new AdShowOutcome(true, false));

            AdService ads = BuildServer();

            int removedEvents = 0;
            ads.OnInterstitialsRemoved += () => removedEvents++;
            ads.PurchaseRemoveAds();

            Assert.That(ads.AreInterstitialsRemoved, Is.True);
            Assert.That(removedEvents, Is.EqualTo(1));
            Assert.That(save.Exists("ads"), Is.True, "an owned purchase must not depend on the next save tick");
            Assert.That(ads.ShowInterstitial(), Is.EqualTo(AdShowResult.RemovedByPurchase));
        }

        [Test]
        public void RestoredRemoveAds_AppliesSilently_AndBlocksBanners()
        {
            provider.WithReady(_ => true);
            AdService first = BuildServer();
            first.PurchaseRemoveAds();

            // The "crash": a fresh service over the same store boots with the ownership applied.
            AdService reborn = new AdService(provider, policy, bus, save, audio);
            reborn.InitializeAsync().Wait();
            int removedEvents = 0;
            reborn.OnInterstitialsRemoved += () => removedEvents++;

            Assert.That(reborn.AreInterstitialsRemoved, Is.True, "the purchase survived the restart");
            Assert.That(removedEvents, Is.EqualTo(0), "boot restore re-fires nothing");
            Assert.That(reborn.ShowInterstitial(), Is.EqualTo(AdShowResult.RemovedByPurchase));

            reborn.ShowBanner();
            Assert.That(provider.LastShownType, Is.EqualTo(null), "banners go dark once remove-ads owns the session");
        }
    }
}
