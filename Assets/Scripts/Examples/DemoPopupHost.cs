using System.Threading;
using System.Threading.Tasks;
using Template.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Template.Examples
{
    /// <summary>
    /// A concrete IPopupHost: the engine side that actually displays the queued popups the
    /// UIManager hands over one at a time. ShowAsync completes when the player dismisses it, so
    /// the queue cannot advance past an open popup. A game binds its own styled host (confirm,
    /// reward, error, no-internet) the same way — this one is only the shape.
    /// </summary>
    public sealed class DemoPopupHost : MonoBehaviour, IPopupHost
    {
        TaskCompletionSource<PopupRequest> pending;
        Text titleText;
        Text bodyText;

        void Awake()
        {
            if (titleText == null) titleText = transform.Find("Card/PopupTitle").GetComponent<Text>();
            if (bodyText == null) bodyText = transform.Find("Card/PopupBody").GetComponent<Text>();
        }

        public Task ShowAsync(PopupRequest request, CancellationToken token = default)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            titleText.text = request.Title;
            bodyText.text = string.IsNullOrEmpty(request.ConfirmLabel)
                ? request.Message
                : request.Message + "\n\nbuttons: " + request.ConfirmLabel
                    + (request.CancelLabel == null ? "" : " / " + request.CancelLabel);

            pending = new TaskCompletionSource<PopupRequest>();
            return pending.Task;
        }

        public Task CloseAsync(CancellationToken token = default)
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            return Task.CompletedTask;
        }

        /// <summary>Wire the popup's OK button onClick here (the scene builder does).</summary>
        public void Confirm()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            pending?.TrySetResult(default);
            pending = null;
        }
    }
}