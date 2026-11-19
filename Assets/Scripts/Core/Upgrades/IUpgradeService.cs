namespace BlindDelivery.Core.Upgrades
{
    public enum UpgradeKind
    {
        EcholocationSlowdown,
        HapticSensitivity,
        BoxCapacity,
    }

    /// <summary>
    /// Minimal ownership gate. Level 0 means not owned; the slow-down checks level 1+ before it
    /// touches the movement state machine. Persistence comes with the progression milestone.
    /// </summary>
    public interface IUpgradeService
    {
        int Level(UpgradeKind kind);

        bool IsUnlocked(UpgradeKind kind, int level);

        void SetLevel(UpgradeKind kind, int level);
    }
}
