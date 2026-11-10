namespace Template.Core.UI
{
    /// <summary>
    /// Load progress as the bar reports it. A Unity scene operation reports 0.0 while loading the
    /// scene's data, holds everything until activation, then finishes 0.9-1.0 after
    /// <c>allowSceneActivation</c> was allowed. To the user the bar should keep creeping all the
    /// way to 100% - this maps the raw progress of the original op to a friendly 0..1 where 0..85%
    /// is data loading and 85..100% is activation (M3 plan: real progress, stall at the 0.9 gate).
    /// </summary>
    public static class TransitionProgress
    {
        /// <summary>The raw scene operation progress that triggers activation.</summary>
        public const float ActivationGate = 0.9f;

        /// <summary>The portion of the bar that finish before activation is allowed.</summary>
        public const float LoadPortion = 0.85f;

        public static float Map(float sceneOperationProgress)
        {
            float p = sceneOperationProgress;
            if (p <= 0f)
            {
                return 0f;
            }

            if (p >= ActivationGate)
            {
                // The activation stretch alone is 0..1; the bar spends it closing the last
                // (1 - LoadPortion) of itself, so it never stalls at 85% on screen.
                float activation = (p - ActivationGate) / (1f - ActivationGate);
                return LoadPortion + (activation * (1f - LoadPortion));
            }

            return (p / ActivationGate) * LoadPortion;
        }
    }
}