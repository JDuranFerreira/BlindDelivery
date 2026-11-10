using System.Collections.Generic;
using Template.Core.Audio;
using Template.Core.Purchases;

namespace Template.Tests.EditMode.Fakes
{
    /// <summary>Records every duck instruction the ad service issues around full-screen shows.</summary>
    public sealed class FakeAudioService : IAudioService
    {
        public readonly List<string> DuckCalls = new List<string>();

        public void PlayMusic(string musicKey, float fadeSeconds = 0.5f)
        {
        }

        public void StopMusic(float fadeSeconds = 0.5f)
        {
        }

        public void PlaySfx(string sfxKey, float volumeScale = 1f)
        {
        }

        public void Duck(float factor, float holdSeconds = 0f)
        {
            DuckCalls.Add($"{factor}:{holdSeconds}");
        }
    }

    /// <summary>In-memory catalog with the ids a test authors for the service.</summary>
    public sealed class FakePurchaseCatalog : IPurchaseCatalog
    {
        readonly List<ProductData> products = new List<ProductData>();

        public FakePurchaseCatalog(params ProductData[] productIds)
        {
            products.AddRange(productIds);
        }

        public IReadOnlyList<ProductData> Products => products;

        public bool TryGet(string productId, out ProductData product)
        {
            foreach (ProductData candidate in products)
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
