using System;
using System.Collections.Generic;
using UnityEngine;
using BlindDelivery.Core.Balance;

namespace BlindDelivery.Gameplay.Balance
{
    /// <summary>
    /// Unity Inspector wrapper for <see cref="GameBalance"/>. One asset instance holds the
    /// game-wide tuning values. Core logic reads the pure C# <see cref="GameBalance"/> data,
    /// while this ScriptableObject exposes it to designers and to runtime bootstrap.
    ///
    /// Create one via the context menu: Assets &gt; Create &gt; Blind Delivery &gt; Game Balance.
    /// </summary>
    [CreateAssetMenu(menuName = "Blind Delivery/Game Balance", fileName = "GameBalance")]
    public sealed class GameBalanceAsset : ScriptableObject
    {
        [Header("Fall rules")]
        [Tooltip("Maximum fall distance in cells before the top crate is lost.")]
        [SerializeField] int safeFallHeightCells = 3;

        [Header("Simulation")]
        [Tooltip("Fixed simulation step in seconds (0.02s = 50 steps per second).")]
        [SerializeField] float fixedStepSeconds = 0.02f;

        [Header("Crates")]
        [Tooltip("Base number of crates the deliveryman starts each run with.")]
        [SerializeField] int baseCrateCount = 3;
        [Tooltip("Maximum number of crates the deliveryman can carry (Box Capacity raises to this).")]
        [SerializeField] int maxCrateCount = 5;

        [Header("Items")]
        [SerializeField] ItemTuning[] items =
        {
            new ItemTuning { type = ItemType.Ramp, moveCost = 5, cooldownSeconds = 1f },
            new ItemTuning { type = ItemType.Spring, moveCost = 10, cooldownSeconds = 2f },
            new ItemTuning { type = ItemType.TactileTile, moveCost = 5, cooldownSeconds = 0.5f },
            new ItemTuning { type = ItemType.AudioBeacon, moveCost = 8, cooldownSeconds = 3f },
            new ItemTuning { type = ItemType.WindBreak, moveCost = 8, cooldownSeconds = 1f },
            new ItemTuning { type = ItemType.Cushion, moveCost = 10, cooldownSeconds = 2f },
        };

        /// <summary>Builds the pure-C# <see cref="GameBalance"/> from this asset's fields.</summary>
        public GameBalance ToGameBalance()
        {
            var moveCosts = new Dictionary<ItemType, int>();
            var cooldowns = new Dictionary<ItemType, float>();
            foreach (ItemTuning tuning in items)
            {
                moveCosts[tuning.type] = tuning.moveCost;
                cooldowns[tuning.type] = tuning.cooldownSeconds;
            }

            return new GameBalance
            {
                SafeFallHeightCells = safeFallHeightCells,
                FixedStepSeconds = fixedStepSeconds,
                BaseCrateCount = baseCrateCount,
                MaxCrateCount = maxCrateCount,
                ItemMoveCosts = moveCosts,
                ItemCooldownSeconds = cooldowns,
            };
        }

        [Serializable]
        public struct ItemTuning
        {
            public ItemType type;
            [Min(0)] public int moveCost;
            [Min(0)] public float cooldownSeconds;
        }
    }
}
