using System;
using Template.Core.Ads;

namespace Template.Services.Ads
{
    /// <summary>
    /// The template's default ad provider: never fills. A real SDK (LevelPlay, AdMob, MAX)
    /// replaces it by implementing <see cref="IAdProvider"/> and being passed into
    /// <see cref="AdService"/> at the composition root — no other change. Its two knobs make it
    /// also the honest fake in tests: a scripted availability and a scripted outcome per show.
    /// </summary>
    public sealed class StubAdProvider : IAdProvider
    {
        Func<AdType, bool> ready;
        Func<AdType, AdShowOutcome> nextOutcome;

        public int InitializeCount { get; private set; }

        public bool IsInitialized { get; private set; }

        public AdType? LastShownType { get; private set; }

        public string LastShownTag { get; private set; }

        public StubAdProvider()
        {
            ready = _ => false;
            nextOutcome = _ => new AdShowOutcome(false, false);
        }

        /// <summary>Scripted availability, the no-fill default.</summary>
        public StubAdProvider WithReady(Func<AdType, bool> readyList)
        {
            ready = readyList;
            return this;
        }

        /// <summary>Scripted outcome per show attempt (shown, no-fill, reward granted...).</summary>
        public StubAdProvider WithOutcome(Func<AdType, AdShowOutcome> outcomeFactory)
        {
            nextOutcome = outcomeFactory;
            return this;
        }

        public void Initialize()
        {
            InitializeCount++;
            IsInitialized = true;
        }

        public bool IsReady(AdType type)
        {
            return IsInitialized && ready(type);
        }

        public void Show(AdType type, string tag, Action<AdShowOutcome> callback)
        {
            LastShownType = type;
            LastShownTag = tag;
            callback(nextOutcome(type));
        }
    }
}
