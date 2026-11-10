using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Events;
using Template.Core.Save;
using Template.Core.Services;
using UnityEngine;

namespace Template.Services.Save
{
    /// <summary>
    /// The real save service over any <see cref="ISaveFileStore"/>: a versioned JSON envelope
    /// {"version": n, "payload": {...game data...}} per key, a forward-only migration chain,
    /// and the crash-safety contract the store implements. JSON is JsonUtility, so TData is any
    /// Unity-serializable class (fields, no nested collections) - the same constraint game data
    /// would carry into an inspector-friendly ([Serializable]) model anyway.
    ///
    /// Load policy: fresh defaults when nothing was ever saved; migrated forward when the
    /// document is older than the chain's current version; <see cref="SaveVersionException"/>
    /// when it is newer than this build (dated data, not a silent reset); the backup file takes
    /// over when the main document cannot be parsed; both failing is the last resort and raises
    /// <see cref="SaveCorruptException"/> for the caller to decide between a reset and reporting.
    /// </summary>
    public sealed class SaveService : ISaveService, IAppService
    {
        const int InitialVersion = 1;

        readonly ISaveFileStore store;
        readonly IEventBus bus;
        readonly List<ISaveMigration> chain = new List<ISaveMigration>();
        readonly Dictionary<string, int> storedVersions = new Dictionary<string, int>(StringComparer.Ordinal);

        public SaveService(ISaveFileStore store, IEventBus bus)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public string Name => "Save";

        /// <summary>
        /// Nothing to initialize: the store is lazy and loads happen on demand - a save is read
        /// when the game asks for it, not at boot. Registered in the chain for ordering only.
        /// </summary>
        public Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;

        public int CurrentVersion => chain.Count == 0 ? InitialVersion : chain[chain.Count - 1].ToVersion;

        public void RegisterMigration(ISaveMigration migration)
        {
            if (migration == null)
            {
                throw new ArgumentNullException(nameof(migration));
            }

            int expectedFrom = chain.Count == 0 ? InitialVersion : chain[chain.Count - 1].ToVersion;
            if (migration.FromVersion != expectedFrom || migration.ToVersion != migration.FromVersion + 1)
            {
                throw new InvalidOperationException(
                    $"The migration chain must stay contiguous from version {InitialVersion}: " +
                    $"expected a step {expectedFrom}->{expectedFrom + 1} and got " +
                    $"{migration.FromVersion}->{migration.ToVersion}.");
            }

            chain.Add(migration);
        }

        public bool Exists(string key)
        {
            SaveKey.Validate(key);
            return storedVersions.ContainsKey(key) || store.Exists(key);
        }

        public int GetStoredVersion(string key)
        {
            SaveKey.Validate(key);
            return storedVersions.TryGetValue(key, out int version) ? version : -1;
        }

        public async Task SaveAsync<TData>(string key, TData data, CancellationToken token = default)
            where TData : class, new()
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            SaveKey.Validate(key);

            SaveEnvelope envelope = new SaveEnvelope
            {
                version = CurrentVersion,
                payload = JsonUtility.ToJson(data),
            };
            await store.WriteAsync(key, JsonUtility.ToJson(envelope));

            storedVersions[key] = envelope.version;
            bus.Publish(new SaveWritten(key, envelope.version));
        }

        public async Task<TData> LoadAsync<TData>(string key, CancellationToken token = default)
            where TData : class, new()
        {
            SaveKey.Validate(key);

            string mainText = await store.ReadMainAsync(key);
            if (mainText == null)
            {
                string backupText = await store.ReadBackupAsync(key);
                if (backupText == null)
                {
                    // Nothing was ever saved under this key: fresh defaults, no version recorded.
                    return new TData();
                }
            }

            TData data = await ReadWithBackupAsync<TData>(key, mainText);
            return data;
        }

        /// <summary>Decodes the main document; on corruption and corruption only, the backup is tried.</summary>
        async Task<TData> ReadWithBackupAsync<TData>(string key, string mainText) where TData : class, new()
        {
            if (mainText != null)
            {
                try
                {
                    return await DecodeAsync<TData>(key, mainText);
                }
                catch (SaveVersionException)
                {
                    // The document is real but belongs to a newer build: raising is correct even
                    // if a backup existed, because the backup is older.
                    throw;
                }
                catch (Exception error)
                {
                    return await DecodeFromBackupAsync<TData>(key, error);
                }
            }

            // No main document: the backup alone (a crash could have stopped before the swap).
            return await DecodeFromBackupAsync<TData>(key, null);
        }

        async Task<TData> DecodeFromBackupAsync<TData>(string key, Exception mainError) where TData : class, new()
        {
            string backupText = await store.ReadBackupAsync(key);
            if (backupText == null)
            {
                throw new SaveCorruptException(
                    $"The save '{key}' failed to parse and has no backup. Data is unreadable.",
                    mainError);
            }

            try
            {
                return await DecodeAsync<TData>(key, backupText);
            }
            catch (SaveVersionException)
            {
                throw;
            }
            catch (Exception backupError)
            {
                throw new SaveCorruptException(
                    $"The save '{key}' failed to parse from both the main document and the backup. Data is unreadable.",
                    backupError);
            }
        }

        async Task<TData> DecodeAsync<TData>(string key, string text) where TData : class, new()
        {
            SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(text);
            if (envelope == null || string.IsNullOrEmpty(envelope.payload) || envelope.version < 1)
            {
                throw new SaveCorruptException($"The save '{key}' is not a versioned envelope.", null);
            }

            if (envelope.version > CurrentVersion)
            {
                throw new SaveVersionException(
                    $"The save '{key}' is schema version {envelope.version}, newer than this build " +
                    $"understands ({CurrentVersion}). Update the app or clear the data.");
            }

            string payload = envelope.version < CurrentVersion
                ? MigrateForward(key, envelope.version, envelope.payload)
                : envelope.payload;

            TData data = JsonUtility.FromJson<TData>(payload);
            if (data == null)
            {
                throw new SaveCorruptException($"The save '{key}' decoded to no data after migration.", null);
            }

            storedVersions[key] = envelope.version;
            return data;
        }

        string MigrateForward(string key, int version, string payload)
        {
            while (version < CurrentVersion)
            {
                ISaveMigration step = FindStep(version, key);
                payload = step.Migrate(payload);
                version = step.ToVersion;
            }

            return payload;
        }

        ISaveMigration FindStep(int version, string key)
        {
            foreach (ISaveMigration migration in chain)
            {
                if (migration.FromVersion == version)
                {
                    return migration;
                }
            }

            throw new SaveVersionException(
                $"The save '{key}' is at schema version {version}, which the registered migration " +
                $"chain (starting at {OldestSupportedVersion()}) cannot read.");
        }

        int OldestSupportedVersion() => chain.Count == 0 ? InitialVersion : chain[0].FromVersion;

        public Task FlushAsync(CancellationToken token = default) => Task.CompletedTask;

        /// <summary>The on-disk envelope. payload is the game data itself, double-encoded by JsonUtility.</summary>
        [Serializable]
        class SaveEnvelope
        {
            public int version;
            public string payload;
        }
    }
}