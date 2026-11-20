using BlindDelivery.Core.Balance;

namespace BlindDelivery.Core.Items
{
    /// <summary>
    /// Pure definition of one tool granted by a level: which item it is and its authored cost
    /// and cooldown. Unity prefabs stay in the Gameplay ScriptableObject; Core sees only data.
    /// </summary>
    public sealed class ItemDefinitionData
    {
        public ItemType Type { get; set; }

        /// <summary>Player-draggable tool or level-design-only fixture.</summary>
        public ItemUsage Usage { get; set; } = ItemUsage.Guidance;

        public int MoveCost { get; set; }

        public float CooldownSeconds { get; set; }
    }
}
