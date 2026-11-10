using System;
using System.Collections.Generic;

namespace Template.Core.UI
{
    /// <summary>
    /// The sequencing half of popups (goal system #6: pooled/cached popups for confirm, reward,
    /// error, no internet). Requests line up here; one at a time is handed to the
    /// <c>UIManager</c>'s <c>IPopupHost</c>, and the next only starts once the current one is
    /// out of view. Pure C# - the queue has views nowhere and is tested in EditMode.
    ///
    /// A request that duplicates the active one or one already queued with the same id is dropped:
    /// "no internet" firing five times in a second should show one popup, not a pile of them.
    /// </summary>
    public sealed class PopupQueue
    {
        readonly Queue<PopupRequest> pending = new Queue<PopupRequest>();

        PopupRequest active;
        bool showing;

        /// <summary>Requests waiting their turn, not counting the one on screen.</summary>
        public int PendingCount => pending.Count;

        /// <summary>True between <see cref="TryStartNext"/> and <see cref="TryComplete"/>.</summary>
        public bool IsShowing => showing;

        public void Enqueue(PopupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PopupId))
            {
                throw new ArgumentException("A popup needs an id.", nameof(request));
            }

            if (showing && string.Equals(active.PopupId, request.PopupId, StringComparison.Ordinal))
            {
                return;
            }

            foreach (PopupRequest queued in pending)
            {
                if (string.Equals(queued.PopupId, request.PopupId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            pending.Enqueue(request);
        }

        /// <summary>
        /// Hands out the next request, or false when one is already on screen or the queue is
        /// empty - the caller pumps this until it returns false.
        /// </summary>
        public bool TryStartNext(out PopupRequest request)
        {
            if (showing || pending.Count == 0)
            {
                request = default;
                return false;
            }

            active = pending.Dequeue();
            showing = true;
            request = active;
            return true;
        }

        /// <summary>Closes the active request so the next one can start. False when idle.</summary>
        public bool TryComplete(out PopupRequest completed)
        {
            if (!showing)
            {
                completed = default;
                return false;
            }

            completed = active;
            active = default;
            showing = false;
            return true;
        }

        /// <summary>Drop everything queued. Does not close an already-started popup.</summary>
        public void Clear()
        {
            pending.Clear();
            active = default;
            showing = false;
        }
    }
}