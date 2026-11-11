using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Purchases;

namespace BlindDelivery.Services.Purchases
{
    /// <summary>
    /// The BlindDelivery's default store: buys always succeed and nothing was ever owned. A real IAP
    /// SDK (Unity IAP) replaces it by implementing <see cref="IStoreSink"/> and being passed into
    /// <see cref="PurchaseService"/> at the composition root â€” no other change. Its knobs also make
    /// it the honest fake in tests: scripted outcome per purchase and a scripted restore list.
    /// </summary>
    public sealed class StubStoreSink : IStoreSink
    {
        Func<string, bool> nextOutcome;
        readonly List<ProductData> registered = new List<ProductData>();
        readonly List<string> restoreOwned = new List<string>();

        public int InitializeCount { get; private set; }

        public IReadOnlyList<ProductData> RegisteredProducts => registered;

        public string LastProductId { get; private set; }

        public bool IsInitialized { get; private set; }

        public StubStoreSink()
        {
            // BlindDelivery default: buys succeed so the purchase flow is playable out of the box.
            nextOutcome = _ => true;
        }

        /// <summary>Scripted outcome per purchase attempt (success by default).</summary>
        public StubStoreSink WithOutcome(Func<string, bool> outcomeFactory)
        {
            nextOutcome = outcomeFactory;
            return this;
        }

        /// <summary>Scripted owned ids for the store-level restore.</summary>
        public StubStoreSink WithRestoreOwned(IReadOnlyCollection<string> ownedIds)
        {
            restoreOwned.Clear();
            restoreOwned.AddRange(ownedIds);
            return this;
        }

        public void Initialize(IReadOnlyCollection<ProductData> products)
        {
            InitializeCount++;
            IsInitialized = true;
            registered.Clear();
            registered.AddRange(products);
        }

        public void Purchase(string productId, Action<string, bool> callback)
        {
            LastProductId = productId;
            callback(productId, nextOutcome(productId));
        }

        public Task<IReadOnlyCollection<string>> RestoreOwnedAsync(CancellationToken token = default)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(restoreOwned);
        }
    }
}

