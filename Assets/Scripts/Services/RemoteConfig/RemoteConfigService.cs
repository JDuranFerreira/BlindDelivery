using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Settings;

namespace Template.Services.RemoteConfig
{
    /// <summary>
    /// Typed configuration values with this hierarchy: fetched last-good values, then shipped
    /// defaults, then the caller's own fallback (goals note system 14: never break offline,
    /// never block the boot).
    ///
    /// The template carries only the cache — a game supplies the concrete
    /// <see cref="IRemoteConfigSource"/> (Firebase or Unity Remote Config) and its shipped
    /// defaults document. Kill switches and force-update flags are ordinary bool/keys read
    /// through <see cref="GetBool"/>; nothing here is ad- or updater-specific.
    /// </summary>
    public sealed class RemoteConfigService : Template.Core.RemoteConfig.IRemoteConfig,
        Template.Core.Services.IAppService
    {
        const string CacheKey = "template.remoteconfig.cache";

        readonly Template.Core.RemoteConfig.IRemoteConfigSource source;
        readonly IPrefsStore prefs;
        readonly float timeoutSeconds;
        readonly Dictionary<string, string> defaults;
        readonly string defaultsVersion;
        readonly Dictionary<string, string> served = new Dictionary<string, string>(StringComparer.Ordinal);

        bool hasRemoteValues;
        string version = "0";

        /// <param name="defaults">Shipped-in-build document; always parseable, always served at minimum.</param>
        /// <param name="source">Concrete fetcher; null keeps the defaults (tests, offline-only games).</param>
        /// <param name="prefs">Durable cache for the last successful fetch, so airplane mode still serves values.</param>
        /// <param name="timeoutSeconds">Hard cap on one fetch attempt; the boot never waits past this.</param>
        public RemoteConfigService(
            Template.Core.RemoteConfig.RemoteConfigDocument defaults,
            Template.Core.RemoteConfig.IRemoteConfigSource source,
            IPrefsStore prefs,
            float timeoutSeconds = 5f)
        {
            this.source = source;
            this.prefs = prefs;
            this.timeoutSeconds = timeoutSeconds <= 0f ? 5f : timeoutSeconds;
            this.defaults = Parse(defaults);
            defaultsVersion = defaults?.version ?? "0";
        }

        public string Name => "RemoteConfig";

        /// <summary>Served values come from the last successful fetch when true, otherwise defaults.</summary>
        public bool HasRemoteValues => hasRemoteValues;

        /// <summary>Version stamp of the last successful fetch, or the built-in defaults' version.</summary>
        public string Version => version;

        public Task InitializeAsync(CancellationToken token = default)
        {
            Serve(defaults, defaultsVersion: defaultsVersion);

            // Last-good values from a previous session outrank defaults immediately, before any
            // fetch this session could succeed or fail.
            if (prefs != null && prefs.TryGetString(CacheKey, out string cached))
            {
                Template.Core.RemoteConfig.RemoteConfigDocument cachedDoc = JsonSafe(cached);
                if (cachedDoc != null)
                {
                    Serve(Parse(cachedDoc), cachedDoc.version);
                    hasRemoteValues = true;
                }
            }

            return Task.CompletedTask;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            return TryGet(key, out string raw)
                && bool.TryParse(raw, out bool parsed)
                    ? parsed
                    : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            return TryGet(key, out string raw)
                && int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsed)
                    ? parsed
                    : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            return TryGet(key, out string raw)
                && float.TryParse(
                    raw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float parsed)
                    ? parsed
                    : fallback;
        }

        public string GetString(string key, string fallback = "")
        {
            return TryGet(key, out string raw) ? raw : fallback;
        }

        /// <summary>
        /// Fetches once with a hard timeout. Never throws (a throwing source counts as offline)
        /// and never blocks beyond the timeout.
        /// </summary>
        public async Task<Template.Core.RemoteConfig.RemoteConfigFetchResult> FetchAsync(
            CancellationToken token = default)
        {
            if (source == null || token.IsCancellationRequested)
            {
                return new Template.Core.RemoteConfig.RemoteConfigFetchResult(
                    Template.Core.RemoteConfig.RemoteConfigStatus.Offline);
            }

            Template.Core.RemoteConfig.RemoteConfigDocument document =
                new Template.Core.RemoteConfig.RemoteConfigDocument();

            Task<Template.Core.RemoteConfig.RemoteConfigFetchResult> fetchTask =
                source.FetchAsync(document, token);
            Task winner = await Task.WhenAny(fetchTask, Task.Delay(TimeoutSpan(), token))
                .ConfigureAwait(false);

            if (winner != fetchTask)
            {
                return new Template.Core.RemoteConfig.RemoteConfigFetchResult(
                    Template.Core.RemoteConfig.RemoteConfigStatus.Timeout);
            }

            Template.Core.RemoteConfig.RemoteConfigFetchResult result = await fetchTask
                .ConfigureAwait(false);

            if (result.Status == Template.Core.RemoteConfig.RemoteConfigStatus.Fetched)
            {
                Serve(Parse(document), document.version);
                hasRemoteValues = true;
                Cache(document);
            }

            return result;
        }

        TimeSpan TimeoutSpan()
        {
            return TimeSpan.FromSeconds(timeoutSeconds);
        }

        bool TryGet(string key, out string value)
        {
            if (key != null)
            {
                return served.TryGetValue(key, out value);
            }

            value = null;
            return false;
        }

        void Serve(Dictionary<string, string> values, string defaultsVersion)
        {
            // Fetched or cached values merge over the shipped defaults: a remote never removes a
            // default, so a partial document leaves the untouched keys still served.
            served.Clear();
            foreach (KeyValuePair<string, string> pair in defaults)
            {
                served.Add(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, string> pair in values)
            {
                served[pair.Key] = pair.Value;
            }

            version = defaultsVersion;
        }

        void Cache(Template.Core.RemoteConfig.RemoteConfigDocument document)
        {
            if (prefs == null)
            {
                return;
            }

            prefs.SetString(CacheKey, JsonSafe(document));
            prefs.Save();
        }

        static Dictionary<string, string> Parse(Template.Core.RemoteConfig.RemoteConfigDocument document)
        {
            Dictionary<string, string> parsed =
                new Dictionary<string, string>(StringComparer.Ordinal);
            if (document?.values != null)
            {
                for (int i = 0; i < document.values.Length; i++)
                {
                    Template.Core.RemoteConfig.RemoteConfigValue entry = document.values[i];
                    if (!string.IsNullOrEmpty(entry?.key))
                    {
                        parsed[entry.key] = entry.value ?? string.Empty;
                    }
                }
            }

            return parsed;
        }

        /// <summary>JsonUtility lives in the engine, so serialization is isolated to the Services
        /// assembly; a null round-trip means the cache was corrupt and defaults take over.</summary>
        static string JsonSafe(Template.Core.RemoteConfig.RemoteConfigDocument document)
        {
            try
            {
                return UnityEngine.JsonUtility.ToJson(document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static Template.Core.RemoteConfig.RemoteConfigDocument JsonSafe(string json)
        {
            try
            {
                return UnityEngine.JsonUtility.FromJson<Template.Core.RemoteConfig.RemoteConfigDocument>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
