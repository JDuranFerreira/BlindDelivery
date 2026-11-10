using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.Scenes
{
    /// <summary>
    /// The presentation side of a scene transition: fade to a cover, display progress, fade back.
    /// Lives behind an interface because it is presentation - load logic does not know Canvas.
    /// A null overlay is legal (headless runs, or a game that wants its own load flow); the loader
    /// then loads without fades.
    /// </summary>
    public interface ISceneTransitionOverlay
    {
        bool IsVisible { get; }

        /// <summary>Fade to opaque. Reset the progress bar before returning.</summary>
        Task FadeOutAsync(CancellationToken token = default);

        /// <summary>Called during loading with the bar's 0..1 position (see TransitionProgress).</summary>
        void SetProgress(float progress01);

        /// <summary>Fade back to the freshly-loaded scene on the cover.</summary>
        Task FadeInAsync(CancellationToken token = default);
    }
}