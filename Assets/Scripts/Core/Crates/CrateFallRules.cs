namespace BlindDelivery.Core.Crates
{
    /// <summary>
    /// Pure maths for a dropped crate's visual fall (GDD §3, "Dropped"). Kept in Core so the
    /// drop and landing decision is testable without a scene or the physics world.
    /// </summary>
    public static class CrateFallRules
    {
        /// <summary>Velocity after one step of gravity; downward is negative.</summary>
        public static float Integrate(float velocity, float gravity, float deltaTime)
            => velocity - gravity * deltaTime;

        /// <summary>A crate settles when it is descending and solid ground is just below it.</summary>
        public static bool HasLanded(bool solidBelow, bool descending)
            => descending && solidBelow;

        /// <summary>True once a courier is past the level's lowest solid plus a margin.</summary>
        public static bool IsOutOfWorld(float y, float levelBottomY, float margin)
            => y < levelBottomY - margin;
    }
}
