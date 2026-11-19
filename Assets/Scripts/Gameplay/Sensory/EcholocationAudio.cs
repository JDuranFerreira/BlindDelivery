using UnityEngine;

namespace BlindDelivery.Gameplay.Sensory
{
    /// <summary>
    /// Procedural echolocation hum: one spatial-free sine whose pitch and volume rise as an edge
    /// gets closer. Generated in OnAudioFilterRead so the blind mechanic is audible without any
    /// authored clip, which keeps it testable in a placeholder-art build.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class EcholocationAudio : MonoBehaviour
    {
        [SerializeField, Min(80f)] float baseFrequency = 180f;
        [SerializeField, Min(0f)] float baseVolume = 0.025f;

        float warning;
        float phase;
        float sampleRate;

        public void SetWarning(float intensity) => warning = Mathf.Clamp01(intensity);

        void Awake()
        {
            // OnAudioFilterRead runs on the audio thread, where Unity APIs are not callable.
            sampleRate = AudioSettings.outputSampleRate;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (sampleRate <= 0f) return;

            float frequency = baseFrequency * (1f + warning * 0.9f);
            float amplitude = baseVolume * (0.35f + warning * 0.65f);
            float step = 2f * Mathf.PI * frequency / sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = Mathf.Sin(phase) * amplitude;
                phase += step;
                if (phase > 2f * Mathf.PI) phase -= 2f * Mathf.PI;

                for (int channel = 0; channel < channels; channel++)
                {
                    if (i + channel < data.Length) data[i + channel] = sample;
                }
            }
        }
    }
}
