using BlindDelivery.Core.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.Movement
{
    /// <summary>
    /// Point-probe sensor over level geometry. Small, deterministic and independent of the physics
    /// solver, so the fixed-step simulation stays reproducible.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProbeMovementSensor : MonoBehaviour, IMovementSensor
    {
        [SerializeField] RectLevelGeometry geometry;
        [SerializeField, Min(0.01f)] float halfWidth = 0.45f;
        [SerializeField, Min(0.01f)] float halfHeight = 0.5f;
        [SerializeField, Min(0f)] float skin = 0.05f;
        [SerializeField, Min(0.01f)] float groundProbeDistance = 0.25f;

        ILevelGeometry Geometry => geometry != null
            ? geometry
            : (geometry = FindAnyObjectByType<RectLevelGeometry>());

        public MovementSensorResult Scan(float x, float y, int facing)
        {
            var result = new MovementSensorResult();
            ILevelGeometry solids = Geometry;
            if (solids == null) return result;

            float footY = y - halfHeight - groundProbeDistance;
            result.WallAhead = solids.Contains(x + facing * (halfWidth + skin), y);
            result.GroundAhead = solids.Contains(x + facing * halfWidth * 0.5f, footY);
            result.GroundBelow = solids.Contains(x, footY);
            return result;
        }
    }
}
