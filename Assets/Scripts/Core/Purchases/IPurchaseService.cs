using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlindDelivery.Core.Purchases
{
    /// <summary>The three product kinds the stores sell (goals note system 10).</summary>
    public enum ProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    /// <summary>One purchasable product, as the catalog describes it. Ids are the store product ids.</summary>
    public readonly struct ProductData
    {
        public ProductData(string id, ProductType type)
        {
            Id = id;
            Type = type;
        }

        public string Id { get; }

        public ProductType Type { get; }
    }

    /// <summary>Why a purchase attempt did not complete for the player.</summary>
    public enum PurchaseFailureReason
    {
        UnknownProduct,
        RejectedOrCancelled,
        StoreUnavailable,
        /// <summary>The same non-consumable is already owned (protects against double grants).</summary>
        AlreadyOwned,
        Failed
    }

    /// <summary>The terminal state of one purchase attempt. Exactly one of the properties is true.</summary>
    public readonly struct PurchaseResult
    {
        public PurchaseResult(string productId, string failureReason)
        {
            ProductId = productId;
            Success = failureReason == null;
            FailureReason = failureReason;
        }

        public string ProductId { get; }

        public bool Success { get; }

        /// <summary>Null when <see cref="Success"/>; a <see cref="PurchaseFailureReason"/> spelling otherwise.</summary>
        public string FailureReason { get; }

        public static PurchaseResult Completed(string productId) => new PurchaseResult(productId, null);

        public static PurchaseResult FromReason(string productId, PurchaseFailureReason reason) =>
            new PurchaseResult(productId, reason.ToString());
    }

    /// <summary>
    /// The purchase seam (goals note system 10). The BlindDelivery's store is a stub out of the box;
    /// Unity IAP slots in as an <see cref="IStoreSink"/> without touching gameplay code.
    /// Non-consumable ownership and remove-ads are persisted through <see cref="ISaveService"/>
    /// <em>inside</em> the completion path (save immediately after a purchase), so a crash after the
    /// store callback cannot lose a paid unlock.
    /// </summary>
    public interface IPurchaseService : BlindDelivery.Core.Services.IAppService
    {
        /// <summary>True when the (restored or purchased) non-consumable/product is owned this session and post-crash.</summary>
        bool IsOwned(string productId);

        /// <summary>Raised exactly once per completed purchase, after the ownership write is durable.</summary>
        event Action<string> OnPurchaseCompleted;

        /// <summary>Buys a product from the catalog. Never throws; the result carries the reason.</summary>
        Task<PurchaseResult> PurchaseAsync(string productId, CancellationToken token = default);

        /// <summary>
        /// Store-level restore (Apple requires it). Returns the store's owned ids; non-consumables
        /// among them are persisted immediately like a normal purchase.
        /// </summary>
        Task<IReadOnlyCollection<string>> RestorePurchasesAsync(CancellationToken token = default);

        /// <summary>All owned non-consumable product ids after the latest load/restore/purchase.</summary>
        IReadOnlyCollection<string> GetOwnedProducts();
    }

    /// <summary>
    /// The store seam a concrete SDK implements (Unity IAP, fake store in tests). Each Purchase
    /// call completes through exactly one callback describing the store-side outcome; ownership
    /// bookkeeping stays the service's job, so no double grant is possible above the store.
    /// </summary>
    public interface IStoreSink
    {
        void Initialize(IReadOnlyCollection<ProductData> products);

        void Purchase(string productId, Action<string, bool> callback);

        /// <summary>Asks the platform for previously owned purchases; completes with the owned ids.</summary>
        Task<IReadOnlyCollection<string>> RestoreOwnedAsync(CancellationToken token = default);
    }

    /// <summary>
    /// Catalog data source (goals note: catalog in a ScriptableObject). Core keeps the contract
    /// pure; the ScriptableObject implementation lives next to the service that consumes it.
    /// </summary>
    public interface IPurchaseCatalog
    {
        IReadOnlyList<ProductData> Products { get; }

        bool TryGet(string productId, out ProductData product);
    }
}


