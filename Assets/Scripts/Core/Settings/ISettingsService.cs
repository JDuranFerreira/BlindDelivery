using System.Threading;

namespace Template.Core.Settings
{
    /// <summary>Which setting a <see cref="Template.Core.Events.SettingsChanged"/> refers to.</summary>
    public enum SettingsKey
    {
        MusicVolume,
        SfxVolume,
        MusicMuted,
        SfxMuted,
        VibrationEnabled,
        LanguageCode,
        QualityLevel,
        TargetFrameRate
    }

    /// <summary>
    /// The player's taste, kept apart from save data (architecture note, M2: PlayerPrefs for
    /// settings only). Typed properties so no string keys leak into callers; setters persist
    /// immediately and publish one <c>SettingsChanged</c> on the bus — subsystems apply what
    /// they own when they see it (audio on the volumes, framerate here, vibration in gameplay).
    /// Runtime application (target frame rate, quality level) happens at initialization, so
    /// settings are live before the first scene, no reload needed.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>0..1, clamped. Semantics: the listener-relative loudness of the music bus.</summary>
        float MusicVolume { get; set; }

        /// <summary>0..1, clamped, applied to effects.</summary>
        float SfxVolume { get; set; }

        bool MusicMuted { get; set; }

        bool SfxMuted { get; set; }

        /// <summary>Whether gameplay is allowed to trigger haptics.</summary>
        bool VibrationEnabled { get; set; }

        /// <summary>BCP-47-ish code ('' means "follow the device"), consumed by M5 localization.</summary>
        string LanguageCode { get; set; }

        /// <summary>Index into QualitySettings.names; -1 means "Unity's project default".</summary>
        int QualityLevel { get; set; }

        /// <summary>Frames per second the game asks for; 0 or negative leaves it untouched.</summary>
        int TargetFrameRate { get; set; }
    }
}