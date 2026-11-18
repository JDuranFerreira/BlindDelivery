using System;
using NUnit.Framework;
using BlindDelivery.Core.Balance;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// GameBalance is pure C# (no Unity types), so these tests run in EditMode without a scene.
    /// </summary>
    public class GameBalanceTests
    {
        [Test]
        public void SafeFallHeight_IsThreeCellsByDefault()
        {
            var balance = new GameBalance();
            Assert.That(balance.SafeFallHeightCells, Is.EqualTo(3));
        }

        [Test]
        public void FixedStep_Is0_02Seconds()
        {
            var balance = new GameBalance();
            Assert.That(balance.FixedStepSeconds, Is.EqualTo(0.02f));
        }

        [Test]
        public void DeliverymanSpeed_DefaultsTo2_5MetresPerSecond()
        {
            var balance = new GameBalance();
            Assert.That(balance.DeliverymanSpeed, Is.EqualTo(2.5f));
        }

        [Test]
        public void BaseCrateCount_IsThree()
        {
            var balance = new GameBalance();
            Assert.That(balance.BaseCrateCount, Is.EqualTo(3));
        }

        [Test]
        public void MaxCrateCount_IsFive()
        {
            var balance = new GameBalance();
            Assert.That(balance.MaxCrateCount, Is.EqualTo(5));
        }

        [Test]
        public void SafeFallHeight_CanBeChangedToTwoForBalanceTesting()
        {
            var balance = new GameBalance { SafeFallHeightCells = 2 };
            Assert.That(balance.SafeFallHeightCells, Is.EqualTo(2));
        }

        [Test]
        public void ItemMoveCosts_AreDefinedForEveryItemType()
        {
            var balance = new GameBalance();
            foreach (ItemType type in (ItemType[])Enum.GetValues(typeof(ItemType)))
            {
                Assert.That(balance.ItemMoveCosts.ContainsKey(type), Is.True,
                    $"Missing move cost for {type}");
                Assert.That(balance.ItemMoveCosts[type], Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void ItemCooldownSeconds_AreDefinedForEveryItemType()
        {
            var balance = new GameBalance();
            foreach (ItemType type in (ItemType[])Enum.GetValues(typeof(ItemType)))
            {
                Assert.That(balance.ItemCooldownSeconds.ContainsKey(type), Is.True,
                    $"Missing cooldown for {type}");
                Assert.That(balance.ItemCooldownSeconds[type], Is.GreaterThanOrEqualTo(0f));
            }
        }
    }
}
