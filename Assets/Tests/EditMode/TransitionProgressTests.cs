using System;
using NUnit.Framework;
using Template.Core.UI;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The bar's contract: the raw op's 0.9 stall maps to a bar that keeps creeping to 100%, so
    /// the player never stares at a stuck bar while activation is held.
    /// </summary>
    public class TransitionProgressTests
    {
        [Test]
        public void Zero_IsZero()
        {
            Assert.That(TransitionProgress.Map(0f), Is.EqualTo(0f));
        }

        [Test]
        public void LoadPhase_FillsTheFirst85Percent()
        {
            Assert.That(TransitionProgress.Map(0.45f), Is.EqualTo(0.45f / 0.9f * 0.85f).Within(0.0001f));
        }

        [Test]
        public void TheGate_MapsExactlyToTheStartOfActivation()
        {
            Assert.That(TransitionProgress.Map(0.9f), Is.EqualTo(TransitionProgress.LoadPortion).Within(0.0001f));
        }

        [Test]
        public void Activation_FillsTheLast15Percent()
        {
            // 0.95f is 50% through activation range [0.9, 1.0], maps to 50% of [0.85, 1.0] = 0.925
            Assert.That(TransitionProgress.Map(0.95f), Is.EqualTo(0.85f + 0.5f * 0.15f).Within(0.0001f));
        }

        [Test]
        public void Done_IsOne()
        {
            Assert.That(TransitionProgress.Map(1f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void TheMappedBar_NeverGoesBackwards()
        {
            float last = 0f;
            for (float p = 0f; p <= 1.001f; p += 0.005f)
            {
                float mapped = TransitionProgress.Map(Math.Min(p, 1f));
                Assert.That(mapped, Is.GreaterThanOrEqualTo(last - 0.0001f), $"at raw {p}");
                last = mapped;
            }
        }
    }
}