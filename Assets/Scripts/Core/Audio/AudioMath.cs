using System;

namespace Template.Core.Audio
{
    /// <summary>
    /// The one place the 0..1 volume convention meets decibels. A volume slider is linear-0..1 for
    /// the player; an audio mixer wants dB, and the ear wants the log curve. Pure so the curve is
    /// testable without any audio system attached — System.Math, not Mathf, because Core keeps
    /// no engine references.
    /// </summary>
    public static class AudioMath
    {
        /// <summary>Silence in dB. Attenuators cannot do -infinity; this floor doubles as 'mute'.</summary>
        public const float SilenceDb = -80f;

        /// <summary>Linear 0..1 to dB with a log curve: 1 -> 0 dB, 0 -> the silence floor.</summary>
        public static float ToDb(float linear01)
        {
            float clamped = Math.Clamp(linear01, 0f, 1f);
            return clamped <= 0f ? SilenceDb : (float)(20.0 * Math.Log10(clamped));
        }

        /// <summary>dB back to linear 0..1; the exact inverse of <see cref="ToDb"/>.</summary>
        public static float ToLinear(float db)
        {
            return Math.Clamp((float)Math.Pow(10.0, db / 20.0), 0f, 1f);
        }

        /// <summary>
        /// Effective volume of one source: the bus level times the duck level (ads duck everything),
        /// zeroed out by mute. Composing it here keeps AudioHost free of rules.
        /// </summary>
        public static float EffectiveVolume(float busVolume01, bool muted, float duckFactor)
        {
            if (muted)
            {
                return 0f;
            }

            return Math.Clamp(busVolume01, 0f, 1f) * Math.Clamp(duckFactor, 0f, 1f);
        }
    }
}