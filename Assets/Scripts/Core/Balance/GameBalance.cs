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

        /// <summary>Forward walking speed in metres per second (GDD range: 1.5–3.5).</summary>
        public float DeliverymanSpeed { get; set; } = 2.5f;

        /// <summary>Gravity magnitude used by both the world and jumppad launch math.</summary>
        public float Gravity { get; set; } = 9.81f;

        /// <summary>Spring launch height in level cells (tuned to 5 for this level).</summary>
        public float JumppadLaunchHeightCells { get; set; } = 5f;

        /// <summary>Forward speed of a jumppad launch, as a fraction of walking speed.</summary>
        public float JumppadForwardSpeedFactor { get; set; } = 0.5f;

        /// <summary>Bubble Gum slows the courier to this fraction of walking speed.</summary>
        public float BubbleGumSpeedFactor { get; set; } = 0.5f;

        /// <summary>Oil speeds the courier up to this multiple of walking speed.</summary>
        public float OilSpeedFactor { get; set; } = 1.5f;

        /// <summary>How long a speed effect lasts after leaving the object.</summary>
        public float SpeedEffectSeconds { get; set; } = 1f;

        /// <summary>Base number of crates the deliveryman starts each run with.</summary>
        public int BaseCrateCount { get; set; } = 3;

        /// <summary>Maximum number of crates the deliveryman can carry (Box Capacity raises to this).</summary>
        public int MaxCrateCount { get; set; } = 5;

        /// <summary>Point move cost per item type. Keyed by item type.</summary>
        public IReadOnlyDictionary<ItemType, int> ItemMoveCosts { get; set; } =
            new Dictionary<ItemType, int>
            {
                [ItemType.Ramp] = 5,
                [ItemType.JumpPad] = 10,
                [ItemType.BubbleGum] = 5,
                [ItemType.AudioBeacon] = 8,
                [ItemType.WindBreak] = 8,
                [ItemType.Cardbox] = 10,
                [ItemType.Bridge] = 5,
                [ItemType.Crate] = 2,
                [ItemType.Oil] = 5,
                [ItemType.Spikes] = 0,
                [ItemType.FallingObject] = 0,
            };

        /// <summary>Cooldown length in seconds per item type. Keyed by item type.</summary>
        public IReadOnlyDictionary<ItemType, float> ItemCooldownSeconds { get; set; } =
            new Dictionary<ItemType, float>
            {
                [ItemType.Ramp] = 1f,
                [ItemType.JumpPad] = 2f,
                [ItemType.BubbleGum] = 0.5f,
                [ItemType.AudioBeacon] = 3f,
                [ItemType.WindBreak] = 1f,
                [ItemType.Cardbox] = 2f,
                [ItemType.Bridge] = 1f,
                [ItemType.Crate] = 0.5f,
                [ItemType.Oil] = 0.5f,
                [ItemType.Spikes] = 0f,
                [ItemType.FallingObject] = 0f,
            };
    }

    /// <summary>
    /// Canonical object types shared between balance data and definition assets (GDD §4).
    /// Values 0–5 keep their serialized meaning; new objects are appended.
    /// </summary>
    public enum ItemType
    {
        Ramp,          // Guidance: 1-cell height gap
        JumpPad,       // Guidance: launch 5 cells up
        BubbleGum,     // Guidance: slow the courier
        AudioBeacon,   // Level Object: 1s directional ping
        WindBreak,     // Level Object: wind wall, launches 5 cells up
        Cardbox,       // Guidance: cushions long drops
        Bridge,        // Guidance: 1-cell width gap
        Crate,         // Guidance: close a gap or block a path
        Oil,           // Guidance: speed up the courier
        Spikes,        // Level Object: kills the courier
        FallingObject, // Level Object: falling trigger, loses a crate
    }
}
