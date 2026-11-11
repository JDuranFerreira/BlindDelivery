using System;

namespace BlindDelivery.Core.OptIn
{
    /// <summary>
    /// Bonus system 6, half one (opt-in file, not base wiring): a rolling FPS monitor fed frame
    /// deltas by whoever owns the player loop (a debug overlay, a game update hook). Pure C# â€”
    /// the average is count / sum(dt) over the last window frames, so it is exact and
    /// unit-testable. The tier classifier pairs with it in Services; this class only measures.
    /// </summary>
    public sealed class FpsMonitor
    {
        readonly float[] window;
        int windowIndex;
        int filled;
        float windowSeconds;
        float worstFps;

        public FpsMonitor(int windowFrames = 60)
        {
            if (windowFrames < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(windowFrames), "The window must hold at least one frame.");
            }

            window = new float[windowFrames];
        }

        /// <summary>Rolling average of the last window frames, or 0 before the first frame.</summary>
        public float AverageFps => filled == 0 ? 0f : filled / windowSeconds;

        /// <summary>Slowest single frame seen since construction, in frames-per-second terms.</summary>
        public float WorstFps => worstFps;

        /// <summary>Feeds one frame delta. Non-positive deltas are ignored, not divided on.</summary>
        public void AddFrame(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (filled == window.Length)
            {
                windowSeconds -= window[windowIndex];
            }
            else
            {
                filled++;
            }

            window[windowIndex] = deltaTime;
            windowSeconds += deltaTime;
            windowIndex = (windowIndex + 1) % window.Length;

            float frameFps = 1f / deltaTime;
            worstFps = filled == 1 || frameFps < worstFps ? frameFps : worstFps;
        }
    }
}

