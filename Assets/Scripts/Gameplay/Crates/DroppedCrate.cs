using BlindDelivery.Core.Crates;
using BlindDelivery.Core.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.Crates
{
    /// <summary>
    /// Visual for one crate knocked off the stack: falls intact, then swaps to the cracked sprite
    /// the moment it touches level geometry. Pure rules decide the landing; this component only
    /// moves the transform and changes the sprite. It deliberately has no CoreBody2D — a dropped
    /// crate must not become a solid for the deliveryman's movement sensor.
    /// </summary>
    public sealed class DroppedCrate : MonoBehaviour
    {
        SpriteRenderer renderer;
        Sprite crackedSprite;
        ILevelGeometry solids;
        float gravity;
        float probeSkin;
        float halfHeight;
        float velocity;
        bool landed;

        public bool Landed => landed;

        public void Initialize(Sprite crackedSprite, ILevelGeometry solids, float gravity, float probeSkin)
        {
            this.crackedSprite = crackedSprite;
            this.solids = solids;
            this.gravity = gravity;
            this.probeSkin = probeSkin;

            renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                halfHeight = renderer.bounds.extents.y;
                if (halfHeight <= 0f) halfHeight = 0.5f;
            }
            else halfHeight = 0.5f;
        }

        /// <summary>One visual step. Returns true once the crate has landed.</summary>
        public bool Step(float deltaTime)
        {
            if (landed) return true;

            velocity = CrateFallRules.Integrate(velocity, gravity, deltaTime);
            Vector3 position = transform.position;
            position.y += velocity * deltaTime;
            transform.position = position;

            bool solidBelow = solids != null && solids.Contains(position.x, position.y - halfHeight - probeSkin);
            if (!CrateFallRules.HasLanded(solidBelow, velocity <= 0f)) return false;

            landed = true;
            if (renderer != null && crackedSprite != null) renderer.sprite = crackedSprite;
            return true;
        }
    }
}
