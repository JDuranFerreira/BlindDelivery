namespace BlindDelivery.Core.Analytics
{
    /// <summary>One typed parameter of an analytics event. Kept as a struct so logging a
    /// gameplay event costs no allocations beyond the params array itself.</summary>
    public readonly struct AnalyticsParam
    {
        public AnalyticsParam(string key, object value)
        {
            Key = key;
            Value = value;
        }


        public string Key { get; }

        public object Value { get; }
    }


    /// <summary>
    /// The analytics seam (architecture note, Decisions table: "one-interface placeholder").
    /// Every call is consent-gated by the implementation: until <see cref="BlindDelivery.Core.Consent.ConsentState"/>'s
    /// decision is Granted, no provider is initialized and events are dropped, so a game can
    /// call LogEvent freely anywhere without its own gating.
    /// </summary>
    public interface IAnalyticsService : BlindDelivery.Core.Services.IAppService
    {
        /// <summary>True only while consent is granted and the underlying provider initialized.</summary>
        bool IsEnabled { get; }

        /// <summary>Master switch, e.g. for a debug build or a remote-config kill switch.</summary>
        void SetEnabled(bool enabled);

        /// <summary>Logs a named event. Dropped silently when not enabled.</summary>
        void LogEvent(string name, params AnalyticsParam[] parameters);
    }


    /// <summary>
    /// Event name constants. Naming them here (goals note system 9) keeps the dashboards and the
    /// code agreeing on spelling; ad-hoc strings leak typos into reports.
    /// </summary>
    public static class AnalyticsEventNames
    {
        public const string SessionStarted = "session_started";

        public const string LevelStarted = "level_started";
        public const string LevelCompleted = "level_completed";

        public const string LevelFailed = "level_failed";

        public const string TutorialStep = "tutorial_step";
        public const string AdViewed = "ad_viewed";

        public const string PurchaseCompleted = "purchase_completed";
        public const string ConsentDecided = "consent_decided";
    }
}


