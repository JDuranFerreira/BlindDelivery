using BlindDelivery.Core.Scoring;

namespace BlindDelivery.Core.UI
{
    /// <summary>
    /// What the result screen renders. Kept as a Core contract so the view never reaches into
    /// gameplay objects; a single scene controller implements it.
    /// </summary>
    public interface IResultState
    {
        bool HasResult { get; }

        StarResult Result { get; }

        /// <summary>Points left when the run ended; the efficiency score (GDD §5).</summary>
        int Score { get; }

        /// <summary>Run time in seconds, recorded when the result was produced (GDD §5).</summary>
        float ElapsedSeconds { get; }

        /// <summary>Best recorded time for this level, or 0 when none is saved yet.</summary>
        float BestSeconds { get; }

        void Replay();
    }
}
