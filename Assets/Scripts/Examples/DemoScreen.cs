using System.Threading;
using System.Threading.Tasks;
using Template.Core.UI;
using UnityEngine;

namespace Template.Examples
{
    /// <summary>
    /// One IScreenView over a full-screen panel (goals note system 6). The view owns Show/Hide —
    /// these would be fades or animation hooks in a game; the UIManager owns the navigation and
    /// only pulls these tasks. The demo scene fades what it can with a CanvasGroup; a game swaps
    /// in its own animations without touching the manager.
    /// </summary>
    public sealed class DemoScreen : MonoBehaviour, IScreenView
    {
        [SerializeField] string screenId = "demo_screen";
        [SerializeField] CanvasGroup group;

        public string ScreenId => screenId;

        void Awake()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
        }

        /// <summary>The demo sets a fresh id per visit — see SystemsDemoRunner.OnUiPush.</summary>
        public void SetId(string value)
        {
            screenId = value;
        }

        public Task ShowAsync(CancellationToken token = default)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            return Task.CompletedTask;
        }

        public Task HideAsync(CancellationToken token = default)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            return Task.CompletedTask;
        }
    }
}