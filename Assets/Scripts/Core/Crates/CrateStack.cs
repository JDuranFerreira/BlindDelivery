using System;

namespace BlindDelivery.Core.Crates
{
    /// <summary>
    /// Minimal crate state needed by item interactions: a long drop loses the top crate unless the
    /// deliveryman landed on a cushion (cardbox). Full dropped/lost crate physics comes later.
    /// </summary>
    public sealed class CrateStack
    {
        int count;

        public CrateStack(int count)
        {
            Reset(count);
        }

        public int Count => count;

        public void Reset(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            count = value;
        }

        public bool LoseOne()
        {
            if (count <= 0) return false;
            count--;
            return true;
        }

        /// <summary>Returns true when a crate was actually lost.</summary>
        public bool ApplyFall(float fallCells, int safeFallHeightCells, bool cushioned)
        {
            if (fallCells < 0f) throw new ArgumentOutOfRangeException(nameof(fallCells));
            if (safeFallHeightCells < 0) throw new ArgumentOutOfRangeException(nameof(safeFallHeightCells));

            return !cushioned && fallCells > safeFallHeightCells && LoseOne();
        }
    }
}
