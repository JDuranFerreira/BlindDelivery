using System;
using UnityEngine;

namespace BlindDelivery.Gameplay.Levels
{
    /// <summary>Visual style of a generated platform. Each style maps to a set of sprite prefabs.</summary>
    public enum PlatformType
    {
        Ground,
    }

    /// <summary>
    /// Authoring marker for a platform: place Start and End GameObjects in the level, pick a type,
    /// then use the Inspector's Build button to generate tiled platforms between them.
    /// The generation logic lives in the Editor; this component only holds the scene-side inputs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlatformBuilder : MonoBehaviour
    {
        public PlatformType type = PlatformType.Ground;
        public Transform start;
        public Transform end;
        [Min(0.01f)] public float cellSize = 1f;

        [HideInInspector] public Transform generatedRoot;

        /// <summary>
        /// Evenly spaced, grid-snapped X positions from the left marker to the right marker.
        /// A single position means the markers are closer than one cell.
        /// </summary>
        public static float[] CalculatePositions(float startX, float endX, float cellSize)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            }

            float left = Mathf.Round(Mathf.Min(startX, endX) / cellSize) * cellSize;
            float right = Mathf.Round(Mathf.Max(startX, endX) / cellSize) * cellSize;
            int count = Mathf.Max(1, Mathf.RoundToInt((right - left) / cellSize) + 1);

            var positions = new float[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = left + i * cellSize;
            }

            return positions;
        }
    }
}
