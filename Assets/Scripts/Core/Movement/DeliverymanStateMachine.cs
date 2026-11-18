using System;

namespace BlindDelivery.Core.Movement
{
    /// <summary>
    /// Pure C# walking controller. It never touches Unity or physics APIs: it reads a sensor result,
    /// writes movement commands through <see cref="IMotor"/>, and exposes the current state.
    /// Prep countdown, crates and upgrades are separate concerns and are not handled here.
    /// </summary>
    public sealed class DeliverymanStateMachine : IMovementStateProvider
    {
        readonly float speed;
        readonly float turnDuration;
        readonly float cellSize;

        float turnRemaining;
        float fallStartY;
        MovementState state = MovementState.Idle;
        int facing = 1;

        /// <summary>Raised once when a fall ends, with the drop distance in level cells.</summary>
        public event Action<float> FallLanded;

        public DeliverymanStateMachine(float speed, float turnDuration = 0.12f, float cellSize = 1f)
        {
            if (speed <= 0f) throw new ArgumentOutOfRangeException(nameof(speed));
            if (turnDuration < 0f) throw new ArgumentOutOfRangeException(nameof(turnDuration));
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));

            this.speed = speed;
            this.turnDuration = turnDuration;
            this.cellSize = cellSize;
        }

        public MovementState State => state;

        public int Facing => facing;

        public void Tick(float deltaTime, MovementSensorResult sensor, IMotor motor)
        {
            if (motor == null) throw new ArgumentNullException(nameof(motor));
            motor.ReadPosition(out float x, out float y);
            motor.ReadVelocity(out float vx, out float vy);

            switch (state)
            {
                case MovementState.Idle:
                    // No prep countdown data yet: begin walking on the first tick.
                    state = MovementState.Walking;
                    motor.SetVelocity(facing * speed, 0f);
                    break;

                case MovementState.Walking:
                    if (sensor.WallAhead)
                    {
                        state = MovementState.Turning;
                        turnRemaining = turnDuration;
                        motor.SetVelocity(0f, 0f);
                    }
                    else if (!sensor.GroundAhead && !sensor.GroundBelow)
                    {
                        state = MovementState.Falling;
                        fallStartY = y;
                        motor.SetVelocity(facing * speed * 0.5f, vy);
                    }
                    else
                    {
                        motor.SetVelocity(facing * speed, 0f);
                    }
                    break;

                case MovementState.Turning:
                    motor.SetVelocity(0f, 0f);
                    turnRemaining -= deltaTime;
                    if (turnRemaining <= 0f)
                    {
                        facing = -facing;
                        state = MovementState.Walking;
                    }
                    break;

                case MovementState.Falling:
                    // Keep gravity's vertical velocity; only horizontal motion is controlled here.
                    motor.SetVelocity(facing * speed * 0.5f, vy);
                    if (sensor.GroundBelow && vy <= 0.01f && y < fallStartY - 0.05f)
                    {
                        float cells = (float)Math.Max(0d, (fallStartY - y) / cellSize);
                        FallLanded?.Invoke(cells);
                        state = MovementState.Walking;
                    }
                    break;

                case MovementState.Delivered:
                    motor.SetVelocity(0f, 0f);
                    break;
            }
        }

        /// <summary>Stops the controller at the end point. The crate/delivery event follows elsewhere.</summary>
        public void Deliver(IMotor motor)
        {
            if (motor == null) throw new ArgumentNullException(nameof(motor));
            state = MovementState.Delivered;
            motor.SetVelocity(0f, 0f);
        }
    }
}
