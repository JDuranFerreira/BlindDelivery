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

        // Availability comes from the factory, so adding a tool never edits this adapter.
        public bool IsToolAvailable(ItemType type) => items != null && items.IsToolSupported(type);

        public float ToolCooldownRatio => SelectedTool.HasValue ? CooldownRatioFor(SelectedTool.Value) : 0f;

        public float CooldownRatioFor(ItemType type)
        {
            if (items == null || items.Cooldowns == null) return 0f;
            float duration = items.CooldownSeconds(type);
            if (duration <= 0f) return 0f;

            float remaining = 0f;
            foreach (GuidanceItem item in items.PlacedItems)
            {
                if (item.Type == type) remaining = Mathf.Max(remaining, items.Cooldowns.Remaining(item.InstanceId));
            }

            return Mathf.Clamp01(remaining / duration);
        }

        public int Crates => runner != null && runner.Crates != null ? runner.Crates.Count : 0;

        public int Points => items != null && items.Budget != null ? items.Budget.Points : 0;

        public int MaxPoints => items != null && items.Budget != null ? items.Budget.StartingPoints : 0;

        public bool IsPreparing => runner != null && runner.IsPreparing;

        public float PrepRemainingSeconds => runner != null ? runner.PrepRemaining : 0f;

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
