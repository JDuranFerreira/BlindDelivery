using BlindDelivery.Core.Balance;
using BlindDelivery.Gameplay.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Items
{
    /// <summary>
    /// Runtime instance of a placed guidance object. While it is a ghost or being dragged the
    /// physics body is disabled; committing enables it at the final position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GuidanceItem : MonoBehaviour
    {
        static readonly Color ValidGhost = new Color(0.3f, 1f, 0.4f, 0.6f);
        static readonly Color InvalidGhost = new Color(1f, 0.3f, 0.3f, 0.6f);

        [SerializeField] ItemType type;
        [Tooltip("Enable for item instances authored directly in a level. They are registered as placed items with live physics bodies at startup.")]
        [SerializeField] bool startsInLevel;

        SpriteRenderer spriteRenderer;
        CoreBody2D body;
        Color originalColor = Color.white;
        int instanceId = -1;
        bool settled;
        float dropTime;
        int restSteps;

        public ItemType Type => type;

        /// <summary>True for item prefabs placed in the scene by a designer rather than spawned by the player.</summary>
        public bool StartsInLevel => startsInLevel;

        public int InstanceId => instanceId;

        /// <summary>False while the item is a ghost or being dragged (no physics body in the world).</summary>
        public bool IsPlaced { get; private set; }

        /// <summary>True once the item rests on the floor; only settled items count as level solids.</summary>
        public bool IsSettled => settled;

        public CoreBody2D Body => body != null ? body : (body = GetComponent<CoreBody2D>());

        public Bounds WorldBounds => spriteRenderer != null
            ? spriteRenderer.bounds
            : new Bounds(transform.position, Vector3.one);

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            body = GetComponent<CoreBody2D>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;
        }

        public void AssignId(int id) => instanceId = id;

        public void SetGhost(bool valid)
        {
            IsPlaced = false;
            if (body != null) body.enabled = false;
            if (spriteRenderer != null) spriteRenderer.color = valid ? ValidGhost : InvalidGhost;
        }

        /// <summary>Places the item in the world at its current transform position.</summary>
        public void Commit()
        {
            IsPlaced = true;
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            if (body != null) body.enabled = true;
        }

        /// <summary>Author it as a resting item (designer-placed instances start on the floor).</summary>
        public void MarkSettled()
        {
            settled = true;
            dropTime = 0f;
            restSteps = 0;
            Body?.SetStatic();
        }

        /// <summary>Drop a mid-air placement: Core gravity takes over until it lands.</summary>
        public void BeginDrop()
        {
            if (!IsPlaced) return;
            settled = false;
            dropTime = 0f;
            restSteps = 0;
            Body?.SetDynamic();
        }

        /// <summary>One fixed step of item physics; the runner calls this after stepping the world.</summary>
        public void StepPhysics(float deltaTime)
        {
            if (!IsPlaced || settled) return;
            CoreBody2D physics = Body;
            if (physics == null || !physics.Body.isValid) return;

            physics.StabilizeRotation();
            physics.SyncTransform();
            dropTime += deltaTime;

            Vector2 velocity = physics.Body.linearVelocity;
            if (dropTime > 0.15f && velocity.sqrMagnitude <= 0.01f)
            {
                restSteps++;
            }
            else
            {
                restSteps = 0;
            }

            if (restSteps >= 5) MarkSettled();
        }
    }
}
