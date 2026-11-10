using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using Template.Core.Pooling;
using Template.Core.Services;
using UPool = UnityEngine.Pool;

namespace Template.Services.Pooling
{
    /// <summary>
    /// Unity ObjectPool-backed implementation of IObjectPool. Wraps Unity's pool
    /// and adds IPoolable lifecycle calls.
    /// </summary>
    internal sealed class UnityObjectPool<T> : Template.Core.Pooling.IObjectPool<T> where T : class
    {
        readonly UPool.ObjectPool<T> pool;
        readonly bool callPoolableLifecycle;

        public UnityObjectPool(Func<T> createFunc, Action<T> actionOnGet, Action<T> actionOnRelease,
            Action<T> actionOnDestroy, bool collectionCheck, int defaultCapacity, int maxSize)
        {
            this.maxSize = maxSize;
            pool = new UPool.ObjectPool<T>(createFunc, actionOnGet, actionOnRelease, actionOnDestroy,
                collectionCheck, defaultCapacity, maxSize);
            callPoolableLifecycle = typeof(IPoolable).IsAssignableFrom(typeof(T));
        }

        public int Count => pool.CountAll;
        public int MaxSize => maxSize;

        readonly int maxSize;

        public T Get()
        {
            var instance = pool.Get();
            if (callPoolableLifecycle && instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }
            return instance;
        }

        public void Release(T instance)
        {
            if (callPoolableLifecycle && instance is IPoolable poolable)
            {
                poolable.OnDespawn();
            }
            pool.Release(instance);
        }
    }

    /// <summary>
    /// Central PoolManager — holds all pools keyed by string id (prefab name or game-specific key).
    /// Uses Unity's ObjectPool under the hood; pre-warms on demand.
    /// </summary>
    [AddComponentMenu("Template/Pooling/Pool Manager")]
    public sealed class PoolManager : MonoBehaviour, IPoolManager
    {
        public const string ServiceName = "PoolManager";

        readonly Dictionary<string, object> pools = new Dictionary<string, object>();
        readonly Dictionary<string, Type> poolTypes = new Dictionary<string, Type>();

        public string Name => ServiceName;

        public async Task InitializeAsync(CancellationToken token = default)
        {
            // Nothing to initialize; pools are created lazily on first GetPool/Prewarm
            await Task.Yield();
        }

        public Template.Core.Pooling.IObjectPool<T> GetPool<T>(string key) where T : class
        {
            if (pools.TryGetValue(key, out var existing))
            {
                return (Template.Core.Pooling.IObjectPool<T>)existing;
            }

            // Pool not found — caller must have registered the factory via Prewarm first
            throw new InvalidOperationException($"Pool '{key}' not registered. Call Prewarm<T>(key, count) first to register the factory.");
        }

        public void Prewarm<T>(string key, int count) where T : class
        {
            if (pools.ContainsKey(key))
            {
                // Already exists — maybe just ensure capacity
                if (pools[key] is UnityObjectPool<T> existingPool && existingPool.Count < count)
                {
                    // Unity's ObjectPool doesn't expose Prewarm directly, so we Get/Release
                    for (int i = existingPool.Count; i < count; i++)
                    {
                        var instance = existingPool.Get();
                        existingPool.Release(instance);
                    }
                }
                return;
            }

            // Need a factory — but we don't have one here. This is a design gap.
            // For now, require a component on the manager or a registered factory.
            // We'll use a convention: look for a PoolFactory component on this GameObject.
            var factory = GetComponent<PoolFactory>();
            if (factory == null)
            {
                throw new InvalidOperationException($"No PoolFactory found on PoolManager to create pool '{key}'.");
            }

            var createFunc = factory.GetCreateFunc<T>(key);
            if (createFunc == null)
            {
                throw new InvalidOperationException($"PoolFactory has no create func for '{key}' (type {typeof(T).Name}).");
            }

            var pool = new UnityObjectPool<T>(
                createFunc,
                actionOnGet: null,
                actionOnRelease: null,
                actionOnDestroy: null,
                collectionCheck: true,
                defaultCapacity: count,
                maxSize: count * 2 // allow some growth
            );

            // Pre-warm
            var instances = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                instances.Add(pool.Get());
            }
            foreach (var inst in instances)
            {
                pool.Release(inst);
            }

            pools[key] = pool;
            poolTypes[key] = typeof(T);
        }

        public void Clear()
        {
            foreach (var pool in pools.Values)
            {
                if (pool is IDisposable d) d.Dispose();
            }
            pools.Clear();
            poolTypes.Clear();
        }
    }
}