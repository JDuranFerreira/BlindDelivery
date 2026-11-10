using System;
using System.Collections.Generic;

namespace Template.Core.UI
{
    /// <summary>
    /// Pure navigation history for screens (goal system #6). Stores screens in push order; the top
    /// is current. Pure C# and to the data only - the hiding and showing of actual views belongs
    /// to <c>UIManager</c> (Services), which reads the transitions out of this stack's return
    /// values so the logic can be tested in EditMode without any view in it.
    /// </summary>
    public sealed class ScreenStack
    {
        readonly List<IScreenView> entries = new List<IScreenView>();

        public int Count => entries.Count;

        /// <summary>The screen at the top, or null when the stack is empty.</summary>
        public IScreenView Current => entries.Count > 0 ? entries[entries.Count - 1] : null;

        /// <summary>All screens below the top. The GUI can render a breadcrumb from it.</summary>
        public IReadOnlyList<IScreenView> Entries => entries;

        /// <summary>
        /// Push a screen on top. A duplicate id or a second copy of the same view anywhere in the
        /// stack is refused: two entries for one view would make Back ambiguous about which copy
        /// to pop.
        /// </summary>
        public void Push(IScreenView screen)
        {
            if (screen == null)
            {
                throw new ArgumentNullException(nameof(screen));
            }

            if (string.IsNullOrWhiteSpace(screen.ScreenId))
            {
                throw new InvalidOperationException("A screen needs a non-empty id.");
            }

            if (entries.Contains(screen))
            {
                throw new InvalidOperationException(
                    $"Screen '{screen.ScreenId}' is already pushed once.");
            }

            if (Contains(screen.ScreenId))
            {
                throw new InvalidOperationException(
                    $"A different view already uses the id '{screen.ScreenId}'.");
            }

            entries.Add(screen);
        }

        /// <summary>
        /// Discard the top. The screen below it is returned so the manager can show it; popping
        /// the only screen on the stack is refused - every region keeps its root visible.
        /// </summary>
        public bool TryPop(out IScreenView revealed)
        {
            if (entries.Count <= 1)
            {
                revealed = null;
                return false;
            }

            entries.RemoveAt(entries.Count - 1);
            revealed = Current;
            return true;
        }

        /// <summary>Pop everything above the given screen without popping it.</summary>
        public void PopTo(IScreenView target)
        {
            int index = target == null ? -1 : entries.IndexOf(target);
            if (index < 0)
            {
                throw new InvalidOperationException("The target screen is not in this stack.");
            }

            entries.RemoveRange(index + 1, entries.Count - index - 1);
        }

        /// <summary>Drop the whole history. Used when the region changes (scene loads).</summary>
        public void Clear() => entries.Clear();

        public bool Contains(string screenId)
            => entries.Exists(es => string.Equals(es.ScreenId, screenId, StringComparison.Ordinal));
    }
}