using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.Scenes
{
    /// <summary>
    /// The seam every scene transition goes through. Direct scene loading behind this interface
    /// now; an Addressables-backed implementation can replace it later without touching callers
    /// (architecture note, Decisions table).
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>Covered single load: fade out, progress, activate, fade in.</summary>
        Task LoadAsync(string sceneName, CancellationToken token = default);

        /// <summary>
        /// Load a scene on top of the ones live, without the cover (what is on screen already
        /// belongs to the player). Progress without fades; use for level chunks and overlays.
        /// </summary>
        Task LoadAdditiveAsync(string sceneName, CancellationToken token = default);

        /// <summary>Unload a scene this loader added additively. Errors if it never added it.</summary>
        Task UnloadAsync(string sceneName, CancellationToken token = default);
    }
}
