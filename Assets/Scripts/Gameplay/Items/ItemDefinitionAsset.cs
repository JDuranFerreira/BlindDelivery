using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;
using UnityEngine;

namespace BlindDelivery.Gameplay.Items
{
    /// <summary>
    /// One authored tool: the prefab the factory instantiates plus its per-level cost and
    /// cooldown. This is where a new item type is registered — no switch in a controller.
    /// Create via Assets &gt; Create &gt; Blind Delivery &gt; Item Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Blind Delivery/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinitionAsset : ScriptableObject
    {
        [SerializeField] ItemType type;
        [SerializeField] ItemUsage usage = ItemUsage.PlayerTool;
        [SerializeField] GameObject prefab;
        [SerializeField, Min(0)] int moveCost;
        [SerializeField, Min(0)] float cooldownSeconds;

        public ItemType Type => type;

        public ItemUsage Usage => usage;

        public GameObject Prefab => prefab;

        public ItemDefinitionData ToData()
        {
            return new ItemDefinitionData
            {
                Type = type,
                Usage = usage,
                MoveCost = moveCost,
                CooldownSeconds = cooldownSeconds,
            };
        }

        /// <summary>Used by authoring/tests to build a definition without a serialized scene.</summary>
        public static ItemDefinitionAsset Create(ItemType itemType, GameObject itemPrefab, int cost, float cooldown)
            => Create(itemType, ItemUsage.PlayerTool, itemPrefab, cost, cooldown);

        public static ItemDefinitionAsset Create(
            ItemType itemType, ItemUsage itemUsage, GameObject itemPrefab, int cost, float cooldown)
        {
            ItemDefinitionAsset asset = CreateInstance<ItemDefinitionAsset>();
            asset.type = itemType;
            asset.usage = itemUsage;
            asset.prefab = itemPrefab;
            asset.moveCost = cost;
            asset.cooldownSeconds = cooldown;
            return asset;
        }
    }
}
