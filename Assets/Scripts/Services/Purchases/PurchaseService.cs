using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Purchases;
using BlindDelivery.Core.Save;
using BlindDelivery.Core.Services;
using UnityEngine;

namespace BlindDelivery.Services.Purchases
{
    /// <summary>Save document for owned non-consumables (consumable and subscription ownership is session-only by nature).</summary>
    public class PurchasesDocument
    {
        /// <summary>
        /// CSV of owned non-consumable product ids. JsonUtility has no List&lt;string&gt;, so the ids
        /// ride one string; ',' must never appear inside a product id (they are dot-reversed
        /// domains in practice) â€” the catalog is authored with that constraint.
        /// </summary>
        public string ownedNonConsumables = "";
    }

    /// <summary>
    /// The purchase system over any <see cref="IStoreSink"/> (goals note system 10). The stub store
    /// always succeeds; Unity IAP slots in as the sink at the composition root, nothing above it
    /// changes.
    ///
    /// Ownership rules: non-consumables are persisted through <see cref="ISaveService"/> inside
    /// the completion path â€” the save is awaited BEFORE <see cref="OnPurchaseCompleted"/> fires, so
    /// a crash after the store callback cannot lose a paid unlock (the M7 acceptance: a purchase
    /// survives a simulated crash via a fresh service over the same store). Subscriptions are
    /// owned for the session only; consumables never enter the owned list, so buying again is
    /// always possible. Restoring re-persists non-consumables but deliberately does NOT fire the
    /// completed event â€” restore must not re-run grant effects; callers read IsOwned afterwards.
    /// </summary>
    public sealed class PurchaseService : IPurchaseService, IAppService
    {
        const string SaveKeyPath = "purchases";

        readonly IStoreSink store;
        readonly IPurchaseCatalog catalog;
        readonly ISaveService save;
        readonly List<string> owned = new List<string>();

        public PurchaseService(IStoreSink store, IPurchaseCatalog catalog, ISaveService save)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public string Name => "Purchases";

        /// <summary>Fires exactly once per completed purchase, after the ownership write is durable.</summary>
        public event Action<string> OnPurchaseCompleted;

        public async Task InitializeAsync(CancellationToken token = default)
        {
            store.Initialize(catalog.Products);

            // A fresh document (nothing saved yet) loads as an empty string â€” no ids owned.
            try
            {
                PurchasesDocument document = await save.LoadAsync<PurchasesDocument>(SaveKeyPath, token);
                foreach (string productId in SplitOwned(document.ownedNonConsumables))
                {
                    if (!owned.Contains(productId))
                    {
                        owned.Add(productId);
                    }
                }
            }
            catch (Exception error)
            {
                // A corrupted purchases document degrades to "everything re-buyable" â€” it must
                // never block boot, and the player's next successful purchase re-persists.
                Debug.LogWarning($"[PurchaseService] Ownership document did not load ({error.Message}); starting unowned.");
            }
        }

        public bool IsOwned(string productId)
        {
            return owned.Contains(productId);
        }

        public IReadOnlyCollection<string> GetOwnedProducts()
        {
            return owned.ToArray();
        }

        public async Task<PurchaseResult> PurchaseAsync(string productId, CancellationToken token = default)
        {
            if (!catalog.TryGet(productId, out ProductData product))
            {
                return PurchaseResult.FromReason(productId, PurchaseFailureReason.UnknownProduct);
            }

            if (product.Type != ProductType.Consumable && owned.Contains(productId))
            {
                // Protects against double grants above the store: a second buy of an owned
                // non-consumable or subscription is refused before the sink is even asked.
                return PurchaseResult.FromReason(productId, PurchaseFailureReason.AlreadyOwned);
            }

            TaskCompletionSource<PurchaseResult> completion =
                new TaskCompletionSource<PurchaseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            store.Purchase(productId, (id, success) =>
                completion.TrySetResult(success
                    ? PurchaseResult.Completed(productId)
                    : PurchaseResult.FromReason(productId, PurchaseFailureReason.RejectedOrCancelled)));

            PurchaseResult result = await completion.Task;
            if (!result.Success)
            {
                return result;
            }

            // Consumables never enter the owned list â€” they stay re-buyable by design. A
            // subscription DOES enter (it is what blocks a double grant) but is session-only:
            // BuildDocument persists non-consumables only.
            if (product.Type != ProductType.Consumable && !owned.Contains(productId))
            {
                owned.Add(productId);
            }

            if (product.Type == ProductType.NonConsumable)
            {
                await save.SaveAsync(SaveKeyPath, BuildDocument(), token);
            }

            OnPurchaseCompleted?.Invoke(productId);
            return result;
        }

        public async Task<IReadOnlyCollection<string>> RestorePurchasesAsync(CancellationToken token = default)
        {
            IReadOnlyCollection<string> restored = await store.RestoreOwnedAsync(token);

            bool changed = false;
            foreach (string productId in restored)
            {
                if (!catalog.TryGet(productId, out ProductData product) || product.Type != ProductType.NonConsumable)
                {
                    // Unknown to the catalog or not persistent by kind: the store keeps it, the
                    // BlindDelivery makes no game grant from it.
                    continue;
                }

                if (!owned.Contains(productId))
                {
                    owned.Add(productId);
                    changed = true;
                }
            }

            if (changed)
            {
                await save.SaveAsync(SaveKeyPath, BuildDocument(), token);
            }

            // Deliberately no OnPurchaseCompleted here â€” see the class comment.
            return restored;
        }

        PurchasesDocument BuildDocument()
        {
            StringBuilder ownedCsv = new StringBuilder();
            foreach (string productId in owned)
            {
                // Only non-consumables ride the document; subscriptions are session-only.
                if (catalog.TryGet(productId, out ProductData product) && product.Type == ProductType.NonConsumable)
                {
                    if (ownedCsv.Length > 0)
                    {
                        ownedCsv.Append(',');
                    }

                    ownedCsv.Append(productId);
                }
            }

            return new PurchasesDocument { ownedNonConsumables = ownedCsv.ToString() };
        }

        static IEnumerable<string> SplitOwned(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                yield break;
            }

            foreach (string part in csv.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    yield return part.Trim();
                }
            }
        }
    }
}

