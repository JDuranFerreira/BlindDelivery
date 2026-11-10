using System;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Events;
using Template.Core.Services;
using Template.Core.Settings;
using UnityEngine;

namespace Template.Services.Settings
{
    /// <summary>
    /// The real settings service. Typed properties with immediate persistence and one
    /// <see cref="SettingsChanged"/> per change; subscribers apply what they own (audio the
    /// volumes, the frame-rate and quality levels apply here and now, M5 localization the
    /// language). Values load in <c>InitializeAsync</c>, so every setting is live before the
    /// first scene without a reload.
    /// </summary>
    public sealed class SettingsService : ISettingsService, IAppService
    {
        const string Prefix = "template.settings.";
        const string MusicVolumeKey = Prefix + "music-volume";
        const string SfxVolumeKey = Prefix + "sfx-volume";
        const string MusicMutedKey = Prefix + "music-muted";
        const string SfxMutedKey = Prefix + "sfx-muted";
        const string VibrationKey = Prefix + "vibration";
        const string LanguageKey = Prefix + "language-code";
        const string QualityKey = Prefix + "quality-level";
        const string FrameRateKey = Prefix + "target-frame-rate";

        readonly IEventBus bus;
        readonly IPrefsStore prefs;

        float musicVolume = 1f;
        float sfxVolume = 1f;
        bool musicMuted;
        bool sfxMuted;
        bool vibrationEnabled = true;
        string languageCode = string.Empty;
        int qualityLevel = -1;
        int targetFrameRate = 0;

        public SettingsService(IEventBus bus, IPrefsStore prefs)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
        }

        public string Name => "Settings";

        public Task InitializeAsync(CancellationToken token = default)
        {
            LoadFromPrefs();
            ApplySystemSettings();
            return Task.CompletedTask;
        }

        void LoadFromPrefs()
        {
            if (prefs.TryGetFloat(MusicVolumeKey, out float musicVolumeStored))
            {
                musicVolume = Mathf.Clamp(musicVolumeStored, 0f, 1f);
            }

            if (prefs.TryGetFloat(SfxVolumeKey, out float sfxVolumeStored))
            {
                sfxVolume = Mathf.Clamp(sfxVolumeStored, 0f, 1f);
            }

            if (prefs.TryGetInt(MusicMutedKey, out int musicMutedStored))
            {
                musicMuted = musicMutedStored != 0;
            }

            if (prefs.TryGetInt(SfxMutedKey, out int sfxMutedStored))
            {
                sfxMuted = sfxMutedStored != 0;
            }

            if (prefs.TryGetInt(VibrationKey, out int vibrationStored))
            {
                vibrationEnabled = vibrationStored != 0;
            }

            if (prefs.TryGetString(LanguageKey, out string languageStored))
            {
                languageCode = languageStored ?? string.Empty;
            }

            if (prefs.TryGetInt(QualityKey, out int qualityStored))
            {
                qualityLevel = qualityStored;
            }

            if (prefs.TryGetInt(FrameRateKey, out int frameRateStored))
            {
                targetFrameRate = frameRateStored;
            }
        }

        void ApplySystemSettings()
        {
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
            }

            if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
            }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                if (ChangeFloat(MusicVolumeKey, ref musicVolume, value))
                {
                    Publish(SettingsKey.MusicVolume);
                }
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                if (ChangeFloat(SfxVolumeKey, ref sfxVolume, value))
                {
                    Publish(SettingsKey.SfxVolume);
                }
            }
        }

        public bool MusicMuted
        {
            get => musicMuted;
            set
            {
                if (ChangeInt(MusicMutedKey, ref musicMuted, value ? 1 : 0, musicMuted ? 1 : 0))
                {
                    Publish(SettingsKey.MusicMuted);
                }
            }
        }

        public bool SfxMuted
        {
            get => sfxMuted;
            set
            {
                if (ChangeInt(SfxMutedKey, ref sfxMuted, value ? 1 : 0, sfxMuted ? 1 : 0))
                {
                    Publish(SettingsKey.SfxMuted);
                }
            }
        }

        public bool VibrationEnabled
        {
            get => vibrationEnabled;
            set
            {
                if (ChangeInt(VibrationKey, ref vibrationEnabled, value ? 1 : 0, vibrationEnabled ? 1 : 0))
                {
                    Publish(SettingsKey.VibrationEnabled);
                }
            }
        }

        public string LanguageCode
        {
            get => languageCode;
            set
            {
                string normalized = value ?? string.Empty;
                if (string.Equals(languageCode, normalized, StringComparison.Ordinal))
                {
                    return;
                }

                languageCode = normalized;
                prefs.SetString(LanguageKey, normalized);
                prefs.Save();
                Publish(SettingsKey.LanguageCode);
            }
        }

        public int QualityLevel
        {
            get => qualityLevel;
            set
            {
                int clamped = ClampQuality(value);
                if (qualityLevel == clamped)
                {
                    return;
                }

                qualityLevel = clamped;
                prefs.SetInt(QualityKey, clamped);
                prefs.Save();
                ApplySystemSettings();
                Publish(SettingsKey.QualityLevel);
            }
        }

        public int TargetFrameRate
        {
            get => targetFrameRate;
            set
            {
                if (targetFrameRate == value)
                {
                    return;
                }

                targetFrameRate = value;
                prefs.SetInt(FrameRateKey, value);
                prefs.Save();
                ApplySystemSettings();
                Publish(SettingsKey.TargetFrameRate);
            }
        }

        int ClampQuality(int value)
        {
            // -1 keeps Unity's project default; anything above the last real level clamps to it.
            return Mathf.Clamp(value, -1, QualitySettings.names.Length - 1);
        }

        bool ChangeFloat(string key, ref float field, float value)
        {
            float clamped = Mathf.Clamp(value, 0f, 1f);
            if (Math.Abs(field - clamped) < 0.0001f)
            {
                return false;
            }

            field = clamped;
            prefs.SetFloat(key, clamped);
            prefs.Save();
            return true;
        }

        bool ChangeInt(string key, ref bool field, int newValue, int currentValue)
        {
            if (newValue == currentValue)
            {
                return false;
            }

            field = newValue != 0;
            prefs.SetInt(key, newValue);
            prefs.Save();
            return true;
        }

        void Publish(SettingsKey key)
        {
            bus.Publish(new SettingsChanged(key));
        }
    }
}