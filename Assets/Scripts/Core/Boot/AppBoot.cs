using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Events;
using Template.Core.Services;

namespace Template.Core.Boot
{
    /// <summary>
    /// Runs the services in registration order and reports through the bus. Pure C# on purpose:
    /// the boot chain — the thing most likely to break silently — is covered by EditMode tests
    /// with fake services, no scene and no Unity player loop involved.
    ///
    /// Failure policy: the first exception stops the chain. A save service that cannot load means
    /// the settings and audio services downstream would be initializing against state that does
    /// not exist, and starting them anyway turns one clear error into three confusing ones.
    /// </summary>
    public sealed class AppBoot
    {
        readonly ServiceRegistry registry;
        readonly IEventBus bus;
        bool hasRun;

        public AppBoot(ServiceRegistry registry, IEventBus bus)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task<BootResult> RunAsync(CancellationToken token = default)
        {
            if (hasRun)
            {
                throw new InvalidOperationException(
                    "AppBoot already ran. Boot is a one-shot: a second pass would re-initialize " +
                    "services that are already live.");
            }

            hasRun = true;

            IReadOnlyList<IAppService> services = registry.Services;
            bus.Publish(new AppBootStarted(services.Count));

            for (int i = 0; i < services.Count; i++)
            {
                IAppService service = services[i];

                if (token.IsCancellationRequested)
                {
                    return BootResult.Cancelled;
                }

                try
                {
                    await service.InitializeAsync(token);
                }
                catch (OperationCanceledException)
                {
                    return BootResult.Cancelled;
                }
                catch (Exception error)
                {
                    bus.Publish(new AppBootFailed(service.Name, error.Message));
                    return BootResult.Failure(service.Name, error);
                }

                bus.Publish(new ServiceInitialized(service.Name, i));
            }

            bus.Publish(new AppBootCompleted(services.Count));
            return BootResult.Success;
        }
    }
}
