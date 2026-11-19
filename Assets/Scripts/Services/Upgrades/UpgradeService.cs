using System;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Services;
using BlindDelivery.Core.Upgrades;

namespace BlindDelivery.Services.Upgrades
{
    /// <summary>
    /// In-memory upgrade ownership for the blind-systems stage. Save-backed progression replaces
    /// the storage later without changing the <see cref="IUpgradeService"/> seam or its callers.
    /// </summary>
    public sealed class UpgradeService : IAppService, IUpgradeService
    {
        const string ServiceName = "Upgrades";
        const int MaxLevel = 3;

        readonly int[] levels = new int[Enum.GetValues(typeof(UpgradeKind)).Length];

        public string Name => ServiceName;

        public Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;

        public int Level(UpgradeKind kind) => levels[(int)kind];

        public bool IsUnlocked(UpgradeKind kind, int level)
            => level > 0 && levels[(int)kind] >= level;

        public void SetLevel(UpgradeKind kind, int level)
        {
            if (level < 0 || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            levels[(int)kind] = level;
        }
    }
}
