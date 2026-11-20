using System;
using NUnit.Framework;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>Pure gameplay rules stay in Core so these run without a scene or a physics world.</summary>
    public sealed class ItemInteractionRulesTests
    {
        static GameBalance Balance(float height = 5f, float gravity = 9.81f, float speed = 2.5f)
        {
            return new GameBalance
            {
                Gravity = gravity,
                JumppadLaunchHeightCells = height,
                DeliverymanSpeed = speed,
                JumppadForwardSpeedFactor = 0.5f,
            };
        }

        [Test]
        public void LaunchVerticalSpeed_MatchesEnergyFormula()
        {
            float expected = (float)Math.Sqrt(2d * 9.81d * 5d);
            Assert.AreEqual(expected, ItemInteractionRules.LaunchVerticalSpeed(Balance()), 1e-4f);
        }

        [Test]
        public void LaunchVerticalSpeed_ScalesWithHeight()
        {
            float low = ItemInteractionRules.LaunchVerticalSpeed(Balance(3f));
            float high = ItemInteractionRules.LaunchVerticalSpeed(Balance(5f));
            Assert.Greater(high, low);
        }

        [Test]
        public void LaunchHorizontalSpeed_IsSignedByFacing()
        {
            GameBalance balance = Balance();
            Assert.AreEqual(1.25f, ItemInteractionRules.LaunchHorizontalSpeed(balance, 1f), 1e-4f);
            Assert.AreEqual(-1.25f, ItemInteractionRules.LaunchHorizontalSpeed(balance, -1f), 1e-4f);
        }

        [Test]
        public void LaunchHorizontalSpeed_UsesLevelSpeedWhenSupplied()
        {
            GameBalance balance = Balance(speed: 9f);
            Assert.AreEqual(1.45f, ItemInteractionRules.LaunchHorizontalSpeed(balance, 1f, 2.9f), 1e-4f);
        }

        [Test]
        public void ReachedSpringCenter_TrueOverPad_IgnoreOutsideXAndBelow()
        {
            Assert.IsTrue(ItemInteractionRules.ReachedSpringCenter(2f, 0f, 0.2f, 2.05f, 0.15f, 0.2f, 0.25f));
            Assert.IsFalse(ItemInteractionRules.ReachedSpringCenter(2f, 0f, 0.2f, 2.6f, 0.15f, 0.2f, 0.25f));
            Assert.IsFalse(ItemInteractionRules.ReachedSpringCenter(2f, 0f, 0.2f, 2f, -0.6f, 0.2f, 0.25f));
        }

        [Test]
        public void TouchesTop_TrueJustAboveCushion_FalseFarBelow()
        {
            Assert.IsTrue(ItemInteractionRules.TouchesTop(2f, 0.5f, 0.4f, 2.2f, 0.45f, 0.6f, 0.25f));
            Assert.IsFalse(ItemInteractionRules.TouchesTop(2f, 0.5f, 0.4f, 3.5f, 0.45f, 0.6f, 0.25f));
            Assert.IsFalse(ItemInteractionRules.TouchesTop(2f, 0.5f, 0.4f, 2f, -0.5f, 0.6f, 0.25f));
        }

        [Test]
        public void SpeedFactor_SlowsWithGum_AndSpeedsWithOil()
        {
            GameBalance balance = Balance();
            balance.BubbleGumSpeedFactor = 0.5f;
            balance.OilSpeedFactor = 1.5f;
            Assert.AreEqual(0.5f, ItemInteractionRules.SpeedFactor(balance, ItemType.BubbleGum), 1e-5f);
            Assert.AreEqual(1.5f, ItemInteractionRules.SpeedFactor(balance, ItemType.Oil), 1e-5f);
            Assert.AreEqual(1f, ItemInteractionRules.SpeedFactor(balance, ItemType.Cardbox), 1e-5f);
        }

        [Test]
        public void NullBalance_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ItemInteractionRules.LaunchVerticalSpeed(null));
            Assert.Throws<ArgumentNullException>(() => ItemInteractionRules.LaunchHorizontalSpeed(null, 1f));
        }
    }
}
