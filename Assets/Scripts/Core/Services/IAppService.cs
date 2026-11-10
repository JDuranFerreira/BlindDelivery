using System.Threading;
using System.Threading.Tasks;

namespace Template.Core.Services
{
    /// <summary>
    /// A long-lived service owned by the bootstrapper. Plain C# — no MonoBehaviour — so the whole
    /// boot chain is testable without a scene.
    ///
    /// InitializeAsync runs once, in registration order, and must not depend on a later service
    /// having started. The order is the composition root's responsibility, not the service's.
    /// </summary>
    public interface IAppService
    {
        /// <summary>Short name used in logs and in <c>AppBootFailed</c>. Stable across runs.</summary>
        string Name { get; }

        Task InitializeAsync(CancellationToken token = default);
    }
}
