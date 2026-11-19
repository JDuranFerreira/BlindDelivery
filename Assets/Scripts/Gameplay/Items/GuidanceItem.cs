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
        SpriteRenderer cooldownRing;
        static Sprite ringSprite;
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

            // GDD §4: a small ring under the item shows its move cooldown. The ring is generated
            // once and shared; it is invisible until SetCooldown is called with a positive ratio.
            GameObject ringObject = new GameObject("CooldownRing");
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            cooldownRing = ringObject.AddComponent<SpriteRenderer>();
            cooldownRing.sprite = GetRingSprite();
            cooldownRing.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Foreground";
            cooldownRing.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
            cooldownRing.color = new Color(1f, 0.84f, 0.35f, 0f);
        }

        /// <summary>Sets the visible cooldown ring; 0 hides it, 1 is a fresh cooldown.</summary>
        public void SetCooldown(float ratio)
        {
            if (cooldownRing == null) return;
            float value = Mathf.Clamp01(ratio);
            Color color = cooldownRing.color;
            color.a = value * 0.9f;
            cooldownRing.color = color;
            cooldownRing.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.6f, value);
        }

        static Sprite GetRingSprite()
        {
            if (ringSprite != null) return ringSprite;

            const int size = 64;
            const float radius = 24f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ItemCooldownRing" };
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((4f - Mathf.Abs(distance - radius)) / 4f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return ringSprite;
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
