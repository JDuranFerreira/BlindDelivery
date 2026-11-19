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

        ItemType? SelectedTool { get; }

        float ToolCooldownSeconds { get; }

        /// <summary>Tool slots in the order shown in the HUD.</summary>
        IReadOnlyList<ItemType> Tools { get; }

        bool IsToolAvailable(ItemType type);

        void SelectTool(int index);
    }
}
