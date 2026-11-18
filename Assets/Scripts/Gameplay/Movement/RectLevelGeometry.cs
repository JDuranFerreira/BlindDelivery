using System.Collections.Generic;
using BlindDelivery.Core.Movement;
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

        public void Rebuild()
        {
            solids.Clear();
            CoreBody2D[] bodies = FindObjectsByType<CoreBody2D>(FindObjectsInactive.Exclude);
            foreach (CoreBody2D body in bodies)
            {
                // The deliveryman is a dynamic body; only static level pieces are solid geometry.
                if (body.GetComponent<CoreMotor>() != null) continue;
                if (body.GetComponent<Deliveryman>() != null) continue;

                SpriteRenderer renderer = body.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                solids.Add(renderer.bounds);
            }
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
