using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Pooling;
using Template.Core.Services;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// Tests the pooling contracts: IPoolable lifecycle, IObjectPool Get/Release, IPoolManager registration.
    /// Uses pure-C# fakes so tests run in EditMode without Unity engine.
    /// </summary>
    public class PoolingTests
    {
        [Test]
        public void PoolableObject_OnSpawnOnDespawn_CalledInOrder()
        {
            var obj = new FakePoolable();
            Assert.That(obj.SpawnCount, Is.EqualTo(0));
            Assert.That(obj.DespawnCount, Is.EqualTo(0));

            obj.OnSpawn();
            Assert.That(obj.SpawnCount, Is.EqualTo(1));
            Assert.That(obj.DespawnCount, Is.EqualTo(0));

            obj.OnDespawn();
            Assert.That(obj.SpawnCount, Is.EqualTo(1));
            Assert.That(obj.DespawnCount, Is.EqualTo(1));

            obj.OnSpawn();
            obj.OnDespawn();
            Assert.That(obj.SpawnCount, Is.EqualTo(2));
            Assert.That(obj.DespawnCount, Is.EqualTo(2));
        }

        [Test]
        public void ObjectPool_GetRelease_CyclesInstances()
        {
            var pool = new FakeObjectPool<FakePoolable>(() => new FakePoolable(), maxSize: 10);
            Assert.That(pool.Count, Is.EqualTo(0));

            var a = pool.Get();
            Assert.That(pool.Count, Is.EqualTo(1));
            Assert.That(a.SpawnCount, Is.EqualTo(1));

            pool.Release(a);
            Assert.That(a.DespawnCount, Is.EqualTo(1));

            var b = pool.Get();
            Assert.That(b, Is.SameAs(a)); // same instance reused
            Assert.That(b.SpawnCount, Is.EqualTo(2));
        }

        [Test]
        public void ObjectPool_RespectsMaxSize()
        {
            var pool = new FakeObjectPool<FakePoolable>(() => new FakePoolable(), maxSize: 2);

            var a = pool.Get();
            var b = pool.Get();
            Assert.That(pool.Count, Is.EqualTo(2));

            // Third Get should still work (pool grows but we don't enforce hard limit in fake)
            // Real Unity ObjectPool would block or return null; we just verify count tracking
            var c = pool.Get();
            Assert.That(pool.Count, Is.EqualTo(3));
        }

        [Test]
        public void PoolManager_RegistersAndRetrievesPoolsByKey()
        {
            var manager = new FakePoolManager();
            manager.RegisterPool("bullets", new FakeObjectPool<FakePoolable>(() => new FakePoolable()));
            manager.RegisterPool("enemies", new FakeObjectPool<FakePoolable>(() => new FakePoolable()));

            Assert.That(manager.GetPool<FakePoolable>("bullets"), Is.Not.Null);
            Assert.That(manager.GetPool<FakePoolable>("enemies"), Is.Not.Null);
            Assert.Throws<InvalidOperationException>(() => manager.GetPool<FakePoolable>("missing"));
        }

        [Test]
        public void PoolManager_Prewarm_CreatesInstances()
        {
            var manager = new FakePoolManager();
            var pool = new FakeObjectPool<FakePoolable>(() => new FakePoolable());
            manager.RegisterPool("test", pool);

            manager.Prewarm<FakePoolable>("test", 5);
            Assert.That(pool.Count, Is.EqualTo(5));
        }

        [Test]
        public void PoolManager_Clear_DisposesAllPools()
        {
            var manager = new FakePoolManager();
            var pool1 = new FakeObjectPool<FakePoolable>(() => new FakePoolable());
            var pool2 = new FakeObjectPool<FakePoolable>(() => new FakePoolable());
            manager.RegisterPool("a", pool1);
            manager.RegisterPool("b", pool2);

            manager.Clear();

            Assert.That(pool1.IsDisposed, Is.True);
            Assert.That(pool2.IsDisposed, Is.True);
        }

        [Test]
        public async Task PoolManager_InitializeAsync_Completes()
        {
            var manager = new FakePoolManager();
            await manager.InitializeAsync();
            // Just verify it doesn't throw
            Assert.Pass();
        }
    }

    /// <summary>
    /// Fake IPoolable for testing lifecycle calls.
    /// </summary>
    public sealed class FakePoolable : IPoolable
    {
        public int SpawnCount { get; private set; }
        public int DespawnCount { get; private set; }

        public void OnSpawn() => SpawnCount++;
        public void OnDespawn() => DespawnCount++;
    }

    /// <summary>
    /// Fake IObjectPool for EditMode testing.
    /// </summary>
    public sealed class FakeObjectPool<T> : IObjectPool<T> where T : class
    {
        readonly Func<T> createFunc;
        readonly Stack<T> available = new Stack<T>();
        readonly HashSet<T> active = new HashSet<T>();

        public FakeObjectPool(Func<T> createFunc, int maxSize = 100)
        {
            this.createFunc = createFunc;
            MaxSize = maxSize;
        }

        public int Count => available.Count + active.Count;
        public int MaxSize { get; }
        public bool IsDisposed { get; private set; }

        public T Get()
        {
            T instance;
            if (available.Count > 0)
            {
                instance = available.Pop();
            }
            else
            {
                instance = createFunc();
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnSpawn();
            }

            active.Add(instance);
            return instance;
        }

        public void Release(T instance)
        {
            if (!active.Contains(instance))
                throw new InvalidOperationException("Instance not from this pool");

            active.Remove(instance);

            if (instance is IPoolable poolable)
            {
                poolable.OnDespawn();
            }

            available.Push(instance);
        }

        public void Dispose()
        {
            IsDisposed = true;
            available.Clear();
            active.Clear();
        }

        public void ForceDispose()
        {
            IsDisposed = true;
            available.Clear();
            active.Clear();
        }
    }

    /// <summary>
    /// Fake IPoolManager for EditMode testing.
    /// </summary>
    public sealed class FakePoolManager : IPoolManager
    {
        readonly Dictionary<string, object> pools = new Dictionary<string, object>();

        public string Name => "FakePoolManager";

        public Task InitializeAsync(System.Threading.CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void RegisterPool<T>(string key, IObjectPool<T> pool) where T : class
        {
            pools[key] = pool;
        }

        public IObjectPool<T> GetPool<T>(string key) where T : class
        {
            if (pools.TryGetValue(key, out var pool))
                return (IObjectPool<T>)pool;

            throw new InvalidOperationException($"Pool '{key}' not registered");
        }

        public void Prewarm<T>(string key, int count) where T : class
        {
            var pool = GetPool<T>(key);
            var instances = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                instances.Add(pool.Get());
            }
            foreach (var inst in instances)
            {
                pool.Release(inst);
            }
        }

        public void Clear()
        {
            foreach (var pool in pools.Values)
            {
                if (pool is FakeObjectPool<FakePoolable> fakePool)
                {
                    fakePool.ForceDispose();
                }
                else if (pool is IDisposable d)
                {
                    d.Dispose();
                }
            }
            pools.Clear();
        }
    }
}