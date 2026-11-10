using NUnit.Framework;
using Template.Core.UI;

namespace Template.Tests.EditMode
{
    /// <summary>Notch math. A pixel off here is a notch touching a button in the wild.</summary>
    public class SafeAreaSolverTests
    {
        [Test]
        public void FullScreenRect_GivesZeroInsets()
        {
            SafeAreaInsets insets = SafeAreaSolver.Compute(1080f, 1920f, 0f, 0f, 1080f, 1920f);

            Assert.That(insets.Left, Is.EqualTo(0f));
            Assert.That(insets.Right, Is.EqualTo(0f));
            Assert.That(insets.Top, Is.EqualTo(0f));
            Assert.That(insets.Bottom, Is.EqualTo(0f));
        }

        [Test]
        public void NotchAtTop_TakesItOutOfTheTopInset()
        {
            // Pixel-notional Android notch: window 1080x1920, safe rect inset 90px left/30 top.
            SafeAreaInsets insets = SafeAreaSolver.Compute(1080f, 1920f, 0f, 30f, 1080f, 1820f);

            Assert.That(insets.Top, Is.EqualTo(70f));
            Assert.That(insets.Left, Is.EqualTo(0f));
            Assert.That(insets.Right, Is.EqualTo(0f));
            Assert.That(insets.Bottom, Is.EqualTo(30f));
        }

        [Test]
        public void NavigationBar_SitsAtTheBottom()
        {
            SafeAreaInsets insets = SafeAreaSolver.Compute(1080f, 1920f, 0f, 180f, 1080f, 1740f);

            Assert.That(insets.Bottom, Is.EqualTo(180f));
            Assert.That(insets.Top, Is.EqualTo(0f));
        }

        [Test]
        public void RoundedCorners_TakeInBothSides()
        {
            SafeAreaInsets insets = SafeAreaSolver.Compute(1080f, 1920f, 40f, 0f, 1000f, 1920f);

            Assert.That(insets.Left, Is.EqualTo(40f));
            Assert.That(insets.Right, Is.EqualTo(40f));
            Assert.That(insets.Top + insets.Bottom, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ZeroSizes_ReturnZeroInsets_RatherThanNaN()
        {
            // Launch on some devices reports a degenerate rect before the first real one.
            SafeAreaInsets insets = SafeAreaSolver.Compute(0f, 0f, 0f, 0f, 0f, 0f);

            Assert.That(insets.Left, Is.EqualTo(0f));
            Assert.That(insets.Right, Is.EqualTo(0f));
            Assert.That(insets.Top, Is.EqualTo(0f));
            Assert.That(insets.Bottom, Is.EqualTo(0f));
        }

        [Test]
        public void SafeRectBiggerThanTheWindow_IsClamped_NoNegativeInsets()
        {
            SafeAreaInsets insets = SafeAreaSolver.Compute(1080f, 1920f, -200f, -100f, 2000f, 3000f);

            Assert.That(insets.Left, Is.EqualTo(0f));
            Assert.That(insets.Bottom, Is.EqualTo(0f));
            Assert.That(insets.Right, Is.EqualTo(0f));
            Assert.That(insets.Top, Is.EqualTo(0f));
        }
    }
}