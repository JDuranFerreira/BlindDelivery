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

        void Replay();
    }
}
