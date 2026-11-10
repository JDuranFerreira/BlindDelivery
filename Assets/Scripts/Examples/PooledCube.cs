using Template.Core.Pooling;
using UnityEngine;

namespace Template.Examples
{
    /// <summary>
    /// A pooled demo object (goals note system 13). IPoolable.ResetState puts the reuse work —
    /// position, scale, spin — into OnSpawn instead of reconstructing anything, so spawning from
    /// the pool costs no allocation and no Instantiate: that is the whole point of the pool on
    /// mobile. Bullets, effects and floating text get the same treatment.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class PooledCube : MonoBehaviour, IPoolable
    {
        float spinSpeed;

        public void OnSpawn()
        {
            transform.position = new Vector3(
                Random.Range(-3f, 3f),
                Random.Range(-1.5f, 1.5f),
                Random.Range(3f, 8f));
            transform.localScale = Vector3.one * Random.Range(0.35f, 0.8f);
            spinSpeed = Random.Range(40f, 160f);
        }

        public void OnDespawn()
        {
            // Nothing to clean up: state resets fully in OnSpawn, which is the ideal pool citizen.
        }

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }
}