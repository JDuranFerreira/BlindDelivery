using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.Scenes
{
    /// <summary>
    /// One in-flight scene load as the transition sequencer drives it, or one queued task in
    /// general. A Unity <c>AsyncOperation</c> holds its progress at 0.9 until
    /// <c>allowSceneActivation</c> goes true - this interface makes that gate explicit so the
    /// sequencer can decide *when* to release it instead of Unity doing so frame one. It also
    /// gives the sequencer a heartbeat (<see cref="WaitOneFrameAsync"/>) that works in real play
    /// (one frame of Awaitable) and in EditMode tests (a fake advancing its own progress).
    /// </summary>
    public interface ISceneOperation
    {
        /// <summary>The raw 0..1 progress the driver reports, 0.9 while held at the gate.</summary>
        float Progress { get; }

        /// <summary>True once the scene has fully activated.</summary>
        bool IsDone { get; }

        /// <summary>True after <see cref="ActivateAsync"/> was called (idempotent).</summary>
        bool ActivationRequested { get; }

        /// <summary>Release the activation gate. Unity then finishes the activation itself.</summary>
        Task ActivateAsync(CancellationToken token = default);

        /// <summary>
        /// Wait for the next tick of the driver (one frame in play). Backed by
        /// <c>Awaitable.NextFrameAsync</c> in the engine adapter, so the sequencer's polling loop
        /// costs nothing between frames.
        /// </summary>
        Task WaitOneFrameAsync(CancellationToken token = default);
    }
}