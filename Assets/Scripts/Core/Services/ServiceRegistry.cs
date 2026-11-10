using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.Services
{
    /// <summary>
    /// The service locator, kept intentionally dumb: it stores what the composition root
    /// constructed and hands it back by type, preserving registration order as the boot order.
    ///
    /// Manual injection, no container (architecture note, Decisions table). The rule is
    /// "Bootstrap constructs, everything else receives" — this is the one place allowed to look
    /// services up by type; gameplay and UI code take theirs through constructors.
    /// </summary>
    public sealed class ServiceRegistry
    {
        readonly List<IAppService> ordered = new List<IAppService>();
        readonly Dictionary<Type, IAppService> byType = new Dictionary<Type, IAppService>();
        readonly object gate = new object();

        public int Count => ordered.Count;

        /// <summary>Registration order — which is boot order.</summary>
        public IReadOnlyList<IAppService> Services => ordered;

        /// <summary>
        /// Registers a service under its concrete type and under every interface it implements, so
        /// callers can ask for the seam (<c>Get&lt;ISceneLoader&gt;()</c>) rather than the
        /// implementation.
        /// </summary>
        public void Register(IAppService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type concrete = service.GetType();
            lock (gate)
            {
                if (byType.ContainsKey(concrete))
                {
                    throw new InvalidOperationException(
                        $"A service of type {concrete.Name} is already registered. " +
                        "Registering twice is a wiring bug: the boot order would contain it twice.");
                }

                byType.Add(concrete, service);
                ordered.Add(service);

                foreach (Type contract in concrete.GetInterfaces())
                {
                    // Every interface the service implements is a usable seam: IAppService for the
                    // boot chain, and feature contracts such as ISceneLoader for the callers. The
                    // marker itself is skipped so "give me any service" is not a legal question.
                    if (contract == typeof(IAppService))
                    {
                        continue;
                    }

                    // First registration wins for a shared contract, so a game's override can be
                    // registered before the default and later registrations stay quiet.
                    if (!byType.ContainsKey(contract))
                    {
                        byType.Add(contract, service);
                    }
                }
            }
        }

        public T Get<T>() where T : class
        {
            if (TryGet<T>(out T found))
            {
                return found;
            }

            throw new InvalidOperationException(
                $"No service registered for {typeof(T).Name}. If it belongs to a later milestone, " +
                "the call site is running before that system exists.");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            lock (gate)
            {
                if (byType.TryGetValue(typeof(T), out IAppService found))
                {
                    service = (T)found;
                    return true;
                }
            }

            service = null;
            return false;
        }
    }
}
