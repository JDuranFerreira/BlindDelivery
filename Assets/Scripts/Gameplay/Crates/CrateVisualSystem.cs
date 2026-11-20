using System.Collections.Generic;
using BlindDelivery.Gameplay.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.Crates
{
    /// <summary>
    /// Turns a crate loss into a visible event (GDD §3): the courier drops an intact crate, which
    /// falls until it touches the floor and then shows the cracked crate sprite. Crate counts and
    /// loss rules stay in Core; this is only their presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrateVisualSystem : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [SerializeField] RectLevelGeometry geometry;
        [SerializeField] GameObject intactPrefab;
        [SerializeField] GameObject crackedPrefab;
        [SerializeField, Min(0f)] float spawnHeightAboveCourier = 0.6f;
        [SerializeField, Min(0f)] float gravity = 9.81f;
        [SerializeField, Min(0f)] float probeSkin = 0.06f;

        readonly List<DroppedCrate> falling = new List<DroppedCrate>();
        CrateFactory factory;
        Sprite crackedSprite;

        public IReadOnlyList<DroppedCrate> Falling => falling;

        void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationRunner>();
            if (geometry == null) geometry = FindAnyObjectByType<RectLevelGeometry>();
            if (crackedPrefab != null)
            {
                SpriteRenderer crackedRenderer = crackedPrefab.GetComponent<SpriteRenderer>();
                crackedSprite = crackedRenderer != null ? crackedRenderer.sprite : null;
            }

            factory = new CrateFactory(intactPrefab);
            if (runner != null) runner.CrateLost += OnCrateLost;
        }

        void OnDisable()
        {
            if (runner != null) runner.CrateLost -= OnCrateLost;
        }

        void OnCrateLost(Vector2 at)
        {
            if (factory == null) return;

            GameObject instance = factory.Create(
                new Vector3(at.x, at.y + spawnHeightAboveCourier, transform.position.z),
                transform);
            if (instance == null) return;

            DroppedCrate drop = instance.AddComponent<DroppedCrate>();
            drop.Initialize(crackedSprite, geometry, gravity, probeSkin);
            falling.Add(drop);
        }

        void Update()
        {
            // Presentation only: crates keep falling after the run ends, so this is not driven by
            // SimulationRunner.Stepped, which stops at the result screen.
            float deltaTime = Mathf.Min(Time.deltaTime, 0.1f);
            for (int i = falling.Count - 1; i >= 0; i--)
            {
                if (falling[i] == null)
                {
                    falling.RemoveAt(i);
                    continue;
                }

                if (falling[i].Step(deltaTime)) falling.RemoveAt(i);
            }
        }
    }
}
