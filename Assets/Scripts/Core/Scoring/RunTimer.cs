namespace BlindDelivery.Core.Scoring
{
    /// <summary>
    /// Fixed-step run clock (GDD §5). Pure C# so timing rules stay testable without a scene;
    /// the simulation advances it by the same fixed step used for gameplay.
    /// </summary>
    public sealed class RunTimer
    {
        float elapsed;
        bool running;

        public float ElapsedSeconds => elapsed;

        /// <summary>Resets and starts counting. Called when the prep countdown ends.</summary>
        public void Start()
        {
            elapsed = 0f;
            running = true;
        }

        public void Stop() => running = false;

        public void Step(float deltaTime)
        {
            if (running && deltaTime > 0f) elapsed += deltaTime;
        }
    }
}
