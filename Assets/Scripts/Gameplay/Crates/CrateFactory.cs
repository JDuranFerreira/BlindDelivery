using UnityEngine;

namespace BlindDelivery.Gameplay.Crates
{
    /// <summary>
    /// Single creation point for dropped-crate visuals. The drop controller never instantiates a
    /// prefab directly; landing swaps the sprite, so only one prefab needs to be created.
    /// </summary>
    public sealed class CrateFactory
    {
        readonly GameObject prefab;

        public CrateFactory(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public GameObject Create(Vector3 position, Transform parent)
            => prefab == null ? null : Object.Instantiate(prefab, position, Quaternion.identity, parent);
    }
}
