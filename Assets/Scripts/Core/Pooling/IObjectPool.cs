using System;

namespace Template.Core.Pooling
{
    /// <summary>
    /// Minimal pool interface — get and return. The generic keeps call sites clean.
    /// </summary>
    /// <typeparam name="T">The pooled type (typically a component or MonoBehaviour).</typeparam>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>Take an instance from the pool (creates if empty and under max).</summary>
        T Get();

        /// <summary>Return an instance to the pool.</summary>
        void Release(T instance);

        /// <summary>Current count of instances in the pool (idle + active if tracked).</summary>
        int Count { get; }

        /// <summary>Maximum pool size (0 = unbounded).</summary>
        int MaxSize { get; }
    }

    /// <summary>
    /// Factory for creating pooled instances. Keeps pool logic separate from object creation.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    public interface IPoolFactory<T> where T : class
    {
        T Create();
    }
}