using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Template.Core.Consent;
using Template.Core.Events;
using Template.Services.Consent;
using Template.Services.Events;
using Template.Tests.EditMode.Fakes;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The decision persists across launches, every real change raises exactly one event on both
    /// the interface and the bus, and Unknown is the first-launch state.
    /// </summary>
    public class ConsentServiceTests
    {
        InMemoryPrefsStore prefs;
        EventBus bus;
        ConsentService service;
        List<ConsentState> changed;
        List<ConsentState> published;

        [SetUp]
        public void SetUp()
        {
            prefs = new InMemoryPrefsStore();
            bus = new EventBus();
            service = new ConsentService(prefs, bus);
            service.InitializeAsync().Wait();
            changed = new List<ConsentState>();
            published = new List<ConsentState>();
            service.OnConsentChanged += changed.Add;
            bus.Subscribe<ConsentChanged>(e => published.Add(e.State));
        }

        [Test]
        public void FirstLaunch_IsUnknown()
        {
            Assert.That(service.State, Is.EqualTo(ConsentState.Unknown));
            Assert.That(changed, Is.Empty);
            Assert.That(published, Is.Empty);
        }

        [Test]
        public async System.Threading.Tasks.Task Grant_RecordsAndRaises()
        {
            await service.InitializeAsync();
            service.Grant();

            Assert.That(service.State, Is.EqualTo(ConsentState.Granted));
            Assert.That(changed, Is.EqualTo(new[] { ConsentState.Granted }));
            Assert.That(published, Is.EqualTo(new[] { ConsentState.Granted }));
        }

        [Test]
        public async System.Threading.Tasks.Task Deny_RecordsAndRaises()
        {
            await service.InitializeAsync();
            service.Deny();

            Assert.That(service.State, Is.EqualTo(ConsentState.Denied));
            Assert.That(changed, Is.EqualTo(new[] { ConsentState.Denied }));
            Assert.That(published, Is.EqualTo(new[] { ConsentState.Denied }));
        }

        [Test]
        public void RepeatingTheSameDecision_RaisesNothing()
        {
            service.Grant();
            service.Grant();

            Assert.That(changed.Single(), Is.EqualTo(ConsentState.Granted));
        }

        [Test]
        public async System.Threading.Tasks.Task Decision_PersistsAcrossLaunches()
        {
            service.Grant();
            service.Deny(); // exercise the later change too

            ConsentService reloadedAfterGrant = new ConsentService(prefs, bus);
            await reloadedAfterGrant.InitializeAsync();
            Assert.That(reloadedAfterGrant.State, Is.EqualTo(ConsentState.Denied));
        }
    }
}
