using Unity.U2D.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Physics
{
    /// <summary>
    /// Creates a 2D Physics Core body and box shape in the default world. This project uses the
    /// new 2D Physics Core (Unity.U2D.Physics), not legacy Rigidbody2D/Collider2D/Physics2D.
    /// The com.unity.2d.physics components are unavailable on Unity 6000.6.3f1, so bodies are
    /// created through the engine API instead.
    /// </summary>
    public sealed class CoreBody2D : MonoBehaviour
    {
        [SerializeField] PhysicsBody.BodyType bodyType = PhysicsBody.BodyType.Static;
        [SerializeField] Vector2 size = Vector2.one;
        [SerializeField] bool isTrigger;
        [SerializeField] bool showGizmo = true;
        [Header("Optional explicit box (used by thin pads)")]
        [SerializeField] bool useCustomShape;
        [SerializeField] Vector2 customCenter;
        [SerializeField] Vector2 customSize = Vector2.one;

        PhysicsBody body;
        int ownerKey;
        bool created;
        Vector2 localCenter;

        public PhysicsBody Body => body;

        /// <summary>Shape centre in the Transform's local space; used to sync pose when the body falls.</summary>
        public Vector2 LocalCenter => localCenter;

        void OnEnable()
        {
            if (!TryGetShape(out Vector2 center, out Vector2 boxSize))
            {
                return;
            }

            var definition = new PhysicsBodyDefinition
            {
                type = bodyType,
                position = center,
                rotation = PhysicsRotate.FromDegrees(transform.eulerAngles.z),
            };

            body = PhysicsWorld.defaultWorld.CreateBody(definition);
            ownerKey = body.SetOwner(this);

            var shapeDefinition = new PhysicsShapeDefinition { isTrigger = isTrigger };
            if (bodyType == PhysicsBody.BodyType.Dynamic)
            {
                // The walking controller sets velocity every fixed step; ground friction would
                // otherwise cancel it. Frictionless + locked rotation gives constant sliding.
                PhysicsShape.SurfaceMaterial surface = shapeDefinition.surfaceMaterial;
                surface.friction = 0f;
                surface.frictionMixing = PhysicsShape.SurfaceMaterial.MixingMode.Minimum;
                surface.frictionPriority = ushort.MaxValue;
                shapeDefinition.surfaceMaterial = surface;
            }
            body.CreateShape(PolygonGeometry.CreateBox(boxSize), shapeDefinition);
            created = true;
        }

        /// <summary>
        /// Sprite pivots in this project are often bottom-centre, so the Transform is not the
        /// sprite's centre. Derive the box from the SpriteRenderer bounds and transform it to
        /// world space. Falls back to the serialized size when no sprite is available.
        /// </summary>
        bool TryGetShape(out Vector2 center, out Vector2 boxSize)
        {
            Vector2 lossy = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            if (useCustomShape)
            {
                localCenter = customCenter;
                boxSize = Vector2.Scale(customSize, lossy);
                center = transform.TransformPoint(customCenter);
                return boxSize.x > 0f && boxSize.y > 0f;
            }

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                Bounds bounds = renderer.sprite.bounds;
                localCenter = bounds.center;
                Vector2 localSize = bounds.size;
                boxSize = Vector2.Scale(localSize, lossy);
                center = transform.TransformPoint(bounds.center);
                return boxSize.x > 0f && boxSize.y > 0f;
            }

            localCenter = Vector2.zero;
            boxSize = Vector2.Scale(size, lossy);
            center = transform.position;
            return boxSize.x > 0f && boxSize.y > 0f;
        }

        /// <summary>Lets a placed item fall under world gravity until it lands.</summary>
        public void SetDynamic()
        {
            if (!created || !body.isValid) return;
            body.type = PhysicsBody.BodyType.Dynamic;
            body.gravityScale = 1f;
            body.linearDamping = 0.4f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.awake = true;
        }

        /// <summary>Freezes a landed item as an immovable solid again.</summary>
        public void SetStatic()
        {
            if (!created || !body.isValid) return;
            body.type = PhysicsBody.BodyType.Static;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.rotation = PhysicsRotate.identity;
        }

        /// <summary>Items are kept upright; the walking controller does the same for the deliveryman.</summary>
        public void StabilizeRotation()
        {
            if (!created || !body.isValid) return;
            body.rotation = PhysicsRotate.identity;
            body.angularVelocity = 0f;
        }

        /// <summary>Copies the simulated body pose back to the Transform (bottom-centre sprite pivots).</summary>
        public void SyncTransform()
        {
            if (!created || !body.isValid) return;
            Vector2 position = body.position - localCenter;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        void OnDisable()
        {
            if (created && body.isValid)
            {
                body.Destroy(ownerKey);
            }

            created = false;
        }

        void OnDrawGizmosSelected()
        {
            if (!showGizmo)
            {
                return;
            }

            if (!TryGetShape(out Vector2 center, out Vector2 boxSize))
            {
                return;
            }

            Gizmos.color = isTrigger ? new Color(0.2f, 0.8f, 1f) : new Color(0.2f, 1f, 0.4f);
            Gizmos.DrawWireCube(center, new Vector3(boxSize.x, boxSize.y, 0f));
        }
    }
}
