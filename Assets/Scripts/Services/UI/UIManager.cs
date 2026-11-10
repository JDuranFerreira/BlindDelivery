using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Services;
using Template.Core.UI;

namespace Template.Services.UI
{
    /// <summary>
    /// The navigation service (goal system #6). It owns the screen registry, the pure
    /// <see cref="ScreenStack"/> and the pure <see cref="PopupQueue"/>; the actual view
    /// Show/Hide calls go through <see cref="IScreenView"/> so the whole flow runs in EditMode
    /// tests with fake views. One invariant carries the design: inside a region of the app, only
    /// the top of the stack is visible - pushing hides the previous top, backing reveals it again,
    /// so no screen ever fights another for input.
    ///
    /// The service stays plain C# (an <see cref="IAppService"/>, not a MonoBehaviour): views are
    /// registered by whatever composes them, popups reach the engine through an
    /// <see cref="IPopupHost"/> the game binds. Android's physical back button is a M4 input
    /// concern - the hook is <see cref="BackAsync"/>, which the input service will call.
    /// </summary>
    public sealed class UIManager : IAppService
    {
        const string ServiceName = "UI";

        readonly ScreenStack screens = new ScreenStack();
        readonly PopupQueue popups = new PopupQueue();
        readonly Dictionary<string, IScreenView> screensById = new Dictionary<string, IScreenView>();

        IPopupHost popupHost;

        public string Name => ServiceName;

        /// <summary>The pure navigation state, exposed read-only for breadcrumbs and tests.</summary>
        public ScreenStack Stack => screens;

        public PopupQueue Popups => popups;

        public Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;

        /// <summary>Makes a view navigable. Two views with one id need two ids, not an excuse.</summary>
        public void RegisterScreen(IScreenView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (string.IsNullOrWhiteSpace(view.ScreenId))
            {
                throw new InvalidOperationException("A screen needs a non-empty id.");
            }

            if (screensById.ContainsKey(view.ScreenId))
            {
                throw new InvalidOperationException(
                    $"Screen id '{view.ScreenId}' is registered a second time.");
            }

            screensById[view.ScreenId] = view;
        }

        /// <summary>Sets the root of the region and drops the whole history. Bottom of the stack.</summary>
        public async Task SetRootAsync(string screenId, CancellationToken token = default)
        {
            IScreenView root = Require(screenId);

            if (screens.Current != null)
            {
                await screens.Current.HideAsync(token).ConfigureAwait(true);
            }

            // Hiding only the top is right: every deeper screen was hidden when it lost top spot,
            // so after a Clear the region below shows exactly this root and nothing else.
            screens.Clear();
            screens.Push(root);
            await root.ShowAsync(token).ConfigureAwait(true);
        }

        /// <summary>Shows a screen on top of the current one. Idempotent when it is already top.</summary>
        public async Task PushAsync(string screenId, CancellationToken token = default)
        {
            IScreenView next = Require(screenId);
            IScreenView previous = screens.Current;

            if (previous != null && ReferenceEquals(previous, next))
            {
                return;
            }

            screens.Push(next);
            if (previous != null)
            {
                await previous.HideAsync(token).ConfigureAwait(true);
            }

            await next.ShowAsync(token).ConfigureAwait(true);
        }

        /// <summary>
        /// One step of back navigation: pop the top and show what is below it. False at the root -
        /// the caller decides what the root's back button means (Android quit dialog, nothing).
        /// </summary>
        public async Task<bool> BackAsync(CancellationToken token = default)
        {
            IScreenView top = screens.Current;
            if (top == null || !screens.TryPop(out IScreenView revealed))
            {
                return false;
            }

            await top.HideAsync(token).ConfigureAwait(true);
            if (revealed != null)
            {
                await revealed.ShowAsync(token).ConfigureAwait(true);
            }

            return true;
        }

        /// <summary>Queues a popup. Duplicates of the active or a queued id are dropped to one.</summary>
        public void EnqueuePopup(PopupRequest request) => popups.Enqueue(request);

        /// <summary>Replaces the queue. It does not dismiss an already-started popup.</summary>
        public void ClearPopups() => popups.Clear();

        /// <summary>Which host shows the queued popups. Bind once; a second bind replaces it.</summary>
        public void BindPopups(IPopupHost host)
        {
            popupHost = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Shows queued popups one by one through the bound host. Blocks until the queue is empty
        /// (or throws when nobody is bound - a silent drop would swallow game feedback).
        /// </summary>
        public async Task PumpPopupsAsync(CancellationToken token = default)
        {
            if (popupHost == null)
            {
                throw new InvalidOperationException(
                    "No IPopupHost bound. Call BindPopups before pumping.");
            }

            while (popups.TryStartNext(out PopupRequest next))
            {
                await popupHost.ShowAsync(next, token).ConfigureAwait(true);
                popups.TryComplete(out _);
            }
        }

        IScreenView Require(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                throw new ArgumentException("A screen id is required.", nameof(screenId));
            }

            if (!screensById.TryGetValue(screenId, out IScreenView view))
            {
                throw new InvalidOperationException(
                    $"No screen registered as '{screenId}'. Call RegisterScreen first.");
            }

            return view;
        }
    }
}