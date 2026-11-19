using System.Collections.Generic;
using BlindDelivery.Core.Balance;

namespace BlindDelivery.Core.UI
{
    /// <summary>
    /// Read/write state the HUD renders. The view depends on this contract, not on the gameplay
    /// classes that happen to own the data today.
    /// </summary>
    public interface IItemHudState
    {
        int Crates { get; }

        int Points { get; }

        /// <summary>Full point pool for the level, for the HUD meter.</summary>
        int MaxPoints { get; }

        /// <summary>True during the prep countdown, before the courier starts walking.</summary>
        bool IsPreparing { get; }

        /// <summary>Seconds left in prep, for the HUD countdown label.</summary>
        float PrepRemainingSeconds { get; }

        ItemType? SelectedTool { get; }

        float ToolCooldownSeconds { get; }

        /// <summary>Selected tool's cooldown as a 0..1 fraction of its tuned duration.</summary>
        float ToolCooldownRatio { get; }

        /// <summary>Cooldown of one tool as a 0..1 fraction, for per-slot HUD feedback.</summary>
        float CooldownRatioFor(ItemType type);

        /// <summary>Tool slots in the order shown in the HUD.</summary>
        IReadOnlyList<ItemType> Tools { get; }

        bool IsToolAvailable(ItemType type);

        void SelectTool(int index);
    }
}
