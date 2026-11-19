using System;
using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;

namespace BlindDelivery.Core.Levels
{
    /// <summary>
    /// Pure, per-level configuration: point pool, crate target, prep time, walking speed and
    /// the tools this level grants. Built from the Gameplay LevelData asset so all rules that
    /// read it remain testable without a scene (GDD §6, Tech Plan §4.10).
    /// </summary>
    public sealed class LevelConfig
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>Fixed efficiency point pool for this level (GDD §5).</summary>
        public int StartingPoints { get; set; } = 100;

        /// <summary>Crates needed to deliver; normally 3 (GDD §5).</summary>
        public int CrateTarget { get; set; } = 3;

        /// <summary>Length of the prep countdown before the deliveryman starts walking.</summary>
        public int PrepCountdownSeconds { get; set; } = 3;

        /// <summary>Per-level walking speed in metres per second (GDD §3: 1.5–3.5).</summary>
        public float DeliverymanSpeed { get; set; } = 2.5f;

        /// <summary>Tools granted by this level, in HUD order.</summary>
        public IReadOnlyList<ItemDefinitionData> Tools { get; set; } = Array.Empty<ItemDefinitionData>();

        /// <summary>Items the player can place and drag; these become HUD slots.</summary>
        public IReadOnlyList<ItemDefinitionData> PlayerTools => Filter(ItemUsage.PlayerTool);

        /// <summary>Level-design-only items (wind zones, etc.); never shown in the player HUD.</summary>
        public IReadOnlyList<ItemDefinitionData> Fixtures => Filter(ItemUsage.LevelFixture);

        IReadOnlyList<ItemDefinitionData> Filter(ItemUsage usage)
        {
            var result = new List<ItemDefinitionData>();
            foreach (ItemDefinitionData tool in Tools)
            {
                if (tool != null && tool.Usage == usage) result.Add(tool);
            }

            return result;
        }

        public bool Grants(ItemType type)
        {
            foreach (ItemDefinitionData tool in Tools)
            {
                if (tool != null && tool.Type == type) return true;
            }

            return false;
        }
    }
}
