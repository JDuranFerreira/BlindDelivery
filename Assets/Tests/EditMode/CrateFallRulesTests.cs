using BlindDelivery.Core.Crates;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class CrateFallRulesTests
    {
        [Test]
        public void Gravity_AcceleratesDownward()
        {
            Assert.That(CrateFallRules.Integrate(0f, 10f, 0.5f), Is.EqualTo(-5f));
            Assert.That(CrateFallRules.Integrate(-5f, 10f, 0.5f), Is.EqualTo(-10f));
        }

        [Test]
        public void Landing_RequiresGroundAndDescent()
        {
            Assert.That(CrateFallRules.HasLanded(true, true), Is.True);
            Assert.That(CrateFallRules.HasLanded(false, true), Is.False, "no ground below means keep falling");
            Assert.That(CrateFallRules.HasLanded(true, false), Is.False, "a rising crate has not landed");
        }

        [Test]
        public void OutOfWorld_RequiresFallingPastLevelBottomAndMargin()
        {
            Assert.That(CrateFallRules.IsOutOfWorld(-5f, -6f, 1f), Is.False, "inside the level");
            Assert.That(CrateFallRules.IsOutOfWorld(-7f, -6f, 1f), Is.False, "at the margin is still in play");
            Assert.That(CrateFallRules.IsOutOfWorld(-7.01f, -6f, 1f), Is.True, "past the margin is out of the world");
        }
    }
}
