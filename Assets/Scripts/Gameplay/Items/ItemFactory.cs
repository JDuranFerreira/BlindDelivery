using System;
using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using UnityEngine;

namespace BlindDelivery.Gameplay.Items
{
    /// <summary>
    /// Creation point for every guidance object that appears in the world. Callers select and
    /// instantiate items by <see cref="ItemType"/> without knowing a prefab or the build steps.
    /// Adding an item type means registering one entry here, not editing a switch in the
    /// placement controller, the HUD and the interaction system separately (GDD §4, SOLID OCP).
    /// </summary>
    public sealed class ItemFactory
    {
        readonly Dictionary<ItemType, GameObject> prefabs = new Dictionary<ItemType, GameObject>();
        readonly List<ItemType> supported = new List<ItemType>();

        public ItemFactory(IEnumerable<ItemFactoryEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            foreach (ItemFactoryEntry entry in entries)
            {
                if (entry.Prefab == null || prefabs.ContainsKey(entry.Type)) continue;
                prefabs.Add(entry.Type, entry.Prefab);
                supported.Add(entry.Type);
            }
        }

        /// <summary>Tool slots in registration order — derived, never a hardcoded list.</summary>
        public IReadOnlyList<ItemType> SupportedTypes => supported;

        public bool CanCreate(ItemType type) => prefabs.ContainsKey(type);

        /// <summary>Instantiates one item, assigns its runtime id and returns its gameplay facade.</summary>
        public GuidanceItem Create(ItemType type, Vector3 position, Transform parent, int instanceId)
        {
            if (!prefabs.TryGetValue(type, out GameObject prefab))
                throw new ArgumentException("No prefab registered for " + type + ".", nameof(type));

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, parent);
            instance.name = prefab.name + " (Placed)";

            GuidanceItem item = instance.GetComponent<GuidanceItem>();
            if (item == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException("Prefab '" + prefab.name + "' has no GuidanceItem component.");
            }

            item.AssignId(instanceId);
            return item;
        }
    }

    /// <summary>One prefab registration for the factory.</summary>
    [Serializable]
    public struct ItemFactoryEntry
    {
        public ItemType Type;
        public GameObject Prefab;

        public ItemFactoryEntry(ItemType type, GameObject prefab)
        {
            Type = type;
            Prefab = prefab;
        }
    }
}
