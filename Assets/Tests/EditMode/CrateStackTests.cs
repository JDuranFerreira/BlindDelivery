using BlindDelivery.Core.Crates;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class CrateStackTests
    {
        [Test]
        public void Fall_LongerThanSafeHeight_LosesOneCrate()
        {
            var crates = new CrateStack(3);

            Assert.That(crates.ApplyFall(3f, 3, false), Is.False);
            Assert.That(crates.Count, Is.EqualTo(3));
            Assert.That(crates.ApplyFall(3.1f, 3, false), Is.True);
            Assert.That(crates.Count, Is.EqualTo(2));
        }

        [Test]
        public void CushionedFall_NeverLosesACrate()
        {
            var crates = new CrateStack(2);

            Assert.That(crates.ApplyFall(12f, 3, true), Is.False);
            Assert.That(crates.Count, Is.EqualTo(2));
        }

        [Test]
        public void LoseOne_CannotGoBelowZero()
        {
            var crates = new CrateStack(1);
            Assert.That(crates.LoseOne(), Is.True);
            Assert.That(crates.LoseOne(), Is.False);
            Assert.That(crates.Count, Is.Zero);
        }
    }
}
