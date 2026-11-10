using System;

namespace Template.Core.Ads
{
    /// <summary>
    /// Frequency caps and cooldowns for ad shows (goals note system 10). Pure data + one pure
    /// decision method, so the gate runs in tests with a scripted clock instead of wall time.
    /// Sessions here mean one boot of the game; counts reset on Initialize.
    /// </summary>
    public sealed class AdPolicy
    {
        /// <summary>Minimum seconds between two shows of the same interstitial-style type. 0 disables the cooldown.</summary>
        public float InterstitialMinIntervalSeconds { get; set; } = 60f;

        /// <summary>Maximum interstitial shows per session; after the cap the type reads BlockedByPolicy. 0 disables.</summary>
        public int InterstitialMaxPerSession { get; set; } = 0;

        /// <summary>
        /// True when an interstitial show is allowed at <paramref name="now"/>, given when it was
        /// last shown and how many times this session. Rewards are gameplay content, not
        /// interruptions — the policy never throttles them.
        /// </summary>
        public bool InterstitialAllowed(DateTime lastShownUtc, int showsThisSession, DateTime nowUtc)
        {
            if (InterstitialMaxPerSession > 0 && showsThisSession >= InterstitialMaxPerSession)
            {
                return false;
            }

            if (lastShownUtc == default || InterstitialMinIntervalSeconds <= 0f)
            {
                return true;
            }

            return (nowUtc - lastShownUtc).TotalSeconds >= InterstitialMinIntervalSeconds;
        }
    }
}
