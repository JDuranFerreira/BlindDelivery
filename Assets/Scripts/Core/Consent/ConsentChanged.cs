using Template.Core.Events;


namespace Template.Core.Consent
{
    /// <summary>
    /// Published on the bus after every change of the consent decision, so screens listening on
    /// the bus react exactly like services listening on <see cref="IConsentService.OnConsentChanged"/>.
    /// </summary>
    public readonly struct ConsentChanged : IEvent
    {
        public ConsentChanged(ConsentState state)
        {
            State = state;
        }

        public ConsentState State { get; }
    }
}
