using Unity.Collections;
using Unity.U2D.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Physics
{
    public sealed partial class CoreBody2D : MonoBehaviour
    {
        Vector2? shapeSizeOverride;

        /// <summary>
        /// Resizes the live box shape while keeping its bottom edge pinned to the Transform's
        /// origin (sprites in this project use bottom-centre pivots). Used by squashing items:
        /// the collider follows the animation so the deliveryman stays supported during the
        /// collapse instead of dropping through immediately.
        /// </summary>
        public void ResizeShape(Vector2 newSize)
        {
            if (newSize.x <= 0f || newSize.y <= 0f) return;
            shapeSizeOverride = newSize;

            if (!created || !body.isValid) return;
            NativeArray<PhysicsShape> shapes = body.GetShapes(Allocator.Temp);
            try
            {
                if (shapes.Length == 0) return;
                PhysicsShape shape = shapes[0];
                shape.polygonGeometry = PolygonGeometry.CreateBox(newSize);
                body.position = new Vector2(body.position.x, transform.position.y + newSize.y * 0.5f);
                body.awake = true;
            }
            finally
            {
                if (shapes.IsCreated) shapes.Dispose();
            }
        }

        /// <summary>Restores the authored size (used if a squash effect is replayed).</summary>
        public void ResetShape()
        {
            shapeSizeOverride = null;
        }
    }
}
