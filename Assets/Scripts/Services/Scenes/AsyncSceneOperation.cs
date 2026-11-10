using System.Threading;
using System.Threading.Tasks;
using Template.Core.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Template.Services.Scenes
{
    /// <summary>
    /// The <see cref="ISceneOperation"/> adapter over a Unity <c>AsyncOperation</c> (a
    /// <c>LoadSceneAsync</c> or <c>UnloadSceneAsync</c> result). Activation is the explicit
    /// <c>allowSceneActivation</c> write, the heartbeat is <c>Awaitable.NextFrameAsync</c>, and
    /// progress is exactly what Unity reports — its stall at 0.9 is the contract the sequencer
    /// was built around.
    /// </summary>
    public sealed class AsyncSceneOperation : ISceneOperation
    {
        readonly UnityEngine.AsyncOperation raw;

        public AsyncSceneOperation(UnityEngine.AsyncOperation raw)
        {
            this.raw = raw != null
                ? raw
                : throw new System.ArgumentNullException(nameof(raw));
        }

        public float Progress => raw.progress;

        public bool IsDone => raw.isDone;

        public bool ActivationRequested { get; private set; }

        public Task ActivateAsync(CancellationToken token = default)
        {
            if (!ActivationRequested)
            {
                raw.allowSceneActivation = true;
                ActivationRequested = true;
            }

            return Task.CompletedTask;
        }

        public async Task WaitOneFrameAsync(CancellationToken token = default)
        {
            await Awaitable.NextFrameAsync(token); // heartbeat on the engine clock, wrapped for the Core seam
        }
    }
}