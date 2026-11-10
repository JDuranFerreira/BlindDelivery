using System;

namespace Template.Core.Events
{
    /// <summary>
    /// Cross-system notifications, typed. The contract lives in Core; the implementation lives
    /// in Services (Template.Services.Events.EventBus) so Core keeps zero Unity references.
    ///
    /// There is deliberately no static state and no Reset(): a boot constructs one bus and hands
    /// it to everything that publishes or listens, so a stale handler is a wiring bug the
    /// bootstrapper can be asked about instead of a silent listener surviving a play session.
    ///
    /// Dropping every subscription (Clear) is not on this interface: unsubscribing is each
    /// system's own job (OnDisable/OnDestroy), and a central clear is how objects learn to skip it.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent;

        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent;

        // <c>in</c> keeps the payload from being copied at the call site. Occasional events cost
        // nothing extra; high-frequency ones cost nothing at all.
        void Publish<TEvent>(in TEvent evt) where TEvent : struct, IEvent;
    }
}
