using System;
using BlindDelivery.Core.Scoring;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class StarCalculatorTests
    {
        [TestCase(0, 0, true)]
        [TestCase(1, 1, false)]
        [TestCase(2, 2, false)]
        [TestCase(3, 3, false)]
        public void CratesUpToTarget_GiveOneStarEach(int crates, int expectedStars, bool expectedFail)
        {
            StarResult result = StarCalculator.Calculate(crates);

            Assert.That(result.Stars, Is.EqualTo(expectedStars));
            Assert.That(result.Failed, Is.EqualTo(expectedFail));
            Assert.That(result.BonusStars, Is.Zero);
            Assert.That(result.PlusPlus, Is.False);
        }

        [Test]
        public void FourthCrate_IsOneGoldBonusStar()
        {
            StarResult result = StarCalculator.Calculate(4);

            Assert.That(result.Stars, Is.EqualTo(3));
            Assert.That(result.BonusStars, Is.EqualTo(1));
            Assert.That(result.PlusPlus, Is.False);
            Assert.That(result.Failed, Is.False);
        }

        [Test]
        public void FifthCrate_IsThreeStarsPlusPlus()
        {
            StarResult result = StarCalculator.Calculate(5);

            Assert.That(result.Stars, Is.EqualTo(3));
            Assert.That(result.BonusStars, Is.EqualTo(2));
            Assert.That(result.PlusPlus, Is.True);
            Assert.That(result.BonusText, Is.EqualTo("++"));
        }

        [Test]
        public void MoreCratesThanCapacity_DoNotGrantExtraStars()
        {
            StarResult result = StarCalculator.Calculate(9);

            Assert.That(result.Stars, Is.EqualTo(3));
            Assert.That(result.BonusStars, Is.EqualTo(2));
            Assert.That(result.PlusPlus, Is.True);
        }

        [Test]
        public void ZeroCrates_IsAFailedRun()
        {
            StarResult result = StarCalculator.Calculate(0);
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Rating, Is.EqualTo("FAILED"));
        }

        [Test]
        public void WithoutCapacityUpgrade_ThereAreNoBonusStars()
        {
            StarResult result = StarCalculator.Calculate(3, target: 3, maxCrates: 3);
            Assert.That(result.BonusStars, Is.Zero);
            Assert.That(result.PlusPlus, Is.False);
        }

        [Test]
        public void NegativeCrates_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StarCalculator.Calculate(-1));
        }
    }
}
