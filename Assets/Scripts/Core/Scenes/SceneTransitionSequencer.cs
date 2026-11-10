using System;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.UI;

namespace Template.Core.Scenes
{
    /// <summary>
    /// The shape every covered transition takes, pure C# so EditMode tests can drive it with fake
    /// operations and overlays (goal system #2: real progress handling, the stall at the 0.9 gate):
    ///
    /// 1. fade the overlay to opaque (a null overlay is legal - the load then simply runs);
    /// 2. create the operation and pump it: report mapped progress each tick
    ///    (<see cref="TransitionProgress.Map"/>, the 0..85% data-loading stretch);
    /// 3. when raw progress reaches the gate, request activation and keep reporting - the last
    ///    stretch 85..100% is the activation itself;
    /// 4. fade back to the freshly-loaded scene.
    ///
    /// The sequencer owns only the choreography. Creating the operation is the caller's business,
    /// which is what keeps this testable and lets Addressables slot in later (Decisions table).
    /// </summary>
    public sealed class SceneTransitionSequencer
    {
        readonly ISceneTransitionOverlay overlay;

        /// <param name="overlay">May be null (headless runs, or a game with its own load flow).</param>
        public SceneTransitionSequencer(ISceneTransitionOverlay overlay = null)
        {
            this.overlay = overlay;
        }

        public async Task RunAsync(
            string sceneName,
            Func<string, ISceneOperation> createOperation,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A scene name is required.", nameof(sceneName));
            }

            if (createOperation == null)
            {
                throw new ArgumentNullException(nameof(createOperation));
            }

            token.ThrowIfCancellationRequested();

            if (overlay != null)
            {
                await overlay.FadeOutAsync(token).ConfigureAwait(true);
                overlay.SetProgress(0f);
            }

            await PumpAsync(createOperation(sceneName), token).ConfigureAwait(true);

            if (overlay != null)
            {
                if (!overlay.IsVisible)
                {
                    throw new InvalidOperationException(
                        "The transition overlay hid itself mid-load; the fresh scene would be" +
                        " revealed before its first frame is drawn.");
                }

                await overlay.FadeInAsync(token).ConfigureAwait(true);

                if (!overlay.IsVisible)
                {
                    throw new InvalidOperationException(
                        "The transition overlay hid itself during fade-in; the fresh scene would be" +
                        " revealed before its first frame is drawn.");
                }
            }
        }

        async Task PumpAsync(ISceneOperation operation, CancellationToken token)
        {
            while (!operation.IsDone)
            {
                token.ThrowIfCancellationRequested();

                if (!operation.ActivationRequested
                    && operation.Progress >= TransitionProgress.ActivationGate)
                {
                    // The bar reaches 85% exactly when the gate releases, then the activation
                    // stretch drives it the rest of the way.
                    overlay?.SetProgress(TransitionProgress.LoadPortion);
                    await operation.ActivateAsync(token).ConfigureAwait(true);
                    continue;
                }

                overlay?.SetProgress(TransitionProgress.Map(operation.Progress));
                await operation.WaitOneFrameAsync(token).ConfigureAwait(true);
            }

            overlay?.SetProgress(1f);
        }
    }
}