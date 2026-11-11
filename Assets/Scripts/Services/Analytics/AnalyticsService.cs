using System;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Analytics;
using BlindDelivery.Core.Consent;
using BlindDelivery.Core.Services;

namespace BlindDelivery.Services.Analytics
{
    /// <summary>
    /// The consent gate over the analytics provider (goals note system 9: never initialize
    /// analytics before consent is known). Games call <see cref="LogEvent"/> freely anywhere;
    /// the service drops everything until the player has granted consent, the sink has been
    /// initialized behind that grant, and the master switch is on.
    /// </summary>
    public sealed class AnalyticsService : IAnalyticsService, IAppService
    {
        readonly IConsentService consent;
        readonly IAnalyticsSink sink;
        bool masterEnabled = true;

        public AnalyticsService(IConsentService consent, IAnalyticsSink sink = null)
        {
            this.consent = consent ?? throw new ArgumentNullException(nameof(consent));
            this.sink = sink;
        }

        public string Name => "Analytics";

        /// <summary>
        /// True only while consent is granted, the provider is initialized, and the master switch
        /// is on. With no sink registered (the BlindDelivery default) nothing can ever be enabled.
        /// </summary>
        public bool IsEnabled =>
            consent.State == ConsentState.Granted
            && masterEnabled
            && sink != null
            && sink.IsInitialized;

        public Task InitializeAsync(CancellationToken token = default)
        {
            consent.OnConsentChanged += OnConsentChanged;

            // Returning players already decided last session: bring the provider up now.
            if (consent.State == ConsentState.Granted)
            {
                InitializeSink();
            }

            return Task.CompletedTask;
        }

        /// <summary>Master switch, e.g. for a debug build or a remote-config kill switch.</summary>
        public void SetEnabled(bool enabled)
        {
            masterEnabled = enabled;

            // The switch Off must also silence the provider itself, not just gate events.
            if (!enabled && sink != null && sink.IsInitialized)
            {
                sink.Shutdown();
            }
            // Switching back On must bring the provider back, but only behind the same consent
            // gate a fresh boot would take â€” never without consent.
            if (enabled && masterEnabled && consent.State == ConsentState.Granted)
            {
                InitializeSink();
            }
        }

        public void LogEvent(string name, params AnalyticsParam[] parameters)
        {
            if (!IsEnabled)
            {
                return;
            }

            sink.LogEvent(name, parameters);
        }

        void OnConsentChanged(ConsentState state)
        {
            if (sink == null)
            {
                return;
            }

            if (state == ConsentState.Granted)
            {
                // SetEnabled(false) shuts the sink down; consent granted brings it back only if
                // the master switch is on. Shutdown bumping IsInitialized off makes this path
                // re-run Initialize through the same gate a fresh boot would take.
                if (masterEnabled)
                {
                    InitializeSink();
                }

                return;
            }

            // Unknown never happens after a decision, but revoking to Denied mid-session must
            // stop the provider immediately, not merely drop events.
            if (state == ConsentState.Denied && sink.IsInitialized)
            {
                sink.Shutdown();
            }
        }

        void InitializeSink()
        {
            if (sink != null && !sink.IsInitialized)
            {
                sink.Initialize();
            }
        }
    }
}


