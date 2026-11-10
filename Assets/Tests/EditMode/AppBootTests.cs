using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Boot;
using Template.Core.Events;
using Template.Core.Services;
using Template.Services.Events;
using Template.Tests.EditMode.Fakes;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The boot chain is the one piece of M1 whose failure would be silent in a build: services
    /// out of order, or a failed service that lets the rest start anyway. These tests pin both.
    /// </summary>
    public class AppBootTests
    {
        ServiceRegistry registry;
        EventBus bus;
        List<string> initialized;
        List<string> events;

        [SetUp]
        public void SetUp()
        {
            registry = new ServiceRegistry();
            bus = new EventBus();
            initialized = new List<string>();
            events = new List<string>();

            bus.Subscribe<AppBootStarted>(e => events.Add($"started:{e.ServiceCount}"));
            bus.Subscribe<ServiceInitialized>(e => events.Add($"init:{e.ServiceName}:{e.Order}"));
            bus.Subscribe<AppBootCompleted>(e => events.Add($"completed:{e.ServiceCount}"));
            bus.Subscribe<AppBootFailed>(e => events.Add($"failed:{e.ServiceName}"));
        }

        FakeService<TTag> RecordingStep<TTag>(string name) where TTag : class, new() =>
            new FakeService<TTag>(name, () => initialized.Add(name));

        [Test]
        public async Task Services_Initialize_InRegistrationOrder()
        {
            registry.Register(RecordingStep<TagOne>("save"));
            registry.Register(RecordingStep<TagTwo>("settings"));
            registry.Register(RecordingStep<TagThree>("audio"));

            BootResult result = await new AppBoot(registry, bus).RunAsync();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(initialized, Is.EqualTo(new[] { "save", "settings", "audio" }));
        }

        [Test]
        public async Task SuccessfulBoot_PublishesStartedThenEachServiceThenCompleted()
        {
            registry.Register(RecordingStep<TagOne>("save"));
            registry.Register(RecordingStep<TagTwo>("settings"));

            await new AppBoot(registry, bus).RunAsync();

            Assert.That(events, Is.EqualTo(new[]
            {
                "started:2",
                "init:save:0",
                "init:settings:1",
                "completed:2",
            }));
        }

        [Test]
        public async Task FailingService_StopsTheChain_AndReportsTheService()
        {
            registry.Register(RecordingStep<TagOne>("save"));
            registry.Register(new FakeService<TagTwo>(
                "settings",
                failure: new InvalidOperationException("disk gone")));
            registry.Register(RecordingStep<TagThree>("audio"));

            BootResult result = await new AppBoot(registry, bus).RunAsync();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailedService, Is.EqualTo("settings"));
            Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(initialized, Is.EqualTo(new[] { "save" }));
            Assert.That(events, Does.Contain("failed:settings"));
            Assert.That(events, Has.No.Member("completed:3"));
        }

        [Test]
        public async Task CancelledToken_InitializesNothing_AndReportsCancelled()
        {
            registry.Register(RecordingStep<TagOne>("save"));
            CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();

            BootResult result = await new AppBoot(registry, bus).RunAsync(source.Token);

            Assert.That(result.WasCancelled, Is.True);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(initialized, Is.Empty);
        }

        [Test]
        public async Task RunningTwice_Throws()
        {
            AppBoot boot = new AppBoot(registry, bus);
            await boot.RunAsync();

            Assert.ThrowsAsync<InvalidOperationException>(() => boot.RunAsync());
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new AppBoot(null, bus));
            Assert.Throws<ArgumentNullException>(() => new AppBoot(registry, null));
        }

        [Test]
        public async Task EmptyRegistry_StillCompletes()
        {
            BootResult result = await new AppBoot(registry, bus).RunAsync();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(events, Is.EqualTo(new[] { "started:0", "completed:0" }));
        }
    }
}
