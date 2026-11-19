using System;

namespace BlindDelivery.Core.Scoring
{
    /// <summary>
    /// Immutable outcome of one run. Normal stars come only from delivered crates; bonus stars
    /// (gold in the UI) are separate so players without the Box Capacity upgrade never feel
    /// punished. GDD §5.
    /// </summary>
    public readonly struct StarResult
    {
        public StarResult(int crates, int stars, int bonusStars, bool plusPlus)
        {
            Crates = crates;
            Stars = stars;
            BonusStars = bonusStars;
            PlusPlus = plusPlus;
        }

        public int Crates { get; }

        /// <summary>Normal stars, 0 (fail) through 3.</summary>
        public int Stars { get; }

        /// <summary>Gold bonus stars for the 4th and 5th crate, 0 through 2.</summary>
        public int BonusStars { get; }

        /// <summary>True at full capacity: the "3 stars plus plus" top rating.</summary>
        public bool PlusPlus { get; }

        public bool Failed => Stars == 0;

        public string Rating => Failed ? "FAILED" : Stars + " / 3 STARS";

        public string BonusText => PlusPlus ? "++" : BonusStars > 0 ? "+" : string.Empty;
    }
}
