using System;
using Template.Core.Services;

namespace Template.Core.Pooling
{
    /// <summary>
    /// Central registry of pools keyed by a string id (typically the prefab name or a game-specific key).
    /// </summary>
    public interface IPoolManager : IAppService
    {
        /// <summary>Get or create a pool for the given key.</summary>
        IObjectPool<T> GetPool<T>(string key) where T : class;

        /// <summary>Pre-warm a pool to a target count during loading screens.</summary>
        void Prewarm<T>(string key, int count) where T : class;

        /// <summary>Release all pools and clear the registry.</summary>
        void Clear();
    }
}