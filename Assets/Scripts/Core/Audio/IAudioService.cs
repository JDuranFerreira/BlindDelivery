namespace Template.Core.Audio
{
    /// <summary>
    /// Music and sound effects, addressed by key. Core keeps this engine-free, so gameplay and UI
    /// depend on a pure C# seam: no <see cref="AudioClip"/> leaks into callers, the library that
    /// resolves keys sits in Services. Volumes come from
    /// <see cref="Template.Core.Settings.ISettingsService"/>; music and effects are ducked by a
    /// temporary factor (full-screen ads), never muted mid-frame.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Starts (or fades to) the looping track for this key. A missing key logs once and does nothing.</summary>
        void PlayMusic(string musicKey, float fadeSeconds = 0.5f);

        /// <summary>Fades the music out over fadeSeconds, then stops it.</summary>
        void StopMusic(float fadeSeconds = 0.5f);

        /// <summary>Plays a one-shot effect. volumeScale is per-cue emphasis, 1 = full.</summary>
        void PlaySfx(string sfxKey, float volumeScale = 1f);

        /// <summary>
        /// Ducks the whole output to factor (0..1, 1 = full) for holdSeconds, then recovers
        /// linearly to full. Used around full-screen ads so the app is quiet when it may not play sound.
        /// </summary>
        void Duck(float factor, float holdSeconds = 0f);
    }
}