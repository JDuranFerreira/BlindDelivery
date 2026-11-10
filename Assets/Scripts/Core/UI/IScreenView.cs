using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.UI
{
    /// <summary>
    /// One screen the <c>UIManager</c> can show. The id is the key the stack and the manager work
    /// by; Show/Hide are the transitions the view owns (they may fade, play a clip, or return
    /// synchronously). A view is not responsible for navigation - it receives it.
    /// </summary>
    public interface IScreenView
    {
        /// <summary>Stable identity inside one region of the app (case-sensitive).</summary>
        string ScreenId { get; }

        /// <summary>Bring this screen into view. Must complete before the manager reports back.</summary>
        Task ShowAsync(CancellationToken token = default);

        /// <summary>Take this screen out of view. Must complete before the manager reports back.</summary>
        Task HideAsync(CancellationToken token = default);
    }
}