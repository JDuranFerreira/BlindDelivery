using System;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Analytics;
using Template.Core.Consent;
using Template.Core.Events;
using Template.Core.Settings;


namespace Template.Services.Consent
{
    /// <summary>
    /// The consent gate. Loads the stored decision at boot (Unknown on first launch), persists
    /// Grant/Deny through the prefs store (IPrefsStore — taste and legality, not progress), and
    /// raises both its own event and a <see cref="ConsentChanged"/> struct on the bus, so screens
    /// listening on the bus react exactly like services listening on the interface event.
    /// </summary>
    public sealed class ConsentService : IConsentService, Template.Core.Services.IAppService
    {
        const string ConsentKey = "template.consent.state";

        readonly IPrefsStore prefs;
        readonly IEventBus bus;

        ConsentState state = ConsentState.Unknown;

        public ConsentService(IPrefsStore prefs, IEventBus bus)
        {
            this.prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
            this.bus = bus;
        }

        public string Name => "Consent";

        public event Action<ConsentState> OnConsentChanged;

        public ConsentState State => state;

        public Task InitializeAsync(CancellationToken token = default)
        {
            if (prefs.TryGetInt(ConsentKey, out int stored))
            {
                state = stored == (int)ConsentState.Granted
                    ? ConsentState.Granted
                    : stored == (int)ConsentState.Denied
                        ? ConsentState.Denied
                        : ConsentState.Unknown;
            }

            return Task.CompletedTask;
        }

        public void Grant() => SetState(ConsentState.Granted);

        public void Deny() => SetState(ConsentState.Denied);

        void SetState(ConsentState value)
        {
            if (state == value)
            {
                return;
            }

            state = value;
            prefs.SetInt(ConsentKey, (int)value);
            prefs.Save();
            OnConsentChanged?.Invoke(value);
            bus?.Publish(new ConsentChanged(value));
        }
    }
}
