using Template.Core.Events;

namespace Template.Core.Ads
{
    /// <summary>The three ad shapes the template supports (goals note system 10).</summary>
    public enum AdType
    {
        Interstitial,
        Rewarded,
        Banner
    }

    /// <summary>Why a show attempt did not happen. No-fill is an expected outcome, not an error.</summary>
    public enum AdFailureReason
    {
        /// <summary>The provider had no ad loaded — the game must fall back gracefully.</summary>
        NoFill,
        /// <summary>The frequency cap or cooldown said not yet.</summary>
        BlockedByPolicy,
        /// <summary>Remove-ads was purchased; interstitials and banners are gone.</summary>
        RemovedByPurchase,
        /// <summary>Anything else (init failed, provider error, ads disabled).</summary>
        Failed
    }

    /// <summary>
    /// What a show call ended with. Rewarded rewards are never a return value here — they arrive
    /// through the callback the game passed to <see cref="IAdService.ShowRewarded"/>, which is the
    /// provider's reward callback itself (goals note: grant rewards only in the reward callback).
    /// </summary>
    public enum AdShowResult
    {
        Shown,
        NoFill,
        BlockedByPolicy,
        RemovedByPurchase,
        Failed
    }

    /// <summary>The reward payload handed to the game's callback. Only EarnedReward matters.</summary>
    public readonly struct AdReward
    {
        public AdReward(string tag, bool earnedReward)
        {
            Tag = tag;
            EarnedReward = earnedReward;
        }

        public string Tag { get; }

        public bool EarnedReward { get; }
    }

    /// <summary>
    /// The ad seam (goals note system 10: monetization behind interfaces so providers can be
    /// swapped — LevelPlay, AdMob, AppLovin). The template's provider is a stub out of the box; a
    /// concrete SDK slots in as an <see cref="IAdProvider"/> without touching gameplay code.
    /// Full-screen ads pause audio and game time: the service ducks audio directly and announces
    /// the show on the bus with <see cref="FullScreenAdStarted"/>/<see cref="FullScreenAdFinished"/>
    /// structs, which gameplay listens to for its own pause.
    /// </summary>
    public interface IAdService : Template.Core.Services.IAppService
    {
        /// <summary>True after RemoveAds; interstitials and banners go dark.</summary>
        bool AreInterstitialsRemoved { get; }

        /// <summary>Raised once when remove-ads turns on, so owned UI updates.</summary>
        event System.Action OnInterstitialsRemoved;

        void ShowBanner();

        void HideBanner();

        /// <summary>True only when the provider reports a rewarded ad loaded and ready right now.</summary>
        bool IsRewardedReady { get; }

        bool IsInterstitialReady { get; }

        /// <summary>
        /// Shows a rewarded ad. <paramref name="rewardTag"/> is a free label (e.g. "extra_life")
        /// echoed back in the reward and events, so analytics can tell offers apart.
        /// </summary>
        AdShowResult ShowRewarded(string rewardTag, System.Action<AdReward> rewardCallback);

        /// <summary>Shows an interstitial, subject to the frequency caps and cooldowns.</summary>
        AdShowResult ShowInterstitial();

        /// <summary>Persists immediately — an owned purchase must not depend on the next save tick.</summary>
        void PurchaseRemoveAds();

        /// <summary>Re-applies a restored remove-ads flag at boot without re-firing the purchase event.</summary>
        void RestoreAdsRemoved();
    }

    /// <summary>Bus struct (past-tense fact, no object refs) announcing a full-screen ad starting.</summary>
    public readonly struct FullScreenAdStarted : IEvent
    {
        public FullScreenAdStarted(AdType type, string tag)
        {
            Type = type;
            ShowTag = tag;
        }

        public AdType Type { get; }

        public string ShowTag { get; }
    }

    /// <summary>Bus struct announcing how a full-screen ad attempt ended.</summary>
    public readonly struct FullScreenAdFinished : IEvent
    {
        public FullScreenAdFinished(AdType type, string tag, bool shown)
        {
            Type = type;
            ShowTag = tag;
            WasShown = shown;
        }

        public AdType Type { get; }

        public string ShowTag { get; }

        public bool WasShown { get; }
    }

    /// <summary>
    /// The provider seam a concrete ad SDK implements (goals note #10: one concrete impl behind
    /// the interface; the default here is the stub, zero package dependency). Each Show call
    /// completes through exactly one callback — synchronously under the stub and in tests,
    /// asynchronously under a real SDK. Availability false is the normal no-fill state.
    /// </summary>
    public interface IAdProvider
    {
        void Initialize();

        /// <summary>True when that ad type could be shown right now.</summary>
        bool IsReady(AdType type);

        /// <summary>Attempts a show. Exactly one callback invocation decides the attempt; never throws to the caller.</summary>
        void Show(AdType type, string tag, System.Action<AdShowOutcome> callback);
    }

    /// <summary>Outcome the SDK hands back per show attempt.</summary>
    public readonly struct AdShowOutcome
    {
        public AdShowOutcome(bool shown, bool earnedReward)
        {
            WasShown = shown;
            EarnedReward = earnedReward;
        }

        public bool WasShown { get; }

        public bool EarnedReward { get; }
    }
}
