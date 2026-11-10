using Template.Core.Settings;

namespace Template.Core.Settings
{
    /// <summary>
    /// Key-value persistence under <see cref="ISettingsService"/>: settings go to PlayerPrefs,
    /// not the save files (architecture note, M2 - they are taste, not progress). Kept as its own
    /// seam so the typed-clamp-decide-publish logic in the settings service is testable with an
    /// in-memory fake instead of the editor's registry.
    /// </summary>
    public interface IPrefsStore
    {
        bool TryGetFloat(string key, out float value);

        void SetFloat(string key, float value);

        bool TryGetInt(string key, out int value);

        void SetInt(string key, int value);

        bool TryGetString(string key, out string value);

        void SetString(string key, string value);

        /// <summary>Makes the accumulated changes durable. Called after each change.</summary>
        void Save();
    }
}