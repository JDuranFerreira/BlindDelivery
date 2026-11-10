using System;
using System.Collections.Generic;
using Template.Core.Purchases;
using UnityEngine;

namespace Template.Services.Purchases
{
    /// <summary>
    /// Per-product entry. Kept serializable by the inspector (fields only, JsonUtility-free).
    /// </summary>
    [Serializable]
    public class PurchaseCatalogEntry
    {
        public string productId;
        public ProductType productType;
    }

    /// <summary>
    /// The catalog as a ScriptableObject (goals note system 10: catalog in a ScriptableObject, ids
    /// are the store product ids). Core keeps the <see cref="IPurchaseCatalog"/> contract pure; this
    /// scriptable version lives next to the service that consumes it. Empty by default — the
    /// template boots with no ids until a game authors them.
    /// </summary>
    [CreateAssetMenu(menuName = "Template/Purchase Catalog", fileName = "PurchaseCatalog")]
    public sealed class ScriptablePurchaseCatalog : ScriptableObject, IPurchaseCatalog
    {
        [SerializeField, Tooltip("Store product ids the catalog owns. They must not contain the ',' used to persist ownership.")]
        List<PurchaseCatalogEntry> products = new List<PurchaseCatalogEntry>();

        List<ProductData> cache;

        public IReadOnlyList<ProductData> Products
        {
            get
            {
                if (cache == null)
                {
                    cache = new List<ProductData>(products.Count);
                    foreach (PurchaseCatalogEntry entry in products)
                    {
                        cache.Add(new ProductData(entry.productId, entry.productType));
                    }
                }

                return cache;
            }
        }

        public bool TryGet(string productId, out ProductData product)
        {
            foreach (ProductData candidate in Products)
            {
                if (candidate.Id == productId)
                {
                    product = candidate;
                    return true;
                }
            }

            product = default;
            return false;
        }
    }
}