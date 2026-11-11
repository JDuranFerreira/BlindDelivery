using System;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Ads;
using BlindDelivery.Core.Audio;
using BlindDelivery.Core.Events;
using BlindDelivery.Core.Save;
using BlindDelivery.Core.Services;
using UnityEngine;

namespace BlindDelivery.Services.Ads
{
    /// <summary>Save document for remove-ads. One field, JsonUtility-serializable.</summary>
    public class AdsDocument
    {
        public bool adsRemoved;
    }

    /// <summary>
    /// The ad system over any <see cref="IAdProvider"/> (goals note system 10). The stub provider
    /// never fills, so the BlindDelivery boots ad-free with zero SDK packages; a game swaps in
    /// LevelPlay/AdMob/MAX at the composition root without touching gameplay code.
    ///
    /// Order of a show decision: remove-ads (RemovedByPurchase) first â€” an owned purchase may not
    /// be contradicted by anything else; then provider readiness (NoFill is an expected graceful
    /// outcome, not an error); then the frequency policy (BlockedByPolicy). Only after every gate
    /// passes does the provider get the show call.
    ///
    /// Full-screen shows duck the audio directly (<see cref="IAudioService.Duck"/>) and announce
    /// themselves on the bus with <see cref="FullScreenAdStarted"/>, so gameplay pauses itself;
    /// <see cref="FullScreenAdFinished"/> ends both. Rewards are granted only through the game's
    /// callback inside the provider's outcome callback â€” never a return value.
    ///
    /// Remove-ads persists immediately through <see cref="ISaveService"/> â€” an owned purchase must
    /// not depend on the next save tick â€” and re-applies at boot through
    /// <see cref="RestoreAdsRemoved"/> without re-firing the purchase event.
    /// </summary>
    public sealed class AdService : IAdService, IAppService
    {
        const string SaveKeyPath = "ads";
        const string InterstitialTag = "";
        const string BannerTag = "banner";

        /// <summary>
        /// How long a duck holds before audio recovers on its own (a safety net for a real SDK
        /// whose show callback is invoked asynchronously, possibly late or never). Every finish
        /// path is followed by a Duck(1, 0), which recovers immediately.
        /// </summary>
        const float AdDuckHoldSeconds = 600f;

        readonly IAdProvider provider;
        readonly AdPolicy policy;
        readonly IEventBus bus;
        readonly ISaveService save;
        readonly IAudioService audio;
        readonly Func<DateTime> nowUtc;

        bool adsRemoved;
        DateTime lastInterstitialShownUtc;
        int interstitialShowsThisSession;

        public AdService(
            IAdProvider provider,
            AdPolicy policy,
            IEventBus bus,
            ISaveService save,
            IAudioService audio,
            Func<DateTime> nowUtc = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            this.audio = audio ?? throw new ArgumentNullException(nameof(audio));
            this.nowUtc = nowUtc ?? (Func<DateTime>)(() => DateTime.UtcNow);
        }

        public string Name => "Ads";

        public bool AreInterstitialsRemoved => adsRemoved;

        /// <summary>Raised once when remove-ads turns on in this session â€” never at boot restore.</summary>
        public event Action OnInterstitialsRemoved;

        public Task InitializeAsync(CancellationToken token = default)
        {
            provider.Initialize();

            // Session-scoped bookkeeping resets here; the persisted remove-ads flag survives.
            lastInterstitialShownUtc = default;
            interstitialShowsThisSession = 0;

            try
            {
                AdsDocument document = save.LoadAsync<AdsDocument>(SaveKeyPath).GetAwaiter().GetResult();
                if (document != null && document.adsRemoved)
                {
                    adsRemoved = true;
                }
            }
            catch (Exception error)
            {
                // A corrupted monetization document degrades to "ads serve again" â€” it must never
                // block boot, and it is also the cheapest possible mistake against the player.
                Debug.LogWarning($"[AdService] Remove-ads document did not load ({error.Message}); serving ads as not-removed.");
            }

            return Task.CompletedTask;
        }

        /// <summary>Re-applies the persisted remove-ads flag silently so owned UI reads it at boot.</summary>
        public void RestoreAdsRemoved()
        {
            // Reads the live document, not a cached field: the freshest arriving state wins.
            try
            {
                AdsDocument document = save.LoadAsync<AdsDocument>(SaveKeyPath).GetAwaiter().GetResult();
                adsRemoved = document != null && document.adsRemoved;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[AdService] RestoreAdsRemoved fell back to not-removed ({error.Message}).");
                adsRemoved = false;
            }
        }

        public void ShowBanner()
        {
            if (adsRemoved || !provider.IsReady(AdType.Banner))
            {
                return;
            }

            // Banners are not full-screen events: no duck, no boot announcement, fire-and-forget.
            provider.Show(AdType.Banner, BannerTag, _ => { });
        }

        public void HideBanner()
        {
            // The banner seam is provider-owned once shown; the policy layer only decides whether
            // a banner may start. Hiding is the provider's business â€” the stub ignores it and a
            // real SDK maps it onto its own hide call.
        }

        public bool IsRewardedReady => provider.IsReady(AdType.Rewarded);

        public bool IsInterstitialReady => !adsRemoved && provider.IsReady(AdType.Interstitial);

        public AdShowResult ShowRewarded(string rewardTag, Action<AdReward> rewardCallback)
        {
            // Remove-ads never touch rewarded content: it is gameplay the player asked for.
            if (!provider.IsReady(AdType.Rewarded))
            {
                return AdShowResult.NoFill;
            }

            AdShowResult result = AdShowResult.Shown;
            string attemptTag = rewardTag ?? InterstitialTag;

            bus.Publish(new FullScreenAdStarted(AdType.Rewarded, attemptTag));
            audio.Duck(0f, AdDuckHoldSeconds);
            provider.Show(AdType.Rewarded, attemptTag, outcome =>
            {
                FinishFullScreen(AdType.Rewarded, attemptTag, outcome.WasShown);
                result = outcome.WasShown ? AdShowResult.Shown : AdShowResult.Failed;

                // Exactly one invocation from the provider's outcome decides the reward. When the
                // ad was not rewarded, the callback still fires with EarnedReward = false, so the
                // game learns the ad ended and grants nothing.
                rewardCallback?.Invoke(new AdReward(attemptTag, outcome.EarnedReward));
            });

            return result;
        }

        public AdShowResult ShowInterstitial()
        {
            if (adsRemoved)
            {
                return AdShowResult.RemovedByPurchase;
            }

            if (!provider.IsReady(AdType.Interstitial))
            {
                return AdShowResult.NoFill;
            }

            if (!policy.InterstitialAllowed(lastInterstitialShownUtc, interstitialShowsThisSession, nowUtc()))
            {
                return AdShowResult.BlockedByPolicy;
            }

            AdShowResult result = AdShowResult.Shown;

            bus.Publish(new FullScreenAdStarted(AdType.Interstitial, InterstitialTag));
            audio.Duck(0f, AdDuckHoldSeconds);
            provider.Show(AdType.Interstitial, InterstitialTag, outcome =>
            {
                if (outcome.WasShown)
                {
                    lastInterstitialShownUtc = nowUtc();
                    interstitialShowsThisSession++;
                }

                FinishFullScreen(AdType.Interstitial, InterstitialTag, outcome.WasShown);
                result = outcome.WasShown ? AdShowResult.Shown : AdShowResult.Failed;
            });

            return result;
        }

        public void PurchaseRemoveAds()
        {
            if (adsRemoved)
            {
                return;
            }

            adsRemoved = true;
            save.SaveAsync(SaveKeyPath, new AdsDocument { adsRemoved = true }).GetAwaiter().GetResult();
            OnInterstitialsRemoved?.Invoke();
        }

        /// <summary>
        /// Ends every full-screen show path the same way, whether the provider reported a real ad
        /// or a failure: the finished fact lands on the bus so gameplay unpauses, and the audio duck
        /// is released immediately (Duck back to full with no hold).
        /// </summary>
        void FinishFullScreen(AdType type, string tag, bool shown)
        {
            audio.Duck(1f, 0f);
            bus.Publish(new FullScreenAdFinished(type, tag, shown));
        }
    }
}


