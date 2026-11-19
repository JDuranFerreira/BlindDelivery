namespace BlindDelivery.Core.Sensory
{
    /// <summary>
    /// Data-only result of one sensory scan. The scan itself is data so the validator and EditMode
    /// tests can read it without audio or haptics (Tech Plan §4.4).
    /// </summary>
    public readonly struct SensoryReading
    {
        public SensoryReading(float ledgeDistanceCells, float wallDistanceCells, float beaconDirection, float warningIntensity, bool slowTrigger)
        {
            LedgeDistanceCells = ledgeDistanceCells;
            WallDistanceCells = wallDistanceCells;
            BeaconDirection = beaconDirection;
            WarningIntensity = warningIntensity;
            SlowTrigger = slowTrigger;
        }

        /// <summary>Distance to the first ledge ahead in cells; 0 means no ledge in range.</summary>
        public float LedgeDistanceCells { get; }

        /// <summary>Distance to the first wall ahead in cells; 0 means clear.</summary>
        public float WallDistanceCells { get; }

        /// <summary>-1 = beacon to the left, +1 = to the right, 0 = none.</summary>
        public float BeaconDirection { get; }

        /// <summary>0..1, grows as the courier approaches an edge. Drives hum pitch and haptics.</summary>
        public float WarningIntensity { get; }

        public bool SlowTrigger { get; }

        public bool HasLedge => LedgeDistanceCells > 0f;

        public bool HasWall => WallDistanceCells > 0f;
    }
}
