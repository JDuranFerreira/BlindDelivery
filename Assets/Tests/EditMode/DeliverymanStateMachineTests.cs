using BlindDelivery.Core.Movement;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class DeliverymanStateMachineTests
    {
        sealed class FakeMotor : IMotor
        {
            public float X, Y, Vx, Vy;

            public void SetVelocity(float x, float y) { Vx = x; Vy = y; }

            public void ReadVelocity(out float x, out float y) { x = Vx; y = Vy; }

            public void ReadPosition(out float x, out float y) { x = X; y = Y; }
        }

        static MovementSensorResult Grounded => new MovementSensorResult
        {
            WallAhead = false,
            GroundAhead = true,
            GroundBelow = true,
        };

        [Test]
        public void FirstTick_LeavesIdleAndWalksForwardAtConfiguredSpeed()
        {
            var machine = new DeliverymanStateMachine(2.5f);
            var motor = new FakeMotor();

            machine.Tick(0.02f, Grounded, motor);

            Assert.That(machine.State, Is.EqualTo(MovementState.Walking));
            Assert.That(motor.Vx, Is.EqualTo(2.5f));
            Assert.That(machine.Facing, Is.EqualTo(1));
        }

        [Test]
        public void WallAhead_TurnsAroundAndWalksTheOtherWay()
        {
            var machine = new DeliverymanStateMachine(2f, turnDuration: 0.1f);
            var motor = new FakeMotor();
            machine.Tick(0.02f, Grounded, motor);

            var wall = new MovementSensorResult { WallAhead = true, GroundAhead = true, GroundBelow = true };
            machine.Tick(0.02f, wall, motor);
            Assert.That(machine.State, Is.EqualTo(MovementState.Turning));
            Assert.That(motor.Vx, Is.Zero);

            for (int i = 0; i < 6; i++) machine.Tick(0.02f, Grounded, motor);
            Assert.That(machine.State, Is.EqualTo(MovementState.Walking));
            Assert.That(machine.Facing, Is.EqualTo(-1));
            Assert.That(motor.Vx, Is.EqualTo(-2f));
        }

        [Test]
        public void Ledge_EntersFallingAndLandingEmitsFallDistance()
        {
            var machine = new DeliverymanStateMachine(2f, cellSize: 1f);
            var motor = new FakeMotor { Y = 4f, Vy = -1f };
            machine.Tick(0.02f, Grounded, motor);

            var ledge = new MovementSensorResult();
            machine.Tick(0.02f, ledge, motor);
            Assert.That(machine.State, Is.EqualTo(MovementState.Falling));

            float landed = -1f;
            machine.FallLanded += distance => landed = distance;
            motor.Y = 1f;
            motor.Vy = -2f;
            machine.Tick(0.02f, Grounded, motor);

            Assert.That(machine.State, Is.EqualTo(MovementState.Walking));
            Assert.That(landed, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Launch_KeepsImpulseUntilLanding_ThenReturnsToWalking()
        {
            var machine = new DeliverymanStateMachine(2f, cellSize: 1f);
            var motor = new FakeMotor { Y = 1f, Vy = 0f };
            machine.Tick(0.02f, Grounded, motor);

            machine.Launch(motor, 1f, 8f);
            Assert.That(machine.State, Is.EqualTo(MovementState.Launched));
            Assert.That(motor.Vy, Is.EqualTo(8f));

            // Rising: the walking controller must not zero out the launch velocity.
            motor.Y = 2f;
            motor.Vy = 6f;
            machine.Tick(0.02f, new MovementSensorResult(), motor);
            Assert.That(motor.Vy, Is.EqualTo(6f));
            Assert.That(motor.Vx, Is.EqualTo(1f));

            float landed = -1f;
            machine.FallLanded += distance => landed = distance;
            motor.Y = -2f;
            motor.Vy = -5f;
            machine.Tick(0.02f, Grounded, motor);

            Assert.That(machine.State, Is.EqualTo(MovementState.Walking));
            Assert.That(landed, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Launch_AfterDelivery_IsIgnored()
        {
            var machine = new DeliverymanStateMachine(2f);
            var motor = new FakeMotor();
            machine.Deliver(motor);

            machine.Launch(motor, 0f, 8f);

            Assert.That(machine.State, Is.EqualTo(MovementState.Delivered));
            Assert.That(motor.Vy, Is.Zero);
        }

        [Test]
        public void Deliver_StopsTheController()
        {
            var machine = new DeliverymanStateMachine(2f);
            var motor = new FakeMotor { Vx = 2f };

            machine.Deliver(motor);
            machine.Tick(0.02f, Grounded, motor);

            Assert.That(machine.State, Is.EqualTo(MovementState.Delivered));
            Assert.That(motor.Vx, Is.Zero);
        }
    }
}
