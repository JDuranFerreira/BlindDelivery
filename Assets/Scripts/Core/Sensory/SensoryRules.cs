using System;

namespace BlindDelivery.Core.Sensory
{
    /// <summary>
    /// Pure interpretation of a sensory scan (GDD §3, Tech Plan §4.4). Distances arrive in cells
    /// from the Gameplay probe; nothing here touches physics, audio or the player loop.
    /// </summary>
    public static class SensoryRules
    {
        public static SensoryReading Evaluate(
            float ledgeDistanceCells,
            float wallDistanceCells,
            float beaconDirection,
            float warningRangeCells,
            float slowRangeCells)
        {
            if (ledgeDistanceCells < 0f) throw new ArgumentOutOfRangeException(nameof(ledgeDistanceCells));
            if (wallDistanceCells < 0f) throw new ArgumentOutOfRangeException(nameof(wallDistanceCells));
            if (warningRangeCells <= 0f) throw new ArgumentOutOfRangeException(nameof(warningRangeCells));

            float warning = Intensity(ledgeDistanceCells, warningRangeCells);
            bool slow = ledgeDistanceCells > 0f && slowRangeCells > 0f && ledgeDistanceCells <= slowRangeCells;
            float direction = Math.Max(-1f, Math.Min(1f, beaconDirection));

            return new SensoryReading(ledgeDistanceCells, wallDistanceCells, direction, warning, slow);
        }

        /// <summary>Linear proximity: 0 at/above the range, 1 at the edge itself.</summary>
        public static float Intensity(float distanceCells, float rangeCells)
        {
            if (distanceCells <= 0f || rangeCells <= 0f || distanceCells > rangeCells) return 0f;
            return 1f - distanceCells / rangeCells;
        }

        /// <summary>Slow-down strength grows with upgrade level: 1 = 0.5x, 2 = 0.6x, 3 = 0.7x speed.</summary>
        public static float SlowFactorForLevel(int level)
        {
            if (level <= 0) return 1f;
            return Math.Min(0.5f + (level - 1) * 0.1f, 0.8f);
        }
    }
}
