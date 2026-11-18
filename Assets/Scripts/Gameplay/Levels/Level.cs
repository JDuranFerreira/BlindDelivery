using UnityEngine;

namespace BlindDelivery.Gameplay.Levels
{
    /// <summary>
    /// Root component of a level prefab. Every level has a Start point (A) and an End point (B);
    /// the deliveryman is spawned on A when the level starts.
    /// </summary>
    public sealed class Level : MonoBehaviour
    {
        [SerializeField] Transform startPoint;
        [SerializeField] Transform endPoint;
        [SerializeField] GameObject deliverymanPrefab;

        GameObject deliveryman;

        public Transform StartPoint => startPoint;

        public Transform EndPoint => endPoint;

        public GameObject Deliveryman => deliveryman;

        void Awake()
        {
            SpawnDeliveryman();
        }

        /// <summary>Spawns (or respawns) the deliveryman at point A. Public so tests and retry can call it.</summary>
        public GameObject SpawnDeliveryman()
        {
            if (deliveryman != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(deliveryman);
                }
                else
                {
                    DestroyImmediate(deliveryman);
                }
            }

            if (startPoint == null || endPoint == null)
            {
                Debug.LogError("[Level] A level needs both a StartPoint and an EndPoint marker.", this);
                return null;
            }

            if (deliverymanPrefab == null)
            {
                Debug.LogError("[Level] No Deliveryman prefab assigned, so the character cannot spawn.", this);
                return null;
            }

            deliveryman = Instantiate(deliverymanPrefab, startPoint.position, startPoint.rotation, transform);
            deliveryman.name = "Deliveryman";
            return deliveryman;
        }

        void OnValidate()
        {
            // The markers are plain GameObjects named StartPoint / EndPoint, kept as positional
            // references only. Re-wire them by name if a designer duplicates the prefab.
            if (startPoint == null)
            {
                Transform found = transform.Find("StartPoint");
                if (found != null) startPoint = found;
            }

            if (endPoint == null)
            {
                Transform found = transform.Find("EndPoint");
                if (found != null) endPoint = found;
            }
        }
    }
}
