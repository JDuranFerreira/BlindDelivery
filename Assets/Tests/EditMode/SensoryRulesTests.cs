using BlindDelivery.Core.Sensory;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class SensoryRulesTests
    {
        [Test]
        public void NoLedge_ProducesNoWarning()
        {
            SensoryReading reading = SensoryRules.Evaluate(0f, 0f, 0f, 2f, 1.5f);
            Assert.That(reading.HasLedge, Is.False);
            Assert.That(reading.WarningIntensity, Is.Zero);
            Assert.That(reading.SlowTrigger, Is.False);
        }

        [Test]
        public void LedgeInsideWarningRange_GrowsInIntensity()
        {
            SensoryReading near = SensoryRules.Evaluate(0.5f, 0f, 0f, 2f, 1.5f);
            SensoryReading far = SensoryRules.Evaluate(1.5f, 0f, 0f, 2f, 1.5f);
            Assert.That(near.WarningIntensity, Is.GreaterThan(far.WarningIntensity));
            Assert.That(far.WarningIntensity, Is.GreaterThan(0f));
        }

        [Test]
        public void LedgeBeyondWarningRange_IsSilent()
        {
            SensoryReading reading = SensoryRules.Evaluate(2.5f, 0f, 0f, 2f, 1.5f);
            Assert.That(reading.HasLedge, Is.True);
            Assert.That(reading.WarningIntensity, Is.Zero);
        }

        [Test]
        public void SlowTrigger_OnlyInsideSlowRange()
        {
            Assert.That(SensoryRules.Evaluate(1f, 0f, 0f, 2f, 1.5f).SlowTrigger, Is.True);
            Assert.That(SensoryRules.Evaluate(1.6f, 0f, 0f, 2f, 1.5f).SlowTrigger, Is.False);
        }

        [Test]
        public void BeaconDirection_IsClamped()
        {
            Assert.That(SensoryRules.Evaluate(0f, 0f, 3f, 2f, 1.5f).BeaconDirection, Is.EqualTo(1f));
            Assert.That(SensoryRules.Evaluate(0f, 0f, -3f, 2f, 1.5f).BeaconDirection, Is.EqualTo(-1f));
        }

        [Test]
        public void SlowFactor_StrengthensWithUpgradeLevel()
        {
            Assert.That(SensoryRules.SlowFactorForLevel(0), Is.EqualTo(1f));
            Assert.That(SensoryRules.SlowFactorForLevel(2), Is.GreaterThan(SensoryRules.SlowFactorForLevel(1)));
            Assert.That(SensoryRules.SlowFactorForLevel(3), Is.LessThanOrEqualTo(0.8f));
        }
    }
}
