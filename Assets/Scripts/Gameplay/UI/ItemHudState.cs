using System;
using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.UI;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.UI
{
    /// <summary>
    /// Adapter from the live gameplay objects to the HUD's narrow data contract. Keeping this
    /// translation here lets the view stay independent of placement and simulation classes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemHudState : MonoBehaviour, IItemHudState
    {
        [SerializeField] ItemPlacementController items;
        [SerializeField] SimulationRunner runner;

        static readonly IReadOnlyList<ItemType> AllTools = (ItemType[])Enum.GetValues(typeof(ItemType));

        public IReadOnlyList<ItemType> Tools => AllTools;

        public bool IsToolAvailable(ItemType type)
            => type == ItemType.Ramp || type == ItemType.Spring || type == ItemType.Cushion;

        public int Crates => runner != null && runner.Crates != null ? runner.Crates.Count : 0;

        public int Points => items != null && items.Budget != null ? items.Budget.Points : 0;

        public ItemType? SelectedTool => items != null ? items.SelectedType : null;

        public float ToolCooldownSeconds
        {
            get
            {
                if (items == null || !items.SelectedType.HasValue) return 0f;

                float remaining = 0f;
                foreach (GuidanceItem item in items.PlacedItems)
                {
                    if (item.Type == items.SelectedType.Value)
                    {
                        remaining = Mathf.Max(remaining, items.Cooldowns.Remaining(item.InstanceId));
                    }
                }

                return remaining;
            }
        }

        public void SelectTool(int index)
        {
            if (items == null || index < 0 || index >= AllTools.Count) return;
            if (IsToolAvailable(AllTools[index])) items.SelectType(AllTools[index]);
        }
    }
}
