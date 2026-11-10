using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.Save
{
    /// <summary>
    /// Durable storage for the game's own data (progress, currency, unlocks), versioned and
    /// crash-safe. The contract lives in Core so gameplay code can take
    /// <c>ISaveService</c> through a constructor; the file implementation lives in Services.
    ///
    /// One JSON envelope per key, stamped with a schema version. Older payloads are pulled
    /// forward through the registered migration chain on load; a payload from a *newer* game
    /// version is refused instead of silently misread. Writes are atomic (temp file swapped in)
    /// and the previous content is kept as a backup, so a kill during a write degrades to
    /// "either the new data or the old data", never a half-written file.
    ///
    /// Settings are NOT saved here — they go through <see cref="Template.Core.Settings.ISettingsService"/>
    /// (PlayerPrefs, see the architecture note). This store is for data that must survive
    /// app updates as a coherent document.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>Highest migration target registered so far; the version new saves are stamped with.</summary>
        int CurrentVersion { get; }

        /// <summary>
        /// Registers one migration step. Steps must attach in order: the first registered step
        /// starts the chain, every later step must continue it (<c>FromVersion</c> equal to the
        /// previous step's <c>ToVersion</c>, and a single version forward). Registering out of
        /// order is a coding bug and throws immediately, before any real save is touched.
        /// </summary>
        void RegisterMigration(ISaveMigration migration);

        /// <summary>True when a document for this key exists (main or backup).</summary>
        bool Exists(string key);

        /// <summary>The schema version stored for this key, or -1 when the key is unknown.</summary>
        int GetStoredVersion(string key);

        Task SaveAsync<TData>(string key, TData data, CancellationToken token = default) where TData : class, new();

        /// <summary>
        /// Loads the document for this key: fresh defaults when nothing was ever saved, migrated
        /// forward when the stored version is older than the current one, an exception when it is
        /// newer than the game knows. A main file that fails to parse falls back to the backup;
        /// both failing is raised as <see cref="Template.Core.Save.SaveCorruptException"/>.
        /// </summary>
        Task<TData> LoadAsync<TData>(string key, CancellationToken token = default) where TData : class, new();

        /// <summary>
        /// Durability point for pause/quit hooks. Writes are synchronous today, so the flush is
        /// already durable when SaveAsync returns; the hook keeps that guarantee for a future
        /// queueing or cloud-backed store and lets gameplay never care.
        /// </summary>
        Task FlushAsync(CancellationToken token = default);
    }
}