using System;
using NUnit.Framework;
using Template.Core.Events;
using Template.Services.Events;

namespace Template.Tests.EditMode
{
    public class EventBusTests
    {
        readonly struct Ping : IEvent
        {
            public Ping(int value) => Value = value;

            public int Value { get; }
        }

        readonly struct Pong : IEvent
        {
        }

        EventBus bus;

        [SetUp]
        public void SetUp() => bus = new EventBus();

        [Test]
        public void Publish_ReachesSubscriber()
        {
            int seen = 0;
            bus.Subscribe<Ping>(e => seen = e.Value);

            bus.Publish(new Ping(7));

            Assert.That(seen, Is.EqualTo(7));
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => bus.Publish(new Ping(1)));
        }

        [Test]
        public void Publish_OnlyReachesTheMatchingEventType()
        {
            int pings = 0;
            int pongs = 0;
            bus.Subscribe<Ping>(_ => pings++);
            bus.Subscribe<Pong>(_ => pongs++);

            bus.Publish(new Pong());

            Assert.That(pings, Is.EqualTo(0));
            Assert.That(pongs, Is.EqualTo(1));
        }

        [Test]
        public void TwoSubscribers_BothReceive()
        {
            int total = 0;
            bus.Subscribe<Ping>(e => total += e.Value);
            bus.Subscribe<Ping>(e => total += e.Value);

            bus.Publish(new Ping(2));

            Assert.That(total, Is.EqualTo(4));
        }

        [Test]
        public void Unsubscribe_StopsDelivery_AndIsIdempotent()
        {
            int calls = 0;
            Action<Ping> handler = _ => calls++;
            bus.Subscribe(handler);

            bus.Unsubscribe(handler);
            bus.Unsubscribe(handler);
            bus.Publish(new Ping(1));

            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void UnsubscribingDuringPublish_DoesNotStopTheOtherHandlers()
        {
            int lateCalls = 0;
            Action<Ping> late = _ => lateCalls++;

            bus.Subscribe<Ping>(_ => bus.Unsubscribe(late));
            bus.Subscribe(late);

            bus.Publish(new Ping(1));
            bus.Publish(new Ping(1));

            // The delegate snapshot taken for the in-flight publish still contains the second
            // handler, so it runs once; the next publish no longer sees it.
            Assert.That(lateCalls, Is.EqualTo(1));
        }

        [Test]
        public void SubscribingDuringPublish_DoesNotReceiveTheInFlightEvent()
        {
            int laterCalls = 0;
            bus.Subscribe<Ping>(_ => bus.Subscribe<Ping>(_ => laterCalls++));

            bus.Publish(new Ping(1));

            Assert.That(laterCalls, Is.EqualTo(0));

            bus.Publish(new Ping(1));

            Assert.That(laterCalls, Is.EqualTo(1));
        }

        [Test]
        public void NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => bus.Subscribe<Ping>(null));
            Assert.Throws<ArgumentNullException>(() => bus.Unsubscribe<Ping>(null));
        }

        [Test]
        public void Clear_DropsEverySubscription()
        {
            int calls = 0;
            bus.Subscribe<Ping>(_ => calls++);

            bus.Clear();
            bus.Publish(new Ping(1));

            Assert.That(calls, Is.EqualTo(0));
        }
    }
}
