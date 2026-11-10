using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.UI;

namespace Template.Tests.EditMode
{
    /// <summary>Back navigation is only trustworthy if the stack itself is. These pin push/pop.</summary>
    public class ScreenStackTests
    {
        FakeScreen root, settings, purchases, result;

        [SetUp]
        public void SetUp()
        {
            root = new FakeScreen("root");
            settings = new FakeScreen("settings");
            purchases = new FakeScreen("purchases");
            result = new FakeScreen("result");
        }

        [Test]
        public void Pop_RevealsTheScreenBelow()
        {
            var stack = new ScreenStack();
            stack.Push(settings);
            stack.Push(purchases);

            bool popped = stack.TryPop(out IScreenView revealed);

            Assert.That(popped, Is.True);
            Assert.That(revealed, Is.EqualTo(settings));
            Assert.That(stack.Current, Is.EqualTo(settings));
        }

        [Test]
        public void Pop_TheOnlyScreen_IsRefused()
        {
            var stack = new ScreenStack();
            stack.Push(root);

            bool popped = stack.TryPop(out IScreenView revealed);

            // A region always keeps its root: the Android back button at it is the caller's call.
            Assert.That(popped, Is.False);
            Assert.That(revealed, Is.Null);
            Assert.That(stack.Current, Is.EqualTo(root));
        }

        [Test]
        public void TheSameView_Twice_AndDuplicateIds_AreRefused()
        {
            var stack = new ScreenStack();
            stack.Push(settings);

            Assert.Throws<InvalidOperationException>(() => stack.Push(settings));
            Assert.Throws<InvalidOperationException>(() => stack.Push(new FakeScreen("settings")));
        }

        [Test]
        public void Push_Needs_ARealView_WithAnId()
        {
            var stack = new ScreenStack();

            Assert.Throws<ArgumentNullException>(() => stack.Push(null));
            Assert.Throws<InvalidOperationException>(() => stack.Push(new FakeScreen(null)));
        }

        [Test]
        public void PopTo_RemovesEverythingAboveTheTarget()
        {
            var stack = new ScreenStack();
            stack.Push(settings);
            stack.Push(purchases);
            stack.Push(result);

            stack.PopTo(settings);

            Assert.That(stack.Current, Is.EqualTo(settings));
            Assert.That(stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void PopTo_AForeignScreen_Throws()
        {
            var stack = new ScreenStack();
            stack.Push(settings);

            Assert.Throws<InvalidOperationException>(() => stack.PopTo(purchases));
        }

        [Test]
        public void Clear_DropsTheHistory_ButKeepsTheData()
        {
            var stack = new ScreenStack();
            stack.Push(settings);
            stack.Push(purchases);

            stack.Clear();

            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(stack.Current, Is.Null);
            Assert.That(stack.Contains("settings"), Is.False);
        }

        sealed class FakeScreen : IScreenView
        {
            public FakeScreen(string id) => ScreenId = id;

            public string ScreenId { get; }

            public Task ShowAsync(CancellationToken token = default) => Task.CompletedTask;

            public Task HideAsync(CancellationToken token = default) => Task.CompletedTask;
        }
    }
}