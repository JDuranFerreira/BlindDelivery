using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.GameServices;

namespace BlindDelivery.Services.GameServices
{
    /// <summary>
    /// The BlindDelivery's default platform provider: sign-in succeeds (or is scripted false) and every
    /// queued item is "sent" into the void. A real SDK (Play Games, Game Center) replaces it by
    /// implementing <see cref="IGameServicesProvider"/> and being passed into
    /// <see cref="GameServicesService"/> at the composition root. Its knobs also make it the fake
    /// in tests: deny the sign-in, fail the sends, record what arrived.
    /// </summary>
    public sealed class StubGameServicesProvider : IGameServicesProvider
    {
        Func<bool> signIn;
        bool sendResult;

        public int SignInAttempts { get; private set; }

        public List<QueuedGameServiceItem> Sent { get; } = new List<QueuedGameServiceItem>();

        public List<string> LeaderboardsShown { get; } = new List<string>();

        public QueuedGameServiceItem LastSentItem => Sent.Count > 0 ? Sent[Sent.Count - 1] : default;

        public StubGameServicesProvider()
        {
            // BlindDelivery default: sign-in succeeds silently, sends always succeed.
            signIn = () => true;
            sendResult = true;
        }

        /// <summary>Scripted sign-in outcome (denial is a normal platform quality, not an error).</summary>
        public StubGameServicesProvider WithSignIn(Func<bool> signInFactory)
        {
            signIn = signInFactory;
            return this;
        }

        /// <summary>Scripted send outcome â€” false keeps the item queued.</summary>
        public StubGameServicesProvider WithSendResult(bool result)
        {
            sendResult = result;
            return this;
        }

        public Task<bool> SignInAsync(CancellationToken token = default)
        {
            SignInAttempts++;
            return Task.FromResult(signIn());
        }

        public void ShowLeaderboard(string platformBoardId)
        {
            LeaderboardsShown.Add(platformBoardId);
        }

        public bool SendItem(QueuedGameServiceItem item)
        {
            if (sendResult)
            {
                Sent.Add(item);
            }

            return sendResult;
        }
    }
}

