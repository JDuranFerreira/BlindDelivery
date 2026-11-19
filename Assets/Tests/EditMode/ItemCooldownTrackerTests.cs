using BlindDelivery.Core.Items;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class ItemCooldownTrackerTests
    {
        [Test]
        public void Start_MarksOnlyThatInstanceActive_UntilTickExpires()
        {
            var tracker = new ItemCooldownTracker();
            tracker.Start(1, 0.3f);

            Assert.That(tracker.IsActive(1), Is.True);
            Assert.That(tracker.IsActive(2), Is.False);

            tracker.Tick(0.1f);
            Assert.That(tracker.Remaining(1), Is.EqualTo(0.2f).Within(0.0001f));

            tracker.Tick(0.2f);
            Assert.That(tracker.IsActive(1), Is.False);
            Assert.That(tracker.Remaining(1), Is.Zero);
        }

        [Test]
        public void Restart_OverwritesThePreviousCooldown()
        {
            var tracker = new ItemCooldownTracker();
            tracker.Start(7, 1f);
            tracker.Tick(0.5f);
            tracker.Start(7, 2f);

            Assert.That(tracker.Remaining(7), Is.EqualTo(2f).Within(0.0001f));
        }
    }
}
