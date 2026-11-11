using System;
using System.Collections.Generic;

namespace BlindDelivery.Core.GameServices
{
    /// <summary>
    /// Platform id mapping in one config (goals note system 11: per-platform achievement/board ids,
    /// mapped in one place). Game code uses stable keys; at send time the map translates them to
    /// the platform id. A missing key means "not configured" and the item is skipped (with a log
    /// at the service) rather than guessed.
    /// </summary>
    public sealed class PlatformIdMap
    {
        readonly Dictionary<string, IReadOnlyDictionary<string, string>> platformToMap =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public string CurrentPlatform { get; private set; }

        /// <summary>Configures the active platform ("android", "ios", "steam", ...).</summary>
        public PlatformIdMap WithPlatform(string platform, IReadOnlyDictionary<string, string> keysToPlatformIds)
        {
            CurrentPlatform = platform;
            platformToMap[platform] = keysToPlatformIds ?? new Dictionary<string, string>();
            return this;
        }

        /// <summary>Stable key to platform id; false when the platform or the key is not configured.</summary>
        public bool TryGetPlatformId(string stableKey, out string platformId)
        {
            platformId = null;

            if (CurrentPlatform == null || !platformToMap.TryGetValue(CurrentPlatform, out IReadOnlyDictionary<string, string> map))
            {
                return false;
            }

            if (!map.TryGetValue(stableKey, out platformId))
            {
                return false;
            }

            return true;
        }
    }
}

