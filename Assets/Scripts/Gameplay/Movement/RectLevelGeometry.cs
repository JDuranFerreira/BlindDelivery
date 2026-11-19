using System.Collections.Generic;
using BlindDelivery.Core.Movement;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Levels;
using BlindDelivery.Gameplay.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Movement
{
    /// <summary>
    /// Builds a static solid-space lookup from the level's static CoreBody2D tiles. This is the
    /// gameplay-side implementation of <see cref="ILevelGeometry"/>; the state machine only sees
    /// the interface. Rebuilt once when the runner starts, since level geometry is static.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RectLevelGeometry : MonoBehaviour, ILevelGeometry
    {
        readonly List<Bounds> solids = new List<Bounds>();

        /// <summary>Shared fallback until a level has been rebuilt (placement + geometry agree).</summary>
        public static Bounds DefaultLevelBounds => new Bounds(Vector3.zero, new Vector3(20f, 20f, 1f));

        /// <summary>Union of all static solids; used to reject placements outside the level.</summary>
        public Bounds LevelBounds { get; private set; }

        public void Rebuild()
        {
            solids.Clear();
            bool haveBounds = false;
            Bounds bounds = default;
            CoreBody2D[] bodies = FindObjectsByType<CoreBody2D>(FindObjectsInactive.Exclude);
            foreach (CoreBody2D body in bodies)
            {
                // The deliveryman is a dynamic body; only static level pieces are solid geometry.
                if (body.GetComponent<CoreMotor>() != null) continue;
                if (body.GetComponent<Deliveryman>() != null) continue;

                // Ghosts, dragged items and items still falling are not solids. Rebuild once an
                // item settles so the sensor can use it as ground immediately.
                GuidanceItem item = body.GetComponent<GuidanceItem>();
                if (item != null && (!item.IsPlaced || !item.IsSettled)) continue;

                if (body.IsTrigger) continue;

                Bounds shape = body.WorldShapeBounds;
                solids.Add(shape);

                if (!haveBounds)
                {
                    bounds = shape;
                    haveBounds = true;
                }
                else
                {
                    bounds.Encapsulate(shape);
                }
            }

            LevelBounds = haveBounds ? bounds : DefaultLevelBounds;
        }

        public bool Contains(float x, float y)
        {
            for (int i = 0; i < solids.Count; i++)
            {
                if (solids[i].Contains(new Vector3(x, y, 0f))) return true;
            }
            return false;
        }
    }
}
