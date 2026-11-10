using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Scenes;
using Template.Core.UI;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The transition's shape, driven with fake overlays and operations (real scene ops need a
    /// player loop): cover first, activation exactly at the gate, mapped progress reaching 100%,
    /// uncover last — and cancellation/vanishing overlay never leaving a broken screen.
    /// </summary>
    public class SceneTransitionSequencerTests
    {
        [Test]
        public async Task FullRun_CoversLoadsAndUnCovers_InThatOrder()
        {
            FakeOverlay overlay = new FakeOverlay();
            // Include 0.95 to trigger the gate (>= 0.9) before the final 1.0
            FakeOperationFactory factory = new FakeOperationFactory(
                new FakeOperation(new[] { 0.2f, 0.7f, 0.89f, 0.95f, 1f }, holdsAtGate: true));
            SceneTransitionSequencer sequencer = new SceneTransitionSequencer(overlay);

            await sequencer.RunAsync("MainMenu", factory.Create);

            // The sequencer produces this log sequence:
            // 1. fadeOut
            // 2. progress:0.0000 (initial SetProgress(0f) before pump)
            // 3. progress:0.0000 (first pump iteration: Map(0)=0 before first WaitOneFrameAsync)
            // 4. progress:0.1889 (after first tick 0.2: Map(0.2))
            // 5. progress:0.6611 (after tick 0.7: Map(0.7))
            // 6. progress:0.8406 (after tick 0.89: Map(0.89))
            // 7. progress:0.8500 (gate crossing at 0.95: SetProgress(LoadPortion))
            // 8. progress:0.9250 (after gate: Map(0.95))
            // 9. progress:1.0000 (after loop: final SetProgress(1f))
            // 10. fadeIn
            Assert.That(overlay.Log, Is.EqualTo(new[]
            {
                "fadeOut", "progress:0.0000", "progress:0.0000", "progress:0.1889", "progress:0.6611", "progress:0.8406",
                "progress:0.8500", "progress:0.9250", "progress:1.0000", "fadeIn"
            }));
            OverlayOrderSanity(overlay);
        }

        [Test]
        public async Task CharOfTheGate_ActivationHappensBeforeTheOverlaySeesAnythingMore()
        {
            FakeOverlay overlay = new FakeOverlay();
            FakeOperation operation = new FakeOperation(new[] { 0.9f, 1f }, holdsAtGate: true);
            SceneTransitionSequencer sequencer = new SceneTransitionSequencer(overlay);

            await sequencer.RunAsync("Level1", _ => operation);

            Assert.That(operation.ActivateCount, Is.EqualTo(1));
            Assert.That(overlay.SetProgressCalls.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task NoOverlay_StillLoadsTheScene()
        {
            FakeOperationFactory factory = new FakeOperationFactory(
                new FakeOperation(new[] { 0.3f, 0.95f, 1f }, holdsAtGate: false));
            SceneTransitionSequencer sequencer = new SceneTransitionSequencer(overlay: null);

            await sequencer.RunAsync("MainMenu", factory.Create);

            Assert.That(factory.Operations.ActivateCount, Is.EqualTo(1));
        }

        [Test]
        public void CancelledMidLoad_StopsPumping_Throws()
        {
            FakeOverlay overlay = new FakeOverlay();
            FakeOperation operation = new FakeOperation(
                ticks: new[] { -1f }, holdsAtGate: false); // -1: a tick that never finishes
            using CancellationTokenSource source = new CancellationTokenSource();
            SceneTransitionSequencer sequencer = new SceneTransitionSequencer(overlay);

            source.Cancel();

            // token.ThrowIfCancellationRequested() throws OperationCanceledException,
            // but the awaited Task wraps it as TaskCanceledException.
            Assert.ThrowsAsync<TaskCanceledException>(
                () => sequencer.RunAsync("MainMenu", _ => operation, source.Token));
        }

        [Test]
        public void AnOverlayThatHidItself_IsAnError_NotASilentUncoveredLoad()
        {
            FakeOverlay overlay = new FakeOverlay(hideSelf: true);
            SceneTransitionSequencer sequencer = new SceneTransitionSequencer(overlay);

            FakeOperationFactory factory = new FakeOperationFactory(
                new FakeOperation(new[] { 0.5f, 1f }, holdsAtGate: false));

            Assert.ThrowsAsync<InvalidOperationException>(
                () => sequencer.RunAsync("MainMenu", factory.Create));
        }

        [Test]
        public void NullSceneName_AndANullFactory_Throw()
        {
            SceneTransitionSequencer sequencer = new SceneTransitionSequencer(null);

            Assert.ThrowsAsync<ArgumentException>(
                () => sequencer.RunAsync(" ", _ => null));
            Assert.ThrowsAsync<ArgumentNullException>(
                () => sequencer.RunAsync("MainMenu", null));
        }

        static void OverlayOrderSanity(FakeOverlay overlay)
        {
            // Bar starts at 0 and its last report is 1 before cover-out; nothing after the fade-in.
            Assert.That(overlay.Log.Last(), Is.EqualTo("fadeIn"));
            Assert.That(overlay.Log.Count(c => c == "fadeIn"), Is.EqualTo(1));
            Assert.That(overlay.Log.Count(c => c == "fadeOut"), Is.EqualTo(1));
        }

        sealed class FakeOverlay : ISceneTransitionOverlay
        {
            readonly List<string> log = new List<string>();
            readonly bool hideSelf;

            public FakeOverlay(bool hideSelf = false)
            {
                this.hideSelf = hideSelf;
            }

            public List<string> Log => log;

            public List<string> SetProgressCalls => log.Where(l => l.StartsWith("progress:", StringComparison.Ordinal)).ToList();

            public bool IsVisible { get; private set; } = true;

            public Task FadeOutAsync(CancellationToken token = default)
            {
                IsVisible = true;
                log.Add("fadeOut");
                return Task.CompletedTask;
            }

            public void SetProgress(float progress01)
            {
                log.Add($"progress:{progress01:F4}");
            }

            public Task FadeInAsync(CancellationToken token = default)
            {
                if (hideSelf)
                {
                    IsVisible = false;
                }

                log.Add("fadeIn");
                return Task.CompletedTask;
            }
        }

        sealed class FakeOperation : ISceneOperation
        {
            readonly Queue<float> ticks;
            readonly bool holdsAtGate;

            bool activated;

            public FakeOperation(float[] ticks, bool holdsAtGate)
            {
                this.ticks = new Queue<float>(ticks);
                Progress = 0f;
                this.holdsAtGate = holdsAtGate;
            }

            public float Progress { get; private set; }

            public bool IsDone => Progress >= 1f;

            public bool ActivationRequested => activated;

            public int ActivateCount { get; private set; }

            public Task ActivateAsync(CancellationToken token = default)
            {
                activated = true;
                ActivateCount++;
                return Task.CompletedTask;
            }

            public Task WaitOneFrameAsync(CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();

                // Heartbeats advance the fake: one tick per pump, like Unity's op reports per frame.
                float next = ticks.Count > 0 ? ticks.Dequeue() : (holdsAtGate && activated ? 1f : 0.999f);
                if (next < 0f)
                {
                    // Marker for an asleep driver: the loop would spin forever if the sequencer
                    // did not stop on the token; the cancellation test proves that it does.
                    return Task.CompletedTask;
                }

                Progress = next;
                return Task.CompletedTask;
            }
        }

        sealed class FakeOperationFactory
        {
            readonly FakeOperation operation;

            public FakeOperationFactory(FakeOperation operation)
            {
                this.operation = operation;
            }

            public FakeOperation Operations => operation;

            public ISceneOperation Create(string sceneName)
            {
                Assert.That(sceneName, Is.Not.Empty);
                return operation;
            }
        }
    }
}