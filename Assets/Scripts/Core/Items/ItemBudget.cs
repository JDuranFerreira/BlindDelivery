using System;

namespace BlindDelivery.Core.Items
{
    /// <summary>
    /// The level's efficiency point pool. First placements are free; only moving an item costs
    /// points, and the pool floors at zero (GDD §5).
    /// </summary>
    public sealed class ItemBudget
    {
        int points;

        public ItemBudget(int startingPoints)
        {
            Reset(startingPoints);
        }

        public int Points => points;

        /// <summary>Full pool for this level; the HUD meter scales remaining points against it.</summary>
        public int StartingPoints { get; private set; }

        public void Reset(int startingPoints)
        {
            if (startingPoints < 0) throw new ArgumentOutOfRangeException(nameof(startingPoints));
            points = startingPoints;
            StartingPoints = startingPoints;
        }

        public bool CanAfford(int cost) => cost >= 0 && points >= cost;

        /// <summary>Spends move cost when affordable. Returns false and changes nothing otherwise.</summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (!CanAfford(cost)) return false;

            points -= cost;
            return true;
        }
    }
}
