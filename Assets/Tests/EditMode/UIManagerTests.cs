using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.UI;
using BlindDelivery.Services.UI;
using NUnit.Framework;

namespace BlindDelivery.Tests.EditMode
{
    public class UIManagerTests
    {
        sealed class FakeView : IScreenView
        {
            public FakeView(string id) { ScreenId = id; }

            public string ScreenId { get; }

            public Task ShowAsync(CancellationToken token = default) => Task.CompletedTask;

            public Task HideAsync(CancellationToken token = default) => Task.CompletedTask;
        }

        [Test]
        public void TryRegisterScreen_RegistersOnce_AndIgnoresRepeatedSceneRegistration()
        {
            var manager = new UIManager();
            var view = new FakeView("Hud");

            Assert.That(manager.TryRegisterScreen(view), Is.True);
            Assert.That(manager.TryRegisterScreen(view), Is.False);
            Assert.That(manager.TryRegisterScreen(new FakeView("Hud")), Is.False);
            Assert.That(manager.TryRegisterScreen(new FakeView("Pause")), Is.True);
        }

        [Test]
        public void TryRegisterScreen_RejectsInvalidIds()
        {
            var manager = new UIManager();

            Assert.That(manager.TryRegisterScreen(null), Is.False);
            Assert.That(manager.TryRegisterScreen(new FakeView("")), Is.False);
        }
    }
}
