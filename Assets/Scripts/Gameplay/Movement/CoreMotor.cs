using BlindDelivery.Core.Movement;
using BlindDelivery.Gameplay.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Movement
{
    /// <summary>
    /// Adapter that lets the pure state machine drive a 2D Physics Core body without knowing
    /// anything about Unity or Unity.U2D.Physics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoreMotor : MonoBehaviour, IMotor
    {
        [SerializeField] CoreBody2D body;

        CoreBody2D Body => body != null ? body : (body = GetComponent<CoreBody2D>());

        public void SetVelocity(float x, float y)
        {
            if (Body == null || !Body.Body.isValid) return;
            Unity.U2D.Physics.PhysicsBody physicsBody = Body.Body;
            // A settled body can sleep; writing velocity to a sleeping body is ignored.
            physicsBody.awake = true;
            physicsBody.linearVelocity = new Vector2(x, y);
        }

        public void ReadVelocity(out float x, out float y)
        {
            if (Body != null && Body.Body.isValid)
            {
                Vector2 velocity = Body.Body.linearVelocity;
                x = velocity.x;
                y = velocity.y;
            }
            else
            {
                x = 0f;
                y = 0f;
            }
        }

        /// <summary>
        /// Contact friction can spin a dynamic box around Z. The walking controller keeps the
        /// character upright by clearing rotation each step instead of constraining the body
        /// (constraining it stops horizontal motion in the Core solver).
        /// </summary>
        public void StabilizeRotation()
        {
            if (Body == null || !Body.Body.isValid) return;
            Unity.U2D.Physics.PhysicsBody physicsBody = Body.Body;
            physicsBody.rotation = Unity.U2D.Physics.PhysicsRotate.identity;
            physicsBody.angularVelocity = 0f;
        }

        public void ReadPosition(out float x, out float y)
        {
            if (Body != null && Body.Body.isValid)
            {
                Vector2 position = Body.Body.position;
                x = position.x;
                y = position.y;
            }
            else
            {
                x = transform.position.x;
                y = transform.position.y;
            }
        }
    }
}
