namespace BlindDelivery.Core.Movement
{
    /// <summary>Movement commands the state machine sends to a physics body. No Unity types live here.</summary>
    public interface IMotor
    {
        void SetVelocity(float x, float y);

        void ReadVelocity(out float x, out float y);

        void ReadPosition(out float x, out float y);
    }
}
