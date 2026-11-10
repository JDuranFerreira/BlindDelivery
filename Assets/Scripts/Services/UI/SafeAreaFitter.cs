using Template.Core.UI;
using UnityEngine;

namespace Template.Services.UI
{
    /// <summary>
    /// The engine half of safe-area layout (goal system #6): a stretched panel that keeps its
    /// content inside notches, bars and corners on whatever device it lands on. It re-applies the
    /// insets every frame the OS reports a changed safe area, because Unity recomputes
    /// <c>Screen.safeArea</c> after orientation changes and on some devices on resume. The pure
    /// math lives in <see cref="SafeAreaSolver"/>; this MonoBehaviour only reads the rect and
    /// writes anchors, which is why the geometry is EditMode-tested there and not here.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect lastSafeArea;
        Vector2 lastScreen;

        RectTransform target;

        void Awake()
        {
            target = GetComponent<RectTransform>();
        }

        void OnEnable() => lastSafeArea = Rect.zero;

        void Update()
        {
            // Refresh on change only: writing anchors every frame would fight a Layout Group and
            // cost rebuilds for nothing.
            Rect safe = Screen.safeArea;
            Vector2 screen = new Vector2(Screen.width, Screen.height);

            if (safe == lastSafeArea && screen == lastScreen)
            {
                return;
            }

            lastSafeArea = safe;
            lastScreen = screen;

            if (screen.x <= 0f || screen.y <= 0f)
            {
                // Launch frame corner case: keep the last look, the next valid frame re-fits.
                return;
            }

            SafeAreaInsets insets = SafeAreaSolver.Compute(
                screen.x, screen.y, safe.x, safe.y, safe.width, safe.height);

            target.anchorMin = new Vector2(insets.Left / screen.x, insets.Bottom / screen.y);
            target.anchorMax = new Vector2(1f - (insets.Right / screen.x), 1f - (insets.Top / screen.y));
            target.anchoredPosition = Vector2.zero;
            target.sizeDelta = Vector2.zero;
        }
    }
}