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

        PhysicsBody body;
        int ownerKey;
        bool created;

        public PhysicsBody Body => body;

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
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                Bounds bounds = renderer.sprite.bounds;
                Vector2 localSize = bounds.size;
                Vector2 lossy = transform.lossyScale;
                boxSize = Vector2.Scale(localSize, new Vector2(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y)));
                center = transform.TransformPoint(bounds.center);
                return boxSize.x > 0f && boxSize.y > 0f;
            }

            boxSize = size;
            center = transform.position;
            return size.x > 0f && size.y > 0f;
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
