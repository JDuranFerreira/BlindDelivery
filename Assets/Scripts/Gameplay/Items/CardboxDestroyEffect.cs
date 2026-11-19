using BlindDelivery.Gameplay.Movement;
using BlindDelivery.Gameplay.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Items
{
    /// <summary>
    /// Runs the cardboxDestroyAnimation after the deliveryman lands on a cardbox. Each frame's
    /// visible height has a matching box shape height, so the collider collapses top-down with
    /// the sprite and keeps supporting the character until the box is gone. Then the item is
    /// removed from placement/geometry and the deliveryman falls to the floor normally.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardboxDestroyEffect : MonoBehaviour
    {
        // Frame heights of cardboxDestroyAnimation.png: bottom pivot fixed, top squashes down.
        static readonly float[] FrameHeights = { 1f, 0.78f, 0.55f, 0.28f, 0.08f };

        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] CoreBody2D body;
        [SerializeField, Min(0.05f)] float duration = 0.5f;
        [SerializeField] Vector2 fullSize = Vector2.one;

        RectLevelGeometry geometry;
        ItemPlacementController placement;
        float elapsed;
        int lastFrame = -1;
        bool playing;
        bool finished;

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (body == null) body = GetComponent<CoreBody2D>();
            geometry = FindAnyObjectByType<RectLevelGeometry>();
            placement = FindAnyObjectByType<ItemPlacementController>();
        }

        /// <summary>Starts the destroy animation once, on a cushioned landing.</summary>
        public void Play()
        {
            if (playing || finished) return;
            playing = true;
            elapsed = 0f;
            lastFrame = -1;
            if (animator != null)
            {
                // Cardbox_Destroy.anim is 0.5s; keep its frames in step with the box shape.
                animator.speed = 0.5f / duration;
                animator.SetTrigger("Destroy");
            }
        }

        void Update()
        {
            if (!playing) return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int frame = Mathf.Min(FrameHeights.Length - 1, Mathf.FloorToInt(t * FrameHeights.Length));
            if (frame != lastFrame)
            {
                lastFrame = frame;
                body?.ResizeShape(new Vector2(fullSize.x, fullSize.y * FrameHeights[frame]));
                geometry?.Rebuild();
            }

            if (t >= 1f) Finish();
        }

        void Finish()
        {
            playing = false;
            finished = true;
            if (body != null) body.enabled = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (animator != null) animator.enabled = false;
            placement?.RemoveItem(GetComponent<GuidanceItem>());
            geometry?.Rebuild();
        }
    }
}
