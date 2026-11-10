using System;

namespace Template.Core.Boot
{
    /// <summary>
    /// What <see cref="AppBoot.RunAsync"/> hands back. The boot events on the bus say *which*
    /// service reported progress; this says whether the app is usable.
    /// </summary>
    public readonly struct BootResult
    {
        BootResult(bool succeeded, bool cancelled, string failedService, Exception error)
        {
            Succeeded = succeeded;
            WasCancelled = cancelled;
            FailedService = failedService;
            Error = error;
        }

        public static BootResult Success { get; } = new BootResult(true, false, null, null);

        public static BootResult Cancelled { get; } = new BootResult(false, true, null, null);

        public static BootResult Failure(string serviceName, Exception error) =>
            new BootResult(false, false, serviceName, error);

        public bool Succeeded { get; }

        public bool WasCancelled { get; }

        /// <summary>The service that threw, or null when the boot succeeded or was cancelled.</summary>
        public string FailedService { get; }

        public Exception Error { get; }
    }
}
