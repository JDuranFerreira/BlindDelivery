namespace BlindDelivery.Core.Haptics
{
    /// <summary>The vibration cues from the design doc, behind one engine-free seam.</summary>
    public enum HapticPattern
    {
        EdgeWarning,
        LedgeDetected,
        SlowPulse,
        CrateLost,
        CratesDelivered,
        ItemPlaced,
        BeaconLeft,
        BeaconRight,
    }

    /// <summary>
    /// iOS Taptic / Android Vibration behind one interface, plus a no-op implementation for the
    /// editor and for players with vibration off. Gameplay never talks to a native API directly.
    /// </summary>
    public interface IHapticsService
    {
        bool IsSupported { get; }

        void Play(HapticPattern pattern, float intensity = 1f);
    }
}
