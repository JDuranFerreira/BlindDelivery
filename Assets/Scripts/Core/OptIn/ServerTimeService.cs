using System;

namespace Template.Core.OptIn
{
    /// <summary>
    /// Bonus system 3 (opt-in file, not base wiring): server time against clock cheating. A game
    /// hands a trusted server UTC timestamp to <see cref="SynchronizeWithServer"/> whenever one
    /// arrives (a login, a remote config poll); from then on <see cref="NowUtc"/> answers server
    /// time derived from the measured offset. Before the first sync it answers device time, and
    /// the honest <see cref="IsSynchronized"/> flag tells the game whether its timers are
    /// cheat-proof. The offset is one immutable tick snapshot — no drifting state.
    /// </summary>
    public sealed class ServerTimeService
    {
        readonly Func<DateTime> deviceNowUtc;

        long offsetTicks;

        public ServerTimeService(Func<DateTime> deviceNowUtc = null)
        {
            this.deviceNowUtc = deviceNowUtc ?? DefaultNowUtc;
        }

        static DateTime DefaultNowUtc() => DateTime.UtcNow;

        public bool IsSynchronized { get; private set; }

        /// <summary>Server minus device, in ticks. 0 until the first sync.</summary>
        public long OffsetTicks => offsetTicks;

        /// <summary>Measures the device offset against a trusted server timestamp. The newest sync wins.</summary>
        public void SynchronizeWithServer(DateTime serverUtc)
        {
            if (serverUtc.Kind != DateTimeKind.Utc && serverUtc != default)
            {
                serverUtc = serverUtc.ToUniversalTime();
            }

            offsetTicks = (serverUtc - deviceNowUtc()).Ticks;
            IsSynchronized = true;
        }

        /// <summary>The honest fallback: answer device time again, and say so via IsSynchronized.</summary>
        public void ForgetServerTime()
        {
            offsetTicks = 0;
            IsSynchronized = false;
        }

        /// <summary>Server time when synchronized, device time otherwise. Timers read this, not the device clock.</summary>
        public DateTime NowUtc()
        {
            return deviceNowUtc() + new TimeSpan(offsetTicks);
        }
    }
}
