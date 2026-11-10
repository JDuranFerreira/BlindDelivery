using System;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Services;

namespace Template.Tests.EditMode.Fakes
{
    /// <summary>Seam used to prove the registry answers interface lookups.</summary>
    public interface IFakeSeam : IAppService
    {
    }

    /// <summary>
    /// Phantom tag types. The registry keys services by type and refuses a second registration of
    /// the same type — the right rule for the real template, where two services sharing a class
    /// would make <c>Get&lt;T&gt;()</c> ambiguous — so tests that need several services use
    /// <see cref="FakeService{TTag}"/>, whose closed generic types are distinct types.
    /// </summary>
    public sealed class TagOne
    {
    }

    public sealed class TagTwo
    {
    }

    public sealed class TagThree
    {
    }

    /// <summary>
    /// A service that records that it ran and can be told to fail. No Unity types: the boot chain
    /// tests stay pure C#.
    /// </summary>
    public sealed class FakeService<TTag> : IFakeSeam
    {
        readonly Action onInitialize;
        readonly Exception failure;

        public FakeService(string name, Action onInitialize = null, Exception failure = null)
        {
            Name = name;
            this.onInitialize = onInitialize;
            this.failure = failure;
        }

        public string Name { get; }

        public int InitializeCount { get; private set; }

        public Task InitializeAsync(CancellationToken token = default)
        {
            InitializeCount++;
            onInitialize?.Invoke();

            return failure != null ? Task.FromException(failure) : Task.CompletedTask;
        }
    }

    /// <summary>A second, unrelated service type, so registrations cannot collide by shape.</summary>
    public sealed class OtherFakeService : IAppService
    {
        public OtherFakeService(string name) => Name = name;

        public string Name { get; }

        public Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;
    }
}
