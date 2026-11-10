namespace Template.Core.Pooling
{
    /// <summary>
    /// Implement on pooled objects to reset state when they are taken from or returned to the pool.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called when the object is spawned from the pool. Reset runtime state here.</summary>
        void OnSpawn();

        /// <summary>Called when the object is returned to the pool. Clean up references here.</summary>
        void OnDespawn();
    }
}