using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Events;
using Template.Core.Settings;
using Template.Services.Events;
using Template.Services.Settings;
using Template.Tests.EditMode.Fakes;
using UnityEngine;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// Settings must persist, apply without a scene reload, and publish exactly one event per real
    /// change. The engine touchpoints here (targetFrameRate, quality) are changed and restored.
    /// </summary>
    public class SettingsServiceTests
    {
        InMemoryPrefsStore prefs;
        EventBus bus;
        SettingsService service;
        List<SettingsKey> published;

        [SetUp]
        public void SetUp()
        {
            prefs = new InMemoryPrefsStore();
            bus = new EventBus();
            service = new SettingsService(bus, prefs);
            published = new List<SettingsKey>();
            bus.Subscribe<SettingsChanged>(e => published.Add(e.Key));
        }

        [Test]
        public void Defaults_AreTheSaneSet()
        {
            Assert.That(service.MusicVolume, Is.EqualTo(1f));
            Assert.That(service.SfxVolume, Is.EqualTo(1f));
            Assert.That(service.MusicMuted, Is.False);
            Assert.That(service.SfxMuted, Is.False);
            Assert.That(service.VibrationEnabled, Is.True);
            Assert.That(service.LanguageCode, Is.EqualTo(string.Empty));
            Assert.That(service.QualityLevel, Is.EqualTo(-1));
            Assert.That(service.TargetFrameRate, Is.EqualTo(0));
        }

        [Test]
        public async Task Initialize_LoadsPersistedValues()
        {
            int originalFrameRate = Application.targetFrameRate;
            service.MusicVolume = 0.25f;
            service.SfxVolume = 0.6f;
            service.MusicMuted = true;
            service.SfxMuted = true;
            service.VibrationEnabled = false;
            service.LanguageCode = "pl";
            service.TargetFrameRate = 45;

            SettingsService reloaded = new SettingsService(bus, prefs);
            await reloaded.InitializeAsync();

            try
            {
                Assert.That(reloaded.MusicVolume, Is.EqualTo(0.25f));
                Assert.That(reloaded.SfxVolume, Is.EqualTo(0.6f));
                Assert.That(reloaded.MusicMuted, Is.True);
                Assert.That(reloaded.SfxMuted, Is.True);
                Assert.That(reloaded.VibrationEnabled, Is.False);
                Assert.That(reloaded.LanguageCode, Is.EqualTo("pl"));
                Assert.That(reloaded.TargetFrameRate, Is.EqualTo(45));
            }
            finally
            {
                Application.targetFrameRate = originalFrameRate;
            }
        }

        [Test]
        public void EachRealChange_PublishesOneEvent()
        {
            service.MusicVolume = 0.5f;
            service.SfxVolume = 0.2f;
            service.SfxMuted = true;
            service.VibrationEnabled = false;
            service.LanguageCode = "de";

            Assert.That(published, Is.EqualTo(new[]
            {
                SettingsKey.MusicVolume,
                SettingsKey.SfxVolume,
                SettingsKey.SfxMuted,
                SettingsKey.VibrationEnabled,
                SettingsKey.LanguageCode,
            }));
        }

        [Test]
        public void UnchangedSet_PublishesNothing()
        {
            service.MusicVolume = 0.5f;
            service.MusicVolume = 0.5f;
            service.VibrationEnabled = true; // already the default

            Assert.That(published, Is.EqualTo(new[] { SettingsKey.MusicVolume }));
        }

        [Test]
        public void VolumeSetters_Clamp()
        {
            service.MusicVolume = 0.25f;

            Assert.That(service.MusicVolume, Is.EqualTo(0.25f));

            service.MusicVolume = 2f;

            Assert.That(service.MusicVolume, Is.EqualTo(1f));

            service.MusicVolume = -0.5f;

            Assert.That(service.MusicVolume, Is.EqualTo(0f));
            Assert.That(published, Is.EqualTo(new[]
            {
                SettingsKey.MusicVolume,
                SettingsKey.MusicVolume,
                SettingsKey.MusicVolume,
            }));
        }

        [Test]
        public async Task TargetFrameRate_IsPersisted_AndAppliedWithoutAReload()
        {
            int original = Application.targetFrameRate;
            try
            {
                service.TargetFrameRate = 44;

                Assert.That(Application.targetFrameRate, Is.EqualTo(44));

                SettingsService reloaded = new SettingsService(bus, prefs);
                await reloaded.InitializeAsync();

                Assert.That(Application.targetFrameRate, Is.EqualTo(44));
            }
            finally
            {
                Application.targetFrameRate = original;
            }
        }

        [Test]
        public async Task QualityLevel_IsPersisted_AndApplied()
        {
            int original = QualitySettings.GetQualityLevel();
            int top = QualitySettings.names.Length - 1;
            try
            {
                service.QualityLevel = top;

                Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(top));

                SettingsService reloaded = new SettingsService(bus, prefs);
                await reloaded.InitializeAsync();

                Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(top));
            }
            finally
            {
                QualitySettings.SetQualityLevel(original, true);
            }
        }

        [Test]
        public void LanguageCode_NullCollapsesToEmpty()
        {
            service.LanguageCode = "pl";
            service.LanguageCode = null;

            Assert.That(service.LanguageCode, Is.EqualTo(string.Empty));
        }
    }
}