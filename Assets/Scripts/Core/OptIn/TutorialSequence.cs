using System;
using System.Collections.Generic;

namespace Template.Core.OptIn
{
    /// <summary>
    /// Bonus system 2 (opt-in file, not base wiring): step-based tutorial. The game defines the
    /// ordered steps (ids are the markers its UI reacts to); the sequence answers the current
    /// step and advances only when the right step completes, so a double-complete or a late UI
    /// event of an old step cannot skip ahead. Saved via <see cref="ProgressIndex"/> — one int
    /// the game persists through the save service and restores in the constructor.
    /// <see cref="SkipAll"/> covers the skippable contract; the game decides which steps offer a
    /// skip button.
    /// </summary>
    public sealed class TutorialSequence
    {
        readonly string[] stepIds;

        /// <summary>Resumes mid-tutorial for returning players; the index was persisted by the game.</summary>
        public TutorialSequence(string[] stepIds, int progressIndex = 0)
        {
            if (stepIds == null || stepIds.Length == 0)
            {
                throw new ArgumentException("At least one tutorial step id is required.", nameof(stepIds));
            }

            HashSet<string> uniqueness = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in stepIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !uniqueness.Add(id))
                {
                    throw new ArgumentException("Tutorial step ids must be non-empty and unique.", nameof(stepIds));
                }
            }

            this.stepIds = (string[])stepIds.Clone();
            ProgressIndex = Math.Clamp(progressIndex, 0, this.stepIds.Length);
        }

        public int StepCount => stepIds.Length;

        /// <summary>
        /// One past the last completed step; equals StepCount once the tutorial is done. Persist
        /// this one int and restore it in the constructor.
        /// </summary>
        public int ProgressIndex { get; private set; }

        /// <summary>Current step id, or null when the whole sequence completed.</summary>
        public string CurrentStep => ProgressIndex < stepIds.Length ? stepIds[ProgressIndex] : null;

        public bool IsComplete => ProgressIndex >= stepIds.Length;

        /// <summary>
        /// Completes the given step: true and advanced only when it is the current one, so a
        /// double-complete is a harmless no-op.
        /// </summary>
        public bool TryComplete(string stepId)
        {
            string current = CurrentStep;
            if (current == null || !string.Equals(current, stepId, StringComparison.Ordinal))
            {
                return false;
            }

            ProgressIndex++;
            return true;
        }

        /// <summary>Skips the rest of the tutorial; there is no backwards path through it.</summary>
        public void SkipAll()
        {
            ProgressIndex = stepIds.Length;
        }
    }
}
