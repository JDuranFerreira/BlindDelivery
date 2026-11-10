namespace Template.Core.Analytics
{
    /// <summary>
    /// The concrete provider behind <see cref="IAnalyticsService"/> (Firebase Analytics /
    /// Crashlytics, Unity Analytics, or any other). The template ships the seam only — a game
    /// adds the implementation, so no analytics package sits in the template's dependency list.
    ///
    /// The service calls <see cref="Initialize"/> only after consent is granted, which is the
    /// law- and policy-relevant moment; providers typically start their network chatter in
    /// their SDK init, so the consent gate lives here and not at event time.
    /// </summary>
    public interface IAnalyticsSink
    {
        /// <summary>True from the first successful <see cref="Initialize"/> until <see cref="Shutdown"/>.</summary>
        bool IsInitialized { get; }

        /// <summary>Starts the provider. Implementations must tolerate being called again.</summary>
        void Initialize();

        /// <summary>Forwards one event. Called only while the service is enabled.</summary>
        void LogEvent(string name, AnalyticsParam[] parameters);

        /// <summary>Stops the provider, e.g. when consent is revoked mid-session.</summary>
        void Shutdown();
    }
}
