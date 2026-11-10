using System;
using UnityEngine;
using Template.Core.Pooling;

namespace Template.Services.Pooling
{
    /// <summary>
    /// Factory component that knows how to create pooled objects for specific keys.
    /// Attach to the same GameObject as PoolManager. Each game adds its own entries.
    /// </summary>
    [AddComponentMenu("Template/Pooling/Pool Factory")]
    public class PoolFactory : MonoBehaviour
    {
        [Serializable]
        public class PoolEntry
        {
            public string key;
            public GameObject prefab;
            public int prewarmCount = 10;
        }

        public PoolEntry[] pools;

        public Func<T> GetCreateFunc<T>(string key) where T : class
        {
            foreach (var entry in pools)
            {
                if (entry.key == key && entry.prefab != null)
                {
                    return () =>
                    {
                        var go = Instantiate(entry.prefab);
                        go.SetActive(false);
                        return go.GetComponent<T>();
                    };
                }
            }
            return null;
        }
    }
}