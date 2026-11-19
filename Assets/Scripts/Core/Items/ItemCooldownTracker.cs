using System;
using System.Collections.Generic;

namespace BlindDelivery.Core.Items
{
    /// <summary>
    /// Per-instance item cooldowns. Time is advanced by the game in seconds; placements and moves
    /// start a cooldown so an item cannot be warped again immediately (GDD §4).
    /// </summary>
    public sealed class ItemCooldownTracker
    {
        const float ExpiryEpsilon = 1e-6f;

        readonly Dictionary<int, float> remaining = new Dictionary<int, float>();

        public bool IsActive(int instanceId) => Remaining(instanceId) > 0f;

        public float Remaining(int instanceId)
        {
            return remaining.TryGetValue(instanceId, out float value) ? value : 0f;
        }

        public void Start(int instanceId, float seconds)
        {
            if (instanceId < 0) throw new ArgumentOutOfRangeException(nameof(instanceId));
            if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));

            remaining[instanceId] = seconds;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (remaining.Count == 0) return;

            // Mono's Dictionary counts value assignment as a modification, so collect changes
            // first and apply them after enumeration.
            var snapshot = new List<KeyValuePair<int, float>>(remaining);
            var finished = new List<int>();
            var updated = new List<KeyValuePair<int, float>>();
            foreach (KeyValuePair<int, float> entry in snapshot)
            {
                float next = entry.Value - deltaTime;
                if (next <= ExpiryEpsilon)
                {
                    finished.Add(entry.Key);
                }
                else
                {
                    updated.Add(new KeyValuePair<int, float>(entry.Key, next));
                }
            }

            foreach (KeyValuePair<int, float> entry in updated) remaining[entry.Key] = entry.Value;
            foreach (int id in finished) remaining.Remove(id);
        }

        public void Clear(int instanceId) => remaining.Remove(instanceId);
    }
}
