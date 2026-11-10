using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.RemoteConfig;
using Template.Services.RemoteConfig;
using Template.Tests.EditMode.Fakes;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The acceptance gates (architecture note, M6): defaults work offline, a successful fetch
    /// outranks the defaults, a failed or timed-out fetch leaves whatever was last served in
    /// place, and the typed getters parse deliberately or hand back the fallback.
    /// </summary>
    public class RemoteConfigServiceTests
    {
        InMemoryPrefsStore prefs;

        [SetUp]
        public void SetUp()
        {
            prefs = new InMemoryPrefsStore();
        }

        static RemoteConfigDocument Defaults()
        {
            return new RemoteConfigDocument
            {
                version = "defaults-1",
                values = new[]
                {
                    new RemoteConfigValue { key = "ad_frequency", value = "3" },
                    new RemoteConfigValue { key = "force_update", value = "false" },
                    new RemoteConfigValue { key = "kill_switch", value = "False" },
                    new RemoteConfigValue { key = "greeting", value = "hello" },
                    new RemoteConfigValue { key = "multiplier", value = "1.5" },
                },
            };
        }

        RemoteConfigService Make(FakeRemoteConfigSource source, float timeout = 5f)
        {
            RemoteConfigService service = new RemoteConfigService(Defaults(), source, prefs, timeout);
            service.InitializeAsync().Wait();
            return service;
        }

        [Test]
        public void Defaults_ServeImmediately_WithTheDefaultsVersion()
        {
            RemoteConfigService service = Make(new FakeRemoteConfigSource());

            Assert.That(service.HasRemoteValues, Is.False);
            Assert.That(service.Version, Is.EqualTo("defaults-1"));
            Assert.That(service.GetInt("ad_frequency"), Is.EqualTo(3));
            Assert.That(service.GetBool("force_update"), Is.False);
            Assert.That(service.GetBool("kill_switch"), Is.False);
            Assert.That(service.GetFloat("multiplier"), Is.EqualTo(1.5f));
            Assert.That(service.GetString("greeting"), Is.EqualTo("hello"));
        }

        [Test]
        public void TypedGetters_FallBackOnBadOrMissingValues()
        {
            RemoteConfigService service = Make(null);

            Assert.That(service.GetInt("missing", 7), Is.EqualTo(7));
            Assert.That(service.GetInt("greeting", 7), Is.EqualTo(7));
            Assert.That(service.GetBool("greeting", true), Is.True);
            Assert.That(service.GetFloat("greeting", 2f), Is.EqualTo(2f));
            Assert.That(service.GetString("missing", "fb"), Is.EqualTo("fb"));
            Assert.That(service.GetString(null, "fb"), Is.EqualTo("fb"));
        }

        [Test]
        public async Task SuccessfulFetch_OutrankDefaults_AndStampsTheVersion()
        {
            RemoteConfigDocument defaults = new RemoteConfigDocument
            {
                version = "defaults-1",
                values = new[]
                {
                    new RemoteConfigValue { key = "ad_frequency", value = "3" },
                    new RemoteConfigValue { key = "new_key", value = "42" },
                },
            };
            RemoteConfigService service = new RemoteConfigService(
                defaults, new FakeRemoteConfigSource { Values = new[]
                {
                    ("ad_frequency", "5"),
                } }, prefs);
            await service.InitializeAsync();

            RemoteConfigFetchResult result = await service.FetchAsync();

            Assert.That(result.Status, Is.EqualTo(RemoteConfigStatus.Fetched));
            Assert.That(service.HasRemoteValues, Is.True);
            Assert.That(service.Version, Is.EqualTo("remote-1"));
            Assert.That(service.GetInt("ad_frequency"), Is.EqualTo(5));
            // A key only in the defaults stays served (fetched values merge over defaults).
            Assert.That(service.GetInt("new_key"), Is.EqualTo(42));
        }

        [Test]
        public async Task OfflineFetch_KeepsWhateverWasLastServed()
        {
            RemoteConfigService service = Make(new FakeRemoteConfigSource
            {
                Outcome = RemoteConfigStatus.Offline,
            });

            RemoteConfigFetchResult result = await service.FetchAsync();

            Assert.That(result.Status, Is.EqualTo(RemoteConfigStatus.Offline));
            Assert.That(service.Version, Is.EqualTo("defaults-1"));
            Assert.That(service.GetInt("ad_frequency"), Is.EqualTo(3));
        }

        [Test]
        public async Task HittingSource_HonorsItsOwnTimeoutCard()
        {
            RemoteConfigService service = Make(new FakeRemoteConfigSource
            {
                Outcome = RemoteConfigStatus.Timeout,
            });

            RemoteConfigFetchResult result = await service.FetchAsync();

            Assert.That(result.Status, Is.EqualTo(RemoteConfigStatus.Timeout));
            Assert.That(service.GetInt("ad_frequency"), Is.EqualTo(3));
        }

        [Test]
        public async Task HungFetch_DoesNotBlockPastTheTimeout()
        {
            RemoteConfigService service = Make(new FakeRemoteConfigSource
            {
                HangForever = true,
            }, timeout: 0.05f);

            RemoteConfigFetchResult result = await service.FetchAsync();

            Assert.That(result.Status, Is.EqualTo(RemoteConfigStatus.Timeout));
            Assert.That(service.GetInt("ad_frequency"), Is.EqualTo(3));
        }

        [Test]
        public async Task LastGoodValues_AreCached_AndServedOfflineOnNextLaunch()
        {
            RemoteConfigService online = Make(new FakeRemoteConfigSource
            {
                Values = new[] { ("ad_frequency", "9") },
            });
            await online.FetchAsync();

            // "Airplane mode" launch: the source reports Offline and cannot deliver values, but
            // the cache from the previous session is already serving before the fetch.
            RemoteConfigService offline = Make(new FakeRemoteConfigSource
            {
                Outcome = RemoteConfigStatus.Offline,
            });

            Assert.That(offline.HasRemoteValues, Is.True);
            Assert.That(offline.GetInt("ad_frequency"), Is.EqualTo(9));
        }

        [Test]
        public async Task CorruptCache_FallsBackToDefaults()
        {
            prefs.SetString("template.remoteconfig.cache", "{not json");
            prefs.Save();

            RemoteConfigService service = Make(null);

            Assert.That(service.HasRemoteValues, Is.False);
            Assert.That(service.GetInt("ad_frequency"), Is.EqualTo(3));
        }
    }
}
