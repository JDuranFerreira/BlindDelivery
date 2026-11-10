using System;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Services;

namespace Template.Core.Input
{
    /// <summary>
    /// One clean layer over touch, gestures, and optional controller/keyboard support.
    /// Game code never touches raw input; it subscribes to these events.
    /// </summary>
    public interface IInputService : IAppService
    {
        /// <summary>Raised when a single tap occurs (not on UI).</summary>
        event Action<Vector2> OnTap;

        /// <summary>Raised when a double-tap occurs (not on UI).</summary>
        event Action<Vector2> OnDoubleTap;

        /// <summary>Raised when a hold begins (press held for HoldThreshold seconds, not on UI).</summary>
        event Action<Vector2> OnHold;

        /// <summary>Raised when a swipe ends (not on UI). Direction is normalized.</summary>
        event Action<Vector2> OnSwipe;

        /// <summary>Raised continuously during a drag (not on UI). Delta is in screen pixels.</summary>
        event Action<Vector2> OnDrag;

        /// <summary>Raised when a pinch gesture changes. Scale is relative (1 = no change).</summary>
        event Action<float> OnPinch;

        /// <summary>Current touch/pointer position in screen pixels, if any.</summary>
        Vector2? PointerPosition { get; }

        /// <summary>Whether a pointer is currently pressed (and not on UI).</summary>
        bool IsPointerDown { get; }
    }

    /// <summary>
    /// Simple 2D vector for input events — no UnityEngine dependency in Core.
    /// </summary>
    public readonly struct Vector2 : IEquatable<Vector2>
    {
        public readonly float X;
        public readonly float Y;

        public Vector2(float x, float y) { X = x; Y = y; }

        public static Vector2 Zero => new Vector2(0f, 0f);
        public static Vector2 One => new Vector2(1f, 1f);

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);
        public Vector2 Normalized => Magnitude > 0f ? new Vector2(X / Magnitude, Y / Magnitude) : Zero;

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.X * s, a.Y * s);
        public static Vector2 operator /(Vector2 a, float s) => new Vector2(a.X / s, a.Y / s);

        public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vector2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);
        public static bool operator !=(Vector2 a, Vector2 b) => !a.Equals(b);
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }
}