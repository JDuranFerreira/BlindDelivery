using BlindDelivery.Core.Scoring;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public sealed class RunTimerTests
    {
        [Test]
        public void Timer_CountsOnlyWhileRunning()
        {
            var timer = new RunTimer();
            timer.Step(0.02f);
            Assert.That(timer.ElapsedSeconds, Is.Zero, "no time passes before the run starts");

            timer.Start();
            timer.Step(0.02f);
            timer.Step(0.02f);
            Assert.That(timer.ElapsedSeconds, Is.EqualTo(0.04f).Within(0.00001f));

            timer.Stop();
            timer.Step(0.02f);
            Assert.That(timer.ElapsedSeconds, Is.EqualTo(0.04f).Within(0.00001f));
        }

        [Test]
        public void Timer_RestartsFromZero()
        {
            var timer = new RunTimer();
            timer.Start();
            timer.Step(1f);
            timer.Start();
            Assert.That(timer.ElapsedSeconds, Is.Zero);
            timer.Step(0.5f);
            Assert.That(timer.ElapsedSeconds, Is.EqualTo(0.5f));
        }
    }
}
