namespace BlindDelivery.Core.Movement
{
    /// <summary>What the movement controller needs to know about the world ahead of the deliveryman.</summary>
    public struct MovementSensorResult
    {
        /// <summary>A wall or blocking object is directly in front.</summary>
        public bool WallAhead;

        /// <summary>There is solid ground just in front of the leading foot (used to spot ledges).</summary>
        public bool GroundAhead;

        /// <summary>There is solid ground directly below the deliveryman.</summary>
        public bool GroundBelow;
    }
}
