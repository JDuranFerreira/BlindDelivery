using System;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Analytics;
using Template.Core.RemoteConfig;

namespace Template.Tests.EditMode.Fakes
{
    /// <summary>Records every call, so consent-gating tests assert on provider traffic.</summary>
    public sealed class FakeAnalyticsSink : IAnalyticsSink
    {
        public int InitializeCount { get; private set; }

        public int ShutdownCount { get; private set; }

        public System.Collections.Generic.List<string> Logged { get; } =
            new System.Collections.Generic.List<string>();

        public bool FailInitialization { get; set; }

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            InitializeCount++;
            if (!FailInitialization)
            {
                IsInitialized = true;
            }
        }

        public void LogEvent(string name, AnalyticsParam[] parameters)
        {
            Logged.Add(name);
        }

        public void Shutdown()
        {
            ShutdownCount++;
            IsInitialized = false;
        }
    }

    /// <summary>A configurable remote source: script the outcome of one fetch attempt.</summary>
    public sealed class FakeRemoteConfigSource : IRemoteConfigSource
    {
        public RemoteConfigStatus Outcome { get; set; } = RemoteConfigStatus.Fetched;

        /// <summary>Values stamped into the document on a successful fetch.</summary>
        public (string key, string value)[] Values { get; set; }

        public string Version { get; set; } = "remote-1";

        /// <summary>When set, FetchAsync hangs until the token fires — the timeout path.</summary>
        public bool HangForever { get; set; }

        public async Task<RemoteConfigFetchResult> FetchAsync(
            RemoteConfigDocument document, CancellationToken token)
        {
            if (HangForever)
            {
                await Task.Delay(Timeout.Infinite, token);
            }

            if (Outcome == RemoteConfigStatus.Fetched)
            {
                document.version = Version;
                document.values = new RemoteConfigValue[Values.Length];
                for (int i = 0; i < Values.Length; i++)
                {
                    document.values[i] = new RemoteConfigValue
                    {
                        key = Values[i].key,
                        value = Values[i].value,
                    };
                }
            }

            return new RemoteConfigFetchResult(Outcome);
        }
    }
}
