using System;
using NUnit.Framework;
using Template.Core.UI;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// One popup at a time, no duplicates. These pin the queue semantics because "no internet"
    /// fires as a burst in every networked game — the one thing this logic must never do is show
    /// five identical popups.
    /// </summary>
    public class PopupQueueTests
    {
        [Test]
        public void StartAndComplete_HandOutOneRequest_ATime()
        {
            var queue = new PopupQueue();
            queue.Enqueue(new PopupRequest("error", "Oops", "Something broke."));

            Assert.That(queue.TryStartNext(out PopupRequest first), Is.True);
            Assert.That(first.PopupId, Is.EqualTo("error"));
            Assert.That(queue.IsShowing, Is.True);

            // While it is on screen, nothing else can start.
            Assert.That(queue.TryStartNext(out _), Is.False);

            Assert.That(queue.TryComplete(out PopupRequest done), Is.True);
            Assert.That(done.PopupId, Is.EqualTo("error"));
            Assert.That(queue.IsShowing, Is.False);
        }

        [Test]
        public void Requests_WaitInThe_OrderTheyArrived()
        {
            var queue = new PopupQueue();
            queue.Enqueue(new PopupRequest("confirm", "Save?", ""));
            queue.Enqueue(new PopupRequest("reward", "Bonus!", ""));
            queue.Enqueue(new PopupRequest("error", "Oops", ""));

            queue.TryStartNext(out PopupRequest first);
            queue.TryComplete(out _);
            queue.TryStartNext(out PopupRequest second);
            queue.TryComplete(out _);
            queue.TryStartNext(out PopupRequest third);

            Assert.That(first.PopupId, Is.EqualTo("confirm"));
            Assert.That(second.PopupId, Is.EqualTo("reward"));
            Assert.That(third.PopupId, Is.EqualTo("error"));
            Assert.That(queue.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void SameIdTwice_QueuesOnlyOne()
        {
            var queue = new PopupQueue();
            queue.Enqueue(new PopupRequest("no-internet", "Offline", "Check your connection."));
            queue.Enqueue(new PopupRequest("no-internet", "Offline", "Check your connection."));

            Assert.That(queue.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void SameId_AsTheActiveOne_IsDropped()
        {
            var queue = new PopupQueue();
            queue.Enqueue(new PopupRequest("no-internet", "Offline", ""));

            queue.TryStartNext(out _);
            queue.Enqueue(new PopupRequest("no-internet", "Offline", ""));

            Assert.That(queue.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void DifferentIds_WithTheSameShape_BothQueue()
        {
            var queue = new PopupQueue();
            queue.Enqueue(new PopupRequest("confirm", "Quit?", ""));
            queue.Enqueue(new PopupRequest("error", "Quit?", ""));

            Assert.That(queue.PendingCount, Is.EqualTo(2));
        }

        [Test]
        public void EmptyId_Throws()
        {
            var queue = new PopupQueue();

            Assert.Throws<ArgumentException>(
                () => queue.Enqueue(new PopupRequest("", "Title", "")));
        }

        [Test]
        public void Complete_WithoutAnActiveOne_IsFalse()
        {
            var queue = new PopupQueue();

            Assert.That(queue.TryComplete(out _), Is.False);
        }

        [Test]
        public void Clear_DropsPending_ButLeavesTheScreenAsItIs()
        {
            var queue = new PopupQueue();
            queue.Enqueue(new PopupRequest("confirm", "A", ""));
            queue.Enqueue(new PopupRequest("reward", "B", ""));
            queue.TryStartNext(out _);

            queue.Clear();

            Assert.That(queue.PendingCount, Is.EqualTo(0));
            Assert.That(queue.IsShowing, Is.False);
        }
    }
}