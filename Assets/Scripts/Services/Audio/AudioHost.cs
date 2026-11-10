using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Audio;
using Template.Core.Events;
using Template.Core.Services;
using Template.Core.Settings;
using UnityEngine;

namespace Template.Services.Audio
{
    /// <summary>
    /// The audio system's engine half: the AudioSources for music and effects, pooled and prewarmed
    /// at init, driven from settings through <see cref="AudioMath"/>. This is the one M2 service
    /// that must touch the engine, so it keeps every decision that does not need one in Core
    /// (dB math, effective volume) and takes its volume/mute/duck inputs as data - which keeps the
    /// behaviour testable in EditMode by driving <see cref="Tick"/> directly instead of the player loop.
    ///
    /// Music is two sources crossfading; effects are a round-robin pool sized for a few cues per
    /// frame. Music recovery from a duck and fades are linear here; curve tweaks belong to AudioMath.
    /// </summary>
    public sealed class AudioHost : MonoBehaviour, IAudioService, IAppService
    {
        const string ServiceName = "Audio";
        const int MusicSourceCount = 2;
        const int SfxSourceCount = 8;
        const float DefaultCrossfadeSeconds = 0.5f;
        const float DuckPerSecond = 2f;

        readonly List<AudioSource> musicSources = new List<AudioSource>(MusicSourceCount);
        readonly List<AudioSource> sfxSources = new List<AudioSource>(SfxSourceCount);
        readonly float[] sfxVolumeScale = new float[SfxSourceCount];

        IEventBus bus;
        ISettingsService settings;
        bool subscribed;

        int musicActive;
        bool fading;
        float fadeClock;
        float fadeDuration;
        int musicFrom;
        int musicTo;
        bool stopAfterFade;
        bool musicRunning;

        int sfxNext;
        float duck = 1f;
        float duckHold;

        public string Name => ServiceName;

        public AudioLibrary Library { get; set; }

        public IReadOnlyList<AudioSource> MusicSources => musicSources;

        public IReadOnlyList<AudioSource> SfxSources => sfxSources;

        /// <summary>Injection for the pure-C# parts a MonoBehaviour cannot take through its constructor.</summary>
        public void Construct(IEventBus bus, ISettingsService settings)
        {
            if (bus == null)
            {
                throw new ArgumentNullException(nameof(bus));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (this.bus != null)
            {
                throw new InvalidOperationException("AudioHost was constructed twice.");
            }

            this.bus = bus;
            this.settings = settings;
        }

        public Task InitializeAsync(CancellationToken token = default)
        {
            if (bus == null)
            {
                throw new InvalidOperationException(
                    "AudioHost was not constructed. The composition root must Construct(bus, settings) before boot.");
            }

            CreateSources();
            bus.Subscribe<SettingsChanged>(OnSettingsChanged);
            subscribed = true;
            ApplyVolumes();
            return Task.CompletedTask;
        }

        void CreateSources()
        {
            for (int i = 0; i < MusicSourceCount; i++)
            {
                musicSources.Add(CreateSource($"Music {i}"));
            }

            for (int i = 0; i < SfxSourceCount; i++)
            {
                sfxSources.Add(CreateSource($"Sfx {i}"));
            }
        }

        AudioSource CreateSource(string sourceName)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances fades and duck recovery by dt. Update() calls it with frame time; tests call it
        /// directly with any dt.
        /// </summary>
        public void Tick(float dt)
        {
            AdvanceDuck(dt);
            AdvanceFade(dt);
            ApplyVolumes();
        }

        void AdvanceDuck(float dt)
        {
            if (duckHold > 0f)
            {
                duckHold = Math.Max(0f, duckHold - dt);
                return;
            }

            if (duck < 1f)
            {
                duck = Math.Min(1f, duck + DuckPerSecond * dt);
            }
        }

        void AdvanceFade(float dt)
        {
            if (!fading)
            {
                return;
            }

            fadeClock += dt;
            if (fadeClock < fadeDuration)
            {
                return;
            }

            // Fade finished: silence and release the source we faded away from.
            AudioSource from = musicSources[musicFrom];
            from.Stop();
            from.volume = 0f;
            if (stopAfterFade)
            {
                AudioSource to = musicSources[musicTo];
                to.Stop();
            }

            musicActive = musicTo;
            musicRunning = !stopAfterFade;
            fading = false;
        }

        void ApplyVolumes()
        {
            // Music channel gains: silent until something is running, split progress during a fade,
            // and zero again once music stops. Sfx sources carry their own per-cue emphasis.
            float musicEffective = AudioMath.EffectiveVolume(settings.MusicVolume, settings.MusicMuted, duck);
            float fadeProgress = fading ? Mathf.Clamp01(fadeClock / Math.Max(0.0001f, fadeDuration)) : 0f;
            for (int i = 0; i < musicSources.Count; i++)
            {
                float gain;
                if (fading)
                {
                    gain = i == musicFrom ? 1f - fadeProgress : (stopAfterFade ? 0f : fadeProgress);
                }
                else
                {
                    gain = musicRunning && i == musicActive ? 1f : 0f;
                }

                musicSources[i].volume = musicEffective * gain;
            }

            float sfxEffective = AudioMath.EffectiveVolume(settings.SfxVolume, settings.SfxMuted, duck);
            for (int i = 0; i < sfxSources.Count; i++)
            {
                sfxSources[i].volume = sfxEffective * Mathf.Clamp(sfxVolumeScale[i], 0f, 4f);
            }
        }

        public void PlayMusic(string musicKey, float fadeSeconds = 0.5f)
        {
            if (!TryResolve(musicKey, out AudioClip clip))
            {
                return;
            }

            AudioSource playing = musicSources[musicActive];
            if (musicRunning && !fading && playing.clip == clip)
            {
                return;
            }

            if (fading)
            {
                // A new request during a fade snaps the current one to its end first, so the
                // fade state machine never has two fades running at once.
                fadeClock = fadeDuration;
                AdvanceFade(0f);
            }

            stopAfterFade = false;
            musicFrom = musicActive;
            musicTo = (musicActive + 1) % musicSources.Count;
            musicSources[musicTo].clip = clip;
            musicSources[musicTo].loop = true;
            musicSources[musicTo].Play();
            if (fadeSeconds > 0f)
            {
                fading = true;
                fadeClock = 0f;
                fadeDuration = fadeSeconds;
            }
            else
            {
                musicSources[musicFrom].Stop();
                musicActive = musicTo;
                musicRunning = true;
            }
        }

        public void StopMusic(float fadeSeconds = 0.5f)
        {
            AudioSource playing = musicSources[musicActive];
            if (!fading && !musicRunning && playing.clip == null)
            {
                return;
            }

            stopAfterFade = true;
            musicFrom = musicActive;
            musicTo = musicActive;
            if (fadeSeconds > 0f)
            {
                fading = true;
                fadeClock = 0f;
                fadeDuration = fadeSeconds;
            }
            else
            {
                musicSources[musicActive].Stop();
                musicRunning = false;
            }
        }

        public void PlaySfx(string sfxKey, float volumeScale = 1f)
        {
            if (!TryResolve(sfxKey, out AudioClip clip))
            {
                return;
            }

            int index = sfxNext;
            sfxNext = (sfxNext + 1) % sfxSources.Count;

            AudioSource source = sfxSources[index];
            source.clip = clip;
            sfxVolumeScale[index] = volumeScale;
            source.Play();
            ApplyVolumes();
        }

        bool TryResolve(string key, out AudioClip clip)
        {
            clip = null;
            if (Library == null || !Library.TryGetClip(key, out clip))
            {
                Debug.LogWarning($"[AudioHost] No audio clip registered for key '{key}'.", this);
                return false;
            }

            return true;
        }

        public void Duck(float factor, float holdSeconds = 0f)
        {
            duck = Mathf.Clamp(factor, 0f, 1f);
            duckHold = duck < 1f ? Math.Max(0f, holdSeconds) : 0f;
            ApplyVolumes();
        }

        void OnSettingsChanged(SettingsChanged changed)
        {
            switch (changed.Key)
            {
                case SettingsKey.MusicVolume:
                case SettingsKey.SfxVolume:
                case SettingsKey.MusicMuted:
                case SettingsKey.SfxMuted:
                    ApplyVolumes();
                    break;
            }
        }

        void OnDestroy()
        {
            if (subscribed)
            {
                bus.Unsubscribe<SettingsChanged>(OnSettingsChanged);
                subscribed = false;
            }
        }
    }
}