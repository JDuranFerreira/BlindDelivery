using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Events;
using Template.Core.Scenes;
using Template.Core.Services;
using Template.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Template.Services.Scenes
{
    /// <summary>
    /// The real <see cref="ISceneLoader"/>: covered, reporting transitions (M3) through the pure
    /// <see cref="SceneTransitionSequencer"/> in Core. Single loads take the overlay through its
    /// full choreography; additive loads without a cover (they stack on top of visible scenes) and
    /// unload without one. <c>Resources.UnloadUnusedAssets</c> runs after each single transition,
    /// behind an optional hook so tests or a later pipeline can replace the call.
    /// When the overlay is the M3 boot one, this is also what gives the 'Press Play anywhere' boot
    /// its progress bar.
    /// </summary>
    public sealed class TransitionSceneLoader : ISceneLoader, IAppService
    {
        const string ServiceName = "SceneLoader";

        readonly IEventBus bus;
        readonly ISceneTransitionOverlay overlay;
        readonly Func<Task> unloadUnusedAssets;
        readonly SceneTransitionSequencer sequencer;
        readonly List<string> additiveScenes = new List<string>();

        public TransitionSceneLoader(
            IEventBus bus,
            ISceneTransitionOverlay overlay = null,
            Func<Task> unloadUnusedAssets = null)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.overlay = overlay;
            this.unloadUnusedAssets = unloadUnusedAssets;
            sequencer = new SceneTransitionSequencer(overlay);
        }

        public string Name => ServiceName;

        /// <summary>Nothing to warm up: the sequencer holds the decisions and it is already made.</summary>
        public Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;

        public async Task LoadAsync(string sceneName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A scene name is required.", nameof(sceneName));
            }

            bus.Publish(new SceneTransitionStarted(sceneName));
            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();

            await sequencer
                .RunAsync(sceneName, CreateSingleOperation, token)
                .ConfigureAwait(true);

            await RunUnloadUnusedAssetsAsync(token).ConfigureAwait(true);

            elapsed.Stop();
            bus.Publish(new SceneTransitionCompleted(sceneName, (float)elapsed.Elapsed.TotalSeconds));
        }

        public async Task LoadAdditiveAsync(string sceneName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A scene name is required.", nameof(sceneName));
            }

            AsyncSceneOperation operation = CreateOperation(
                sceneName,
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive),
                additive: true);

            await RunToCompletionAsync(operation, token).ConfigureAwait(true);
            additiveScenes.Add(sceneName);
        }

        public async Task UnloadAsync(string sceneName, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A scene name is required.", nameof(sceneName));
            }

            // Not tracked here: UnloadSceneAsync cannot refuse a single-loaded active scene,
            // that is the caller's bookkeeping. The list exists so a future flow can unload all.
            if (!additiveScenes.Remove(sceneName))
            {
                Debug.LogWarning(
                    $"[TransitionSceneLoader] Unloading '{sceneName}', which this loader never added. It may leave a hole someone else tracks.");
            }

            AsyncSceneOperation operation = new AsyncSceneOperation(
                SceneManager.UnloadSceneAsync(sceneName));

            await RunToCompletionAsync(operation, token).ConfigureAwait(true);
        }

        AsyncSceneOperation CreateSingleOperation(string sceneName)
        {
            return CreateOperation(
                sceneName,
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single),
                additive: false);
        }

        AsyncSceneOperation CreateOperation(
            string sceneName,
            UnityEngine.AsyncOperation raw,
            bool additive)
        {
            if (raw == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{sceneName}' could not be loaded (additive: {additive}). Add it to Build Settings.");
            }

            return new AsyncSceneOperation(raw);
        }

        async Task RunToCompletionAsync(AsyncSceneOperation operation, CancellationToken token)
        {
            while (!operation.IsDone)
            {
                token.ThrowIfCancellationRequested();
                await operation.WaitOneFrameAsync(token).ConfigureAwait(true);
            }
        }

        async Task RunUnloadUnusedAssetsAsync(CancellationToken token)
        {
            // Resources.UnloadUnusedAssets is the engine default (set by the composition root);
            // a game with Addressables or its own pipeline replaces the hook, not this class.
            if (unloadUnusedAssets != null)
            {
                await unloadUnusedAssets().ConfigureAwait(true);
            }
        }
    }
}