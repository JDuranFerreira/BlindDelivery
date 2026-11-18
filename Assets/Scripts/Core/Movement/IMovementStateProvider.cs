namespace BlindDelivery.Core.Movement
{
    /// <summary>Read-only movement facts a view can render without depending on the simulation.</summary>
    public interface IMovementStateProvider
    {
        int Facing { get; }

        MovementState State { get; }
    }
}
