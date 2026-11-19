using System;

namespace BlindDelivery.Core.Scoring
{
    /// <summary>
    /// Pure crate-to-star rules (GDD §5). One crate is one star up to the target of 3; the 4th
    /// crate is one gold bonus star, the 5th gives the "3 stars plus plus" rating. Nothing except
    /// delivered crates affects stars, and stars are never deducted.
    /// </summary>
    public static class StarCalculator
    {
        public const int MaxNormalStars = 3;
        public const int MaxCrates = 5;

        public static StarResult Calculate(int cratesDelivered, int target = MaxNormalStars, int maxCrates = MaxCrates)
        {
            if (cratesDelivered < 0) throw new ArgumentOutOfRangeException(nameof(cratesDelivered));
            if (target <= 0) throw new ArgumentOutOfRangeException(nameof(target));
            if (maxCrates < target) throw new ArgumentOutOfRangeException(nameof(maxCrates));

            int stars = Math.Min(cratesDelivered, target);
            int capacity = maxCrates - target;
            int bonus = Math.Max(0, Math.Min(cratesDelivered - target, capacity));
            bool plusPlus = capacity > 0 && cratesDelivered >= maxCrates;

            return new StarResult(cratesDelivered, stars, bonus, plusPlus);
        }
    }
}
