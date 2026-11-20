using System;
using System.Collections.Generic;

namespace BlindDelivery.Core.Save
{
    /// <summary>One level's best results. Plain data so JsonUtility can serialize it.</summary>
    [Serializable]
    public sealed class LevelResultEntry
    {
        public string levelId;
        public float bestTimeSeconds;
        public int bestStars;
    }

    /// <summary>
    /// Durable per-level results (GDD §5 replay loop). Stars never decrease; best time is the
    /// shortest successful run. Stored under a single save key.
    /// </summary>
    [Serializable]
    public sealed class LevelResultsData
    {
        public List<LevelResultEntry> levels = new List<LevelResultEntry>();

        /// <summary>Returns true when this run improved the stored time or star count.</summary>
        public bool Record(string levelId, float timeSeconds, int stars)
        {
            if (string.IsNullOrEmpty(levelId)) return false;

            LevelResultEntry entry = null;
            foreach (LevelResultEntry candidate in levels)
            {
                if (candidate != null && candidate.levelId == levelId)
                {
                    entry = candidate;
                    break;
                }
            }

            if (entry == null)
            {
                entry = new LevelResultEntry
                {
                    levelId = levelId,
                    bestTimeSeconds = float.MaxValue,
                    bestStars = -1,
                };
                levels.Add(entry);
            }

            bool improved = false;
            if (timeSeconds > 0f && timeSeconds < entry.bestTimeSeconds)
            {
                entry.bestTimeSeconds = timeSeconds;
                improved = true;
            }
            if (stars > entry.bestStars)
            {
                entry.bestStars = stars;
                improved = true;
            }

            return improved;
        }

        public float BestTime(string levelId)
        {
            foreach (LevelResultEntry entry in levels)
            {
                if (entry != null && entry.levelId == levelId) return entry.bestTimeSeconds;
            }
            return float.MaxValue;
        }
    }
}
