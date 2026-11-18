namespace BlindDelivery.Core.Movement
{
    /// <summary>States of the deliveryman's walking controller (tech plan 4.2).</summary>
    public enum MovementState
    {
        Idle,
        Walking,
        Turning,
        Falling,
        Delivered,
    }
}
