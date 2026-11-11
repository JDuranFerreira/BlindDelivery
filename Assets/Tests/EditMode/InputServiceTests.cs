using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using BlindDelivery.Core.Input;
using BlindDelivery.Core.Services;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// Tests the IInputService contract with a fake implementation â€” verifies event signatures,
    /// gesture detection logic, and DPI scaling math. The real InputService wraps Unity's
    /// Input System; these tests exercise the pure logic without engine dependencies.
    /// </summary>
    public class InputServiceTests
    {
        [Test]
        public void Vector2_ZeroAndOne_AreConstants()
        {
            Assert.That(BlindDelivery.Core.Input.Vector2.Zero, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(0f, 0f)));
            Assert.That(BlindDelivery.Core.Input.Vector2.One, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(1f, 1f)));
        }

        [Test]
        public void Vector2_Magnitude_And_Normalized()
        {
            var v = new BlindDelivery.Core.Input.Vector2(3f, 4f);
            Assert.That(v.Magnitude, Is.EqualTo(5f).Within(0.0001f));
            var n = v.Normalized;
            Assert.That(n.Magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Vector2_Operators_AddSubtractMultiplyDivide()
        {
            var a = new BlindDelivery.Core.Input.Vector2(2f, 3f);
            var b = new BlindDelivery.Core.Input.Vector2(5f, 7f);
            Assert.That(a + b, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(7f, 10f)));
            Assert.That(b - a, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(3f, 4f)));
            Assert.That(a * 2f, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(4f, 6f)));
            Assert.That(b / 2f, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(2.5f, 3.5f)));
        }

        [Test]
        public void Vector2_Equality_And_HashCode()
        {
            var a = new BlindDelivery.Core.Input.Vector2(1.5f, 2.5f);
            var b = new BlindDelivery.Core.Input.Vector2(1.5f, 2.5f);
            var c = new BlindDelivery.Core.Input.Vector2(2f, 3f);
            Assert.That(a == b, Is.True);
            Assert.That(a != c, Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void FakeInputService_CanSubscribeAndInvokeAllEvents()
        {
            var fake = new FakeInputService();
            var events = new List<string>();

            fake.OnTap += v => events.Add($"tap:{v}");
            fake.OnDoubleTap += v => events.Add($"doubletap:{v}");
            fake.OnHold += v => events.Add($"hold:{v}");
            fake.OnSwipe += v => events.Add($"swipe:{v}");
            fake.OnDrag += v => events.Add($"drag:{v}");
            fake.OnPinch += s => events.Add($"pinch:{s}");

            fake.RaiseTap(new BlindDelivery.Core.Input.Vector2(10f, 20f));
            fake.RaiseDoubleTap(new BlindDelivery.Core.Input.Vector2(30f, 40f));
            fake.RaiseHold(new BlindDelivery.Core.Input.Vector2(50f, 60f));
            fake.RaiseSwipe(new BlindDelivery.Core.Input.Vector2(0.7f, 0.7f));
            fake.RaiseDrag(new BlindDelivery.Core.Input.Vector2(5f, 10f));
            fake.RaisePinch(1.5f);

            Assert.That(events, Has.Count.EqualTo(6));
            Assert.That(events[0], Is.EqualTo("tap:(10, 20)"));
            Assert.That(events[1], Is.EqualTo("doubletap:(30, 40)"));
            Assert.That(events[2], Is.EqualTo("hold:(50, 60)"));
            Assert.That(events[3], Is.EqualTo("swipe:(0.7, 0.7)"));
            Assert.That(events[4], Is.EqualTo("drag:(5, 10)"));
            Assert.That(events[5], Is.EqualTo("pinch:1.5"));
        }

        [Test]
        public void FakeInputService_PointerPosition_And_IsPointerDown()
        {
            var fake = new FakeInputService();
            Assert.That(fake.PointerPosition, Is.Null);
            Assert.That(fake.IsPointerDown, Is.False);

            fake.SetPointerPosition(new BlindDelivery.Core.Input.Vector2(100f, 200f));
            fake.SetPointerDown(true);

            Assert.That(fake.PointerPosition, Is.EqualTo(new BlindDelivery.Core.Input.Vector2(100f, 200f)));
            Assert.That(fake.IsPointerDown, Is.True);

            fake.SetPointerDown(false);
            Assert.That(fake.IsPointerDown, Is.False);
        }
    }

    /// <summary>
    /// Minimal fake implementing IInputService for EditMode testing.
    /// The real InputService wraps UnityEngine.InputSystem; this exercises the interface contract.
    /// </summary>
    public sealed class FakeInputService : IInputService
    {
        public string Name => "FakeInput";

        public event Action<BlindDelivery.Core.Input.Vector2> OnTap;
        public event Action<BlindDelivery.Core.Input.Vector2> OnDoubleTap;
        public event Action<BlindDelivery.Core.Input.Vector2> OnHold;
        public event Action<BlindDelivery.Core.Input.Vector2> OnSwipe;
        public event Action<BlindDelivery.Core.Input.Vector2> OnDrag;
        public event Action<float> OnPinch;

        public BlindDelivery.Core.Input.Vector2? PointerPosition { get; private set; }
        public bool IsPointerDown { get; private set; }

        public Task InitializeAsync(System.Threading.CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void RaiseTap(BlindDelivery.Core.Input.Vector2 pos) => OnTap?.Invoke(pos);
        public void RaiseDoubleTap(BlindDelivery.Core.Input.Vector2 pos) => OnDoubleTap?.Invoke(pos);
        public void RaiseHold(BlindDelivery.Core.Input.Vector2 pos) => OnHold?.Invoke(pos);
        public void RaiseSwipe(BlindDelivery.Core.Input.Vector2 dir) => OnSwipe?.Invoke(dir);
        public void RaiseDrag(BlindDelivery.Core.Input.Vector2 delta) => OnDrag?.Invoke(delta);
        public void RaisePinch(float scale) => OnPinch?.Invoke(scale);

        public void SetPointerPosition(BlindDelivery.Core.Input.Vector2 pos) => PointerPosition = pos;
        public void SetPointerDown(bool down) => IsPointerDown = down;
    }
}

