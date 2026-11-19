using BlindDelivery.Core.Upgrades;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class UpgradeServiceTests
    {
        [Test]
        public void NewUpgrade_IsLocked()
        {
            var upgrades = new BlindDelivery.Services.Upgrades.UpgradeService();
            Assert.That(upgrades.Level(UpgradeKind.EcholocationSlowdown), Is.Zero);
            Assert.That(upgrades.IsUnlocked(UpgradeKind.EcholocationSlowdown, 1), Is.False);
        }

        [Test]
        public void OwnedLevel_UnlocksEveryLowerTier()
        {
            var upgrades = new BlindDelivery.Services.Upgrades.UpgradeService();
            upgrades.SetLevel(UpgradeKind.EcholocationSlowdown, 2);

            Assert.That(upgrades.IsUnlocked(UpgradeKind.EcholocationSlowdown, 1), Is.True);
            Assert.That(upgrades.IsUnlocked(UpgradeKind.EcholocationSlowdown, 2), Is.True);
            Assert.That(upgrades.IsUnlocked(UpgradeKind.EcholocationSlowdown, 3), Is.False);
        }

        [Test]
        public void UpgradesAreIndependent()
        {
            var upgrades = new BlindDelivery.Services.Upgrades.UpgradeService();
            upgrades.SetLevel(UpgradeKind.EcholocationSlowdown, 3);

            Assert.That(upgrades.Level(UpgradeKind.BoxCapacity), Is.Zero);
        }
    }
}
