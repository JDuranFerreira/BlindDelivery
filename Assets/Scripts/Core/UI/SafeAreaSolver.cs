namespace Template.Core.UI
{
    /// <summary>Distances from a rectangle's four borders to the safe area, all in the same unit.</summary>
    public readonly struct SafeAreaInsets
    {
        public SafeAreaInsets(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public float Left { get; }

        public float Right { get; }

        public float Top { get; }

        public float Bottom { get; }
    }

    /// <summary>
    /// Notch and bar math as a pure function of the OS safe-area rect
    /// (goal system #6: <c>SafeArea</c> from <c>Screen.safeArea</c>). The engine wrapper reports
    /// the rect; this decides the insets, so EditMode tests drive the geometry directly and no
    /// trust of a notch is ever needed at build time.
    /// </summary>
    public static class SafeAreaSolver
    {
        /// <summary>
        /// Insets from the window border to the safe area. Degenerate sizes (zero window, zero
        /// screen) return zeros rather than NaN, so the launch frame on some devices applies 0 and
        /// corrects itself on the next change instead of throwing.
        /// </summary>
        public static SafeAreaInsets Compute(float windowWidth, float windowHeight,
            float safeX, float safeY, float safeWidth, float safeHeight)
        {
            if (windowWidth <= 0 || windowHeight <= 0 || safeWidth <= 0 || safeHeight <= 0)
            {
                return new SafeAreaInsets(0f, 0f, 0f, 0f);
            }

            float w = System.Math.Clamp(windowWidth, 0f, float.MaxValue);
            float h = System.Math.Clamp(windowHeight, 0f, float.MaxValue);

            // Unity reports safeArea in window space, bottom-left origin: clamp the rect onto the
            // window first, the math then is subtractions.
            float x = Clamp(safeX, 0f, w);
            float y = Clamp(safeY, 0f, h);
            float width = Clamp(safeWidth + x, x, w) - x;
            float height = Clamp(safeHeight + y, y, h) - y;

            return new SafeAreaInsets(
                left: x,
                right: w - (x + width),
                top: h - (y + height),
                bottom: y);
        }

        static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}