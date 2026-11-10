using System;
using System.Collections.Generic;
using Template.Core.Events;

namespace Template.Services.Events
{
    /// <summary>
    /// The bus. One instance, constructed by the composition root and handed to everything that
    /// publishes or listens. An instance rather than a static generic: play-mode reload options
    /// can keep statics alive between sessions, and a static handler list then needs a reset path
    /// plus the discipline to call it. An instance needs neither.
    ///
    /// The price is one dictionary lookup per publish, on a path that fires a few dozen times a
    /// frame at worst. That is not the frame budget's problem.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        // Delegate rather than List<Action<T>>: a multicast delegate is immutable, so Publish reads
        // a snapshot. That is what makes it safe for a handler to subscribe or unsubscribe during a
        // publish — the running invocation list is not the one being replaced.
        readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type key = typeof(TEvent);
            handlers[key] = handlers.TryGetValue(key, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type key = typeof(TEvent);
            if (!handlers.TryGetValue(key, out Delegate existing))
            {
                return;
            }

            Delegate remaining = Delegate.Remove(existing, handler);

            // Drop the key rather than leaving a null behind, so publishing a type nobody listens
            // to costs one failed lookup and nothing else.
            if (remaining == null)
            {
                handlers.Remove(key);
                return;
            }

            handlers[key] = remaining;
        }

        public void Publish<TEvent>(in TEvent evt) where TEvent : struct, IEvent
        {
            if (handlers.TryGetValue(typeof(TEvent), out Delegate existing))
            {
                ((Action<TEvent>)existing).Invoke(evt);
            }
        }

        /// <summary>
        /// Drops every subscription. For teardown and for tests that want to prove a bus is empty.
        /// Not a substitute for unsubscribing — see <see cref="IEventBus"/>.
        /// </summary>
        public void Clear() => handlers.Clear();
    }
}
