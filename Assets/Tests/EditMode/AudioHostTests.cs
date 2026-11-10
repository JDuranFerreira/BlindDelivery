using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Events;
using Template.Services.Audio;
using Template.Services.Events;
using Template.Services.Settings;
using Template.Tests.EditMode.Fakes;
using UnityEngine;
using UnityEngine.TestTools;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// Audio behaviour without a real player loop: tests drive <see cref="AudioHost.Tick"/> with
    /// their own dt, so crossfades, ducking and pool rotation are exact rather than frame-luck.
    /// </summary>
    public class AudioHostTests
    {
        GameObject gameObject;
        AudioHost host;
        EventBus bus;
        SettingsService settings;
        AudioLibrary library;
        AudioClip click;
        AudioClip loop;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("audio-host");
            host = gameObject.AddComponent<AudioHost>();
            bus = new EventBus();
            settings = new SettingsService(bus, new InMemoryPrefsStore());
            library = ScriptableObject.CreateInstance<AudioLibrary>();
            click = AudioClip.Create("click", 8, 1, 8000, false);
            loop = AudioClip.Create("loop", 8, 1, 8000, false);
            library.Add("click", click);
            library.Add("loop", loop);
            host.Construct(bus, settings);
            host.Library = library;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(library);
            Object.DestroyImmediate(click);
            Object.DestroyImmediate(loop);
        }

        async Task<AudioHost> Booted()
        {
            await host.InitializeAsync();
            return host;
        }

        [Test]
        public async Task Init_PrewarmsMusicAndSfxPools()
        {
            await Booted();

            Assert.That(host.MusicSources.Count, Is.EqualTo(2));
            Assert.That(host.SfxSources.Count, Is.EqualTo(8));
        }

        [Test]
        public async Task Volumes_FollowSettings_Immediately()
        {
            settings.MusicVolume = 0.5f;
            await Booted();
            host.PlayMusic("loop", 0f);
            host.Tick(0f);

            Assert.That(host.MusicSources[1].volume, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public async Task MutedMusic_PlaysSilently()
        {
            await Booted();
            settings.MusicMuted = true;
            host.PlayMusic("loop", 0f);
            host.Tick(0f);

            Assert.That(host.MusicSources[host.MusicSources.Count - 1].volume, Is.EqualTo(0f));
        }

        [Test]
        public async Task PlaySfx_CyclesThePool_AndSetsThePlaybackVolume()
        {
            await Booted();

            host.PlaySfx("click");
            host.PlaySfx("click");

            Assert.That(host.SfxSources[0].clip, Is.EqualTo(click));
            Assert.That(host.SfxSources[1].clip, Is.EqualTo(click));
            Assert.That(host.SfxSources[0].volume, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public async Task MissingKeys_LogAndDoNothing()
        {
            await Booted();
            LogAssert.ignoreFailingMessages = true;
            try
            {
                host.PlayMusic("nope", 0.1f);
                host.PlaySfx("nope");
                host.Tick(0f);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            Assert.That(host.SfxSources[0].clip, Is.Null);
            Assert.That(host.MusicSources[0].volume, Is.EqualTo(0f));
        }

        [Test]
        public async Task PlayMusic_InstantSwitch_ReplacesTheTrack()
        {
            await Booted();

            host.PlayMusic("loop", 0f);
            host.Tick(0f);

            Assert.That(host.MusicSources[1].clip, Is.EqualTo(loop));
            Assert.That(host.MusicSources[1].volume, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(host.MusicSources[0].volume, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public async Task SameTrackWhilePlaying_DoesNotRestart()
        {
            await Booted();
            host.PlayMusic("loop", 0f);

            host.PlayMusic("loop", 0.5f);
            host.Tick(0.1f);

            Assert.That(host.MusicSources[1].volume, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public async Task Crossfade_SplitsVolume_ArrivingAtFull()
        {
            await Booted();
            host.PlayMusic("loop", 0f);

            host.PlayMusic("click", 0.5f);
            host.Tick(0.25f);

            Assert.That(host.MusicSources[1].volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(host.MusicSources[0].volume, Is.EqualTo(0.5f).Within(0.0001f));

            host.Tick(0.25f);

            Assert.That(host.MusicSources[0].volume, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(host.MusicSources[1].volume, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public async Task StopMusic_FadesOut_AndEndsSilent()
        {
            await Booted();
            host.PlayMusic("loop", 0f);

            host.StopMusic(0.5f);
            host.Tick(0.25f);

            Assert.That(host.MusicSources[1].volume, Is.EqualTo(0.5f).Within(0.0001f));

            host.Tick(0.25f);

            Assert.That(host.MusicSources[1].volume, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(host.MusicSources[0].volume, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public async Task Duck_ComposesWithVolume_ThenRecoversToFull()
        {
            await Booted();
            settings.SfxVolume = 0.8f;

            host.Duck(0.25f, 0.2f);
            host.PlaySfx("click");

            Assert.That(host.SfxSources[0].volume, Is.EqualTo(0.2f).Within(0.0001f));

            host.Tick(0.2f); // hold ends
            host.Tick(0.4f); // recovery: 0.25 + 2*0.4 -> full

            Assert.That(host.SfxSources[0].volume, Is.EqualTo(0.8f).Within(0.0001f));
        }
    }
}