using System;
using BlindDelivery.Core.Movement;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class SlowdownTests
    {
        sealed class FakeMotor : IMotor
        {
            public float Vx, Vy;
            public void SetVelocity(float x, float y) { Vx = x; Vy = y; }
            public void ReadVelocity(out float x, out float y) { x = Vx; y = Vy; }
            public void ReadPosition(out float x, out float y) { x = 0f; y = 0f; }
        }

        static MovementSensorResult Grounded => new MovementSensorResult
        {
            GroundAhead = true,
            GroundBelow = true,
        };

        [Test]
        public void ApplySlow_WhileWalking_ReducesSpeed()
        {
            var machine = new DeliverymanStateMachine(2f);
            var motor = new FakeMotor();
            machine.Tick(0.02f, Grounded, motor);

            machine.ApplySlow(0.5f, 1f);
            machine.Tick(0.02f, Grounded, motor);

            Assert.That(machine.State, Is.EqualTo(MovementState.Slowed));
            Assert.That(motor.Vx, Is.EqualTo(1f));
        }

        [Test]
        public void Slow_ExpiresBackToWalkingAtFullSpeed()
        {
            var machine = new DeliverymanStateMachine(2f);
            var motor = new FakeMotor();
            machine.Tick(0.02f, Grounded, motor);
            machine.ApplySlow(0.5f, 0.05f);

            for (int i = 0; i < 4; i++) machine.Tick(0.02f, Grounded, motor);

            Assert.That(machine.State, Is.EqualTo(MovementState.Walking));
            Assert.That(motor.Vx, Is.EqualTo(2f));
        }

        [Test]
        public void Slow_IsIgnoredBeforeWalking()
        {
            var machine = new DeliverymanStateMachine(2f);
            machine.ApplySlow(0.5f, 1f);
            Assert.That(machine.State, Is.EqualTo(MovementState.Idle));
        }

        [Test]
        public void Slow_IsIgnoredAfterDelivery()
        {
            var machine = new DeliverymanStateMachine(2f);
            var motor = new FakeMotor();
            machine.Deliver(motor);
            machine.ApplySlow(0.5f, 1f);
            Assert.That(machine.State, Is.EqualTo(MovementState.Delivered));
        }

        [Test]
        public void InvalidFactor_IsRejected()
        {
            var machine = new DeliverymanStateMachine(2f);
            Assert.Throws<ArgumentOutOfRangeException>(() => machine.ApplySlow(1.2f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => machine.ApplySlow(0.5f, 0f));
        }
    }
}
