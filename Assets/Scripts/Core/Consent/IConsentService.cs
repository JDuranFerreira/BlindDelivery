using System;


namespace Template.Core.Consent
{
    /// <summary>The GDPR/ATT tracking decision. Unknown is the first-run state: nothing
    /// analytics-shaped may initialize while it lasts (architecture note, M6).</summary>
    public enum ConsentState
    {
        Unknown = 0,
        Granted = 1,
        Denied = 2
    }


    /// <summary>
    /// Whether the player allows analytics/ads/data collection. Every external-data provider
    /// (analytics, later M7 ads) reads it before its first initialize or network call. The
    /// decision persists across launches via an IPrefsStore, so returning players keep it.
    /// </summary>
    public interface IConsentService : Template.Core.Services.IAppService
    {
        /// <summary>Raised after every change of <see cref="State"/>.</summary>
        event Action<ConsentState> OnConsentChanged;


        ConsentState State { get;


        }

        /// <summary>Records the player's permission and raises <see cref="OnConsentChanged"/>.</summary>
        void Grant();


        /// <summary>Records the player's refusal and raises <see cref="OnConsentChanged"/>.</summary>
        void Deny();
    }
}
