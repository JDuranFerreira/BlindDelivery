using System.Threading;
using System.Threading.Tasks;


namespace BlindDelivery.Core.RemoteConfig
{
    public enum RemoteConfigStatus
    {
        /// <summary>A new config arrived and is now the served state.</summary>
        Fetched = 0,

        /// <summary>The fetch failed outright (no network, bad url) â€” fallback values are served.</summary>
        Offline = 1,
        /// <summary>The fetch hit the fetch timeout â€” fallback values are served.</summary>
        Timeout = 2
    }


    /// <summary>What one fetch attempt produced.</summary>
    public readonly struct RemoteConfigFetchResult
    {
        public RemoteConfigFetchResult(RemoteConfigStatus status)
        {
            Status = status;
        }


        public RemoteConfigStatus Status { get; }
    }


    /// <summary>
    /// A place the config comes from (goals note system 14: Firebase or Unity Remote Config for
    /// real). The BlindDelivery ships the seam plus a cache-backed service; a game adds the concrete
    /// source, so no package sits in the BlindDelivery's dependency list.
    /// </summary>
    public interface IRemoteConfigSource
    {
        Task<RemoteConfigFetchResult> FetchAsync(RemoteConfigDocument document, CancellationToken token);
    }


    /// <summary>The wire format: a flat list of typed key/value strings, version-stamped.</summary>
    [System.Serializable]
    public sealed class RemoteConfigDocument
    {
        public string version = "0";
        public RemoteConfigValue[] values = new RemoteConfigValue[0];
    }


    [System.Serializable]
    public sealed class RemoteConfigValue
    {
        public string key;
        /// <summary>Serialized text of the value; typed read methods parse it deliberately.</summary>
        public string value;
    }


    /// <summary>
    /// Typed configuration values with this hierarchy: fetched last-good values, then shipped
    /// defaults, then the caller's own fallback. Fetching never blocks startup longer than its
    /// timeout, and failure leaves whatever was last known serving (goals note: never break
    /// offline, never block the boot).
    /// </summary>
    public interface IRemoteConfig : BlindDelivery.Core.Services.IAppService
    {
        /// <summary>Served values come from the last successful fetch when true, otherwise defaults.</summary>
        bool HasRemoteValues { get; }

        /// <summary>Version stamp of the last successful fetch, or the built-in defaults' version.</summary>
        string Version { get; }

        bool GetBool(string key, bool fallback = false);

        int GetInt(string key, int fallback = 0);
        float GetFloat(string key, float fallback = 0f);

        string GetString(string key, string fallback = "");

        /// <summary>Fetches once with a hard timeout. Never throws and never blocks beyond the timeout.</summary>
        Task<RemoteConfigFetchResult> FetchAsync(CancellationToken token = default);
    }
}


