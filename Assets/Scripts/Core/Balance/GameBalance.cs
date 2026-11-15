using System;
using System.Collections.Generic;

namespace BlindDelivery.Core.Balance
{
    /// <summary>
    /// Game-wide tuning data: Safe Fall Height, fixed simulation step, item point costs,
    /// cooldown lengths and crate limits.
    ///
    /// Pure C# with no Unity types, so Core logic and unit tests can read it without a scene.
    /// Balance changes edit this class (or its ScriptableObject wrapper in Gameplay), never
    /// level data. Values here are defaults; designers tune them via the GameBalance asset.
    /// </summary>
    public sealed class GameBalance
    {
        /// <summary>Maximum fall distance in cells before the top crate is lost (default 3).</summary>
        public int SafeFallHeightCells { get; set; } = 3;

        /// <summary>Fixed simulation step in seconds (0.02s = 50 steps per second).</summary>
        public float FixedStepSeconds { get; set; } = 0.02f;

        /// <summary>Base number of crates the boy starts each run with.</summary>
        public int BaseCrateCount { get; set; } = 3;

        /// <summary>Maximum number of crates the boy can carry (Box Capacity raises to this).</summary>
        public int MaxCrateCount { get; set; } = 5;

        /// <summary>Point move cost per item type. Keyed by item type.</summary>
        public IReadOnlyDictionary<ItemType, int> ItemMoveCosts { get; set; } =
            new Dictionary<ItemType, int>
            {
                [ItemType.Ramp] = 5,
                [ItemType.Spring] = 10,
                [ItemType.TactileTile] = 5,
                [ItemType.AudioBeacon] = 8,
                [ItemType.WindBreak] = 8,
                [ItemType.Cushion] = 10,
            };

        /// <summary>Cooldown length in seconds per item type. Keyed by item type.</summary>
        public IReadOnlyDictionary<ItemType, float> ItemCooldownSeconds { get; set; } =
            new Dictionary<ItemType, float>
            {
                [ItemType.Ramp] = 1f,
                [ItemType.Spring] = 2f,
                [ItemType.TactileTile] = 0.5f,
                [ItemType.AudioBeacon] = 3f,
                [ItemType.WindBreak] = 1f,
                [ItemType.Cushion] = 2f,
            };
    }

    /// <summary>Canonical item types shared between balance data and ItemDefinition assets.</summary>
    public enum ItemType
    {
        Ramp,
        Spring,
        TactileTile,
        AudioBeacon,
        WindBreak,
        Cushion,
    }
}
