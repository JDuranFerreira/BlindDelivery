namespace BlindDelivery.Core.Movement
{
    /// <summary>Probes the level around a position. Implementing types decide how to query the world.</summary>
    public interface IMovementSensor
    {
        MovementSensorResult Scan(float x, float y, int facing);
    }
}
