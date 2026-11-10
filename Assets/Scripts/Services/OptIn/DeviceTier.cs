using UnityEngine;

namespace Template.Services.OptIn
{
    /// <summary>Which qualification bucket a device belongs to. Low is the cautious default.</summary>
    public enum DeviceTier
    {
        Low,
        Mid,
        High,
    }

    /// <summary>
    /// Bonus system 6, half two (opt-in file, not base wiring): device tier detection. The
    /// classifier is a pure function over the three numbers Unity exposes, so the buckets are
    /// testable; <see cref="DeviceTierClassifier.Current"/> reads SystemInfo from the running
    /// device. A game uses the result for quality presets, not for gameplay numbers.
    /// </summary>
    public static class DeviceTierClassifier
    {
        /// <summary>
        /// RAM is the main bucket, CPU cores and graphics memory the tie-breakers. Thresholds are
        /// deliberately round: 2/4 GB of system memory, 4/8 cores, 1 GB of graphics memory.
        /// </summary>
        public static DeviceTier Classify(int systemMemoryMb, int processorCount, int graphicsMemoryMb)
        {
            int score = 0;
            if (systemMemoryMb >= 2000)
            {
                score++;
            }

            if (systemMemoryMb >= 4000)
            {
                score++;
            }

            if (processorCount >= 4)
            {
                score++;
            }

            if (processorCount >= 8)
            {
                score++;
            }

            if (graphicsMemoryMb >= 1000)
            {
                score++;
            }

            if (score >= 4)
            {
                return DeviceTier.High;
            }

            return score >= 2 ? DeviceTier.Mid : DeviceTier.Low;
        }

        /// <summary>The running device's tier, straight from SystemInfo.</summary>
        public static DeviceTier Current => Classify(
            SystemInfo.systemMemorySize,
            SystemInfo.processorCount,
            SystemInfo.graphicsMemorySize);
    }
}
