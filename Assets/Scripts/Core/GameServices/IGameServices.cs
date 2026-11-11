using System.Threading;
using System.Threading.Tasks;

namespace BlindDelivery.Core.GameServices
{
    /// <summary>
    /// The platform social seam (goals note system 11: Google Play Games / Game Center sign-in,
    /// leaderboards, achievements). Sign-in is optional and silent â€” the game is fully playable
    /// without it. Scores and achievements reported while offline are queued and flushed when a
    /// signed-in state arrives again.
    /// </summary>
    public interface IGameServices : BlindDelivery.Core.Services.IAppService
    {
        /// <summary>True when the platform account is connected. False must never block gameplay.</summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// Silent sign-in attempt. False (or a denial) is a normal quality of the platform, not an
        /// error â€” the BlindDelivery records the outcome and keeps the queue for the next attempt.
        /// </summary>
        Task<bool> SignInAsync(CancellationToken token = default);

        /// <summary>Reports a score to a board under a stable game-side key (mapped per platform in the config). Queued while offline.</summary>
        void ReportScore(string boardKey, long score);

        /// <summary>Unlocks an achievement by stable game-side key (mapped per platform). Queued while offline.</summary>
        void UnlockAchievement(string achievementKey);

        /// <summary>Opens the platform leaderboard UI for a board key. No-op when not signed in.</summary>
        void ShowLeaderboard(string boardKey = "");

        /// <summary>How many items are queued waiting for sign-in (debug overlay / tests).</summary>
        int PendingQueueCount { get; }

        /// <summary>Forces a queue flush attempt against the provider (uses: signed in mid-session).</summary>
        Task<int> FlushQueueAsync(CancellationToken token = default);
    }

    /// <summary>The provider seam a platform implementation fills (Play Games, Game Center, fake).</summary>
    public interface IGameServicesProvider
    {
        /// <summary>Attempt to sign in silently. False = denied/absent, not fatal.</summary>
        Task<bool> SignInAsync(CancellationToken token = default);

        void ShowLeaderboard(string platformBoardId);

        /// <summary>Sends one queued item. False means "not sent, keep queued" (absent, failure).</summary>
        bool SendItem(QueuedGameServiceItem item);
    }

    /// <summary>One offline-queued report. Kind discriminates score vs achievement.</summary>
    public readonly struct QueuedGameServiceItem
    {
        public bool IsValid => !string.IsNullOrEmpty(Key);

        public QueuedGameServiceItem(int kind, string key, long value, string utcTime)
        {
            Kind = kind;
            Key = key;
            Value = value;
            QueuedUtc = utcTime;
        }

        public int Kind { get; }
        public string Key { get; }
        public long Value { get; }
        public string QueuedUtc { get; }
    }

    /// <summary>Stable game-side keys for boards and achievements (goals note: never platform ids in code).</summary>
    public static class GameServiceKeys
    {
        public const int KindScore = 0;
        public const int KindAchievement = 1;
    }
}


