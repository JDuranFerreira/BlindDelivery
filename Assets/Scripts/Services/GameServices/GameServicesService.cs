using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.GameServices;
using BlindDelivery.Core.Services;
using UnityEngine;

namespace BlindDelivery.Services.GameServices
{
    /// <summary>
    /// The platform social system over any <see cref="IGameServicesProvider"/> (goals note system
    /// 11). Sign-in is optional and silent: a denial â€” like a null provider â€” never blocks
    /// gameplay, scores and achievements just queue. Reports carry stable game-side keys; the
    /// <see cref="PlatformIdMap"/> translates them to platform ids at SEND time, so a key missing
    /// from the config is dropped with a log rather than guessed. Offline reports flush when a
    /// signed-in state arrives (sign-in itself, or <see cref="FlushQueueAsync"/>).
    /// </summary>
    public sealed class GameServicesService : IGameServices, IAppService
    {
        readonly IGameServicesProvider provider;
        readonly PlatformIdMap platformIds;
        readonly List<QueuedGameServiceItem> queue = new List<QueuedGameServiceItem>();

        public GameServicesService(IGameServicesProvider provider, PlatformIdMap platformIds)
        {
            // A null provider is the BlindDelivery default â€” the platform is optional, not required.
            this.provider = provider;
            this.platformIds = platformIds ?? new PlatformIdMap();
        }

        public string Name => "GameServices";

        public bool IsSignedIn { get; private set; }

        public int PendingQueueCount => queue.Count;

        public Task InitializeAsync(CancellationToken token = default)
        {
            // Nothing to do at boot: sign-in is the game's explicit call (menu button or first
            // gameplay), never a startup assumption.
            return Task.CompletedTask;
        }

        public async Task<bool> SignInAsync(CancellationToken token = default)
        {
            IsSignedIn = provider != null && await provider.SignInAsync(token);

            // A signed-in state arriving is what the queued reports were waiting for.
            await FlushQueueAsync(token);
            return IsSignedIn;
        }

        public void ReportScore(string boardKey, long score)
        {
            ReportOrQueue(GameServiceKeys.KindScore, boardKey, score);
        }

        public void UnlockAchievement(string achievementKey)
        {
            ReportOrQueue(GameServiceKeys.KindAchievement, achievementKey, 0);
        }

        public void ShowLeaderboard(string boardKey = "")
        {
            if (!IsSignedIn || provider == null)
            {
                // No-op offline per contract â€” the platform UI cannot open without the account.
                return;
            }

            if (string.IsNullOrEmpty(boardKey))
            {
                // The all-boards/default UI passes through with no id.
                provider.ShowLeaderboard("");
                return;
            }

            if (platformIds.TryGetPlatformId(boardKey, out string platformId))
            {
                provider.ShowLeaderboard(platformId);
            }
            else
            {
                Debug.Log($"[GameServices] Board '{boardKey}' has no platform id on '{platformIds.CurrentPlatform ?? "unconfigured"}'; skipped.");
            }
        }

        public Task<int> FlushQueueAsync(CancellationToken token = default)
        {
            if (!IsSignedIn || provider == null)
            {
                return Task.FromResult(0);
            }

            int sent = 0;
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                QueuedGameServiceItem item = queue[i];

                if (!platformIds.TryGetPlatformId(item.Key, out string platformId))
                {
                    // Not configured: dropped with a log, never guessed (goals note system 11).
                    queue.RemoveAt(i);
                    Debug.Log($"[GameServices] '{item.Key}' has no platform id on '{platformIds.CurrentPlatform ?? "unconfigured"}'; dropped from queue.");
                    continue;
                }

                QueuedGameServiceItem mapped = new QueuedGameServiceItem(item.Kind, platformId, item.Value, item.QueuedUtc);
                if (provider.SendItem(mapped))
                {
                    queue.RemoveAt(i);
                    sent++;
                }
                // else: not sent, keep queued â€” the next flush or sign-in retries it.
            }

            return Task.FromResult(sent);
        }

        void ReportOrQueue(int kind, string stableKey, long value)
        {
            if (string.IsNullOrEmpty(stableKey))
            {
                Debug.LogWarning($"[GameServices] {nameof(ReportScore)} or {nameof(UnlockAchievement)} called with an empty key; ignored.");
                return;
            }

            if (TrySendNow(kind, stableKey, value))
            {
                return;
            }

            // Offline (or the send failed): the stable key queues, mapping happens at flush time.
            queue.Add(new QueuedGameServiceItem(kind, stableKey, value, DateTime.UtcNow.ToString("o")));
        }

        /// <summary>True when the item is fully handled now (sent, or unmapped = skipped by config). False means queue it.</summary>
        bool TrySendNow(int kind, string stableKey, long value)
        {
            if (!IsSignedIn || provider == null)
            {
                return false;
            }

            if (!platformIds.TryGetPlatformId(stableKey, out string platformId))
            {
                Debug.Log($"[GameServices] '{stableKey}' has no platform id on '{platformIds.CurrentPlatform ?? "unconfigured"}'; skipped.");
                return true;
            }

            QueuedGameServiceItem mapped = new QueuedGameServiceItem(kind, platformId, value, DateTime.UtcNow.ToString("o"));
            return provider.SendItem(mapped);
        }
    }
}

