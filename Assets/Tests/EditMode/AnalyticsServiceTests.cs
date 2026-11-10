using NUnit.Framework;
using Template.Core.Consent;
using Template.Services.Analytics;
using Template.Services.Consent;
using Template.Services.Events;
using Template.Tests.EditMode.Fakes;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The acceptance gates (architecture note, M6): consent denied → nothing initializes, no
    /// event reaches the provider; consent granted → the provider comes up and events flow; the
    /// master switch and a mid-session revoke both cut the provider off.
    /// </summary>
    public class AnalyticsServiceTests
    {
        InMemoryPrefsStore prefs;
        EventBus bus;
        ConsentService consent;
        FakeAnalyticsSink sink;
        AnalyticsService analytics;

        [SetUp]
        public void SetUp()
        {
            prefs = new InMemoryPrefsStore();
            bus = new EventBus();
            consent = new ConsentService(prefs, bus);
            consent.InitializeAsync().Wait();
            sink = new FakeAnalyticsSink();
            analytics = new AnalyticsService(consent, sink);
            analytics.InitializeAsync().Wait();
        }

        [Test]
        public void WithoutConsent_NothingInitializes_AndEventsDrop()
        {
            Assert.That(analytics.IsEnabled, Is.False);
            Assert.That(sink.InitializeCount, Is.EqualTo(0));

            analytics.LogEvent("level_completed");

            Assert.That(sink.Logged, Is.Empty);
        }

        [Test]
        public void ConsentGranted_Initializes_AndEventsFlow()
        {
            consent.Grant();
            consent.Deny();
            consent.Grant();

            Assert.That(analytics.IsEnabled, Is.True);
            Assert.That(sink.InitializeCount, Is.EqualTo(2));
            Assert.That(sink.IsInitialized, Is.True);

            analytics.LogEvent("level_completed",
                new Template.Core.Analytics.AnalyticsParam("level", 3));

            Assert.That(sink.Logged, Is.EqualTo(new[] { "level_completed" }));
        }

        [Test]
        public void ReturningPlayer_InitializesAtBoot_WithoutANewDecision()
        {
            consent.Grant();

            AnalyticsService fresh = new AnalyticsService(consent, sink);
            fresh.InitializeAsync().Wait();

            Assert.That(fresh.IsEnabled, Is.True);
            Assert.That(sink.InitializeCount, Is.EqualTo(1));
        }

        [Test]
        public void ConsentRevokedMidSession_ShutsTheProviderDown()
        {
            consent.Grant();
            Assert.That(sink.IsInitialized, Is.True);

            consent.Deny();

            Assert.That(analytics.IsEnabled, Is.False);
            Assert.That(sink.IsInitialized, Is.False);
            Assert.That(sink.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void MasterSwitchOff_DropsEvents_AndShutsDown()
        {
            consent.Grant();
            Assert.That(sink.IsInitialized, Is.True);

            analytics.SetEnabled(false);

            Assert.That(analytics.IsEnabled, Is.False);
            Assert.That(sink.IsInitialized, Is.False);

            analytics.LogEvent("level_completed");

            Assert.That(sink.Logged, Is.Empty);
        }

        [Test]
        public void MasterSwitchOnAgain_WithConsentGranted_BringsTheProviderBack()
        {
            consent.Grant();
            analytics.SetEnabled(false);
            analytics.SetEnabled(true);

            Assert.That(analytics.IsEnabled, Is.True);
            Assert.That(sink.InitializeCount, Is.EqualTo(2));
        }

        [Test]
        public void NoSink_IsNeverEnabled_AndDropsEverythingSilently()
        {
            AnalyticsService sinkless = new AnalyticsService(consent, null);
            sinkless.InitializeAsync().Wait();

            consent.Grant();

            Assert.That(sinkless.IsEnabled, Is.False);
            Assert.DoesNotThrow(() => sinkless.LogEvent("level_completed"));
        }
    }
}
