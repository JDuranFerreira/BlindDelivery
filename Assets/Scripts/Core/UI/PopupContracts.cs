using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.UI
{
    /// <summary>
    /// What a popup is to say and do. Modest by design: title, body and two action labels - the
    /// concrete confirm/reward/error/no-internet popups are views in the game that receive this.
    /// A single PopupId selects the view to use, so every request also works as intent.
    /// </summary>
    public readonly struct PopupRequest
    {
        public PopupRequest(string popupId, string title, string message,
            string confirmLabel = null, string cancelLabel = null)
        {
            PopupId = popupId;
            Title = title;
            Message = message;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
        }

        /// <summary>Which registered popup shows this. Case-sensitive.</summary>
        public string PopupId { get; }

        public string Title { get; }

        public string Message { get; }

        public string ConfirmLabel { get; }

        public string CancelLabel { get; }
    }

    /// <summary>
    /// Engine or game code that can display one popup kind. Services never news these up; files
    /// for popups are registered file-type views. Reuse via the queued Show/Close workflow.
    /// </summary>
    public interface IPopupView
    {
        /// <summary>The popup id this view answers to (used to find it by request).</summary>
        string PopupId { get; }

        /// <summary>Display the request and complete when it is fully visible.</summary>
        Task ShowAsync(PopupRequest request, CancellationToken token = default);

        /// <summary>Leave the popup out of view and complete if it is gone (pooled or inactive).</summary>
        Task CloseAsync(CancellationToken token = default);
    }

    /// <summary>
    /// The engine side that actually displays queued popups. The <c>UIManager</c> hands it one
    /// <see cref="PopupRequest"/> at a time and waits for dismissal before the next; locating or
    /// instantiating the concrete view is the host's business (cached or pooled per Prefab).
    /// </summary>
    public interface IPopupHost
    {
        /// <summary>Display the request and complete when the user dismissed it.</summary>
        Task ShowAsync(PopupRequest request, CancellationToken token = default);
    }
}