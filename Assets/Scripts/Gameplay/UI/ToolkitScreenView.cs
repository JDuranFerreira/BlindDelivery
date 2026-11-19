using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlindDelivery.Gameplay.UI
{
    /// <summary>
    /// Base class for every runtime UI Toolkit screen. It owns the shared plumbing — stylesheet
    /// application, element lookup, safe area and the IScreenView show/hide contract — so a
    /// concrete screen only binds its own elements and renders its own state.
    /// </summary>
    public abstract class ToolkitScreenView : MonoBehaviour, IScreenView
    {
        [SerializeField] string screenId;
        [SerializeField] UIDocument document;
        [SerializeField] StyleSheet sharedStyle;
        [SerializeField] StyleSheet screenStyle;
        [SerializeField] string safeAreaElementName = "SafeArea";

        VisualElement safeArea;
        Rect lastSafeArea;
        Vector2 lastScreen;

        public string ScreenId => screenId;

        protected VisualElement Root => document != null ? document.rootVisualElement : null;

        void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            if (document == null)
            {
                Debug.LogError($"[{GetType().Name}] No UIDocument on this screen.", this);
                return;
            }

            VisualElement root = Root;
            if (sharedStyle != null && !root.styleSheets.Contains(sharedStyle)) root.styleSheets.Add(sharedStyle);
            if (screenStyle != null && !root.styleSheets.Contains(screenStyle)) root.styleSheets.Add(screenStyle);

            safeArea = string.IsNullOrEmpty(safeAreaElementName)
                ? null
                : UQueryExtensions.Query(root, safeAreaElementName).First();

            Bind(root);
            lastSafeArea = Rect.zero;
            lastScreen = Vector2.zero;
            ApplySafeArea(root);
        }

        void Update()
        {
            Rect currentSafe = Screen.safeArea;
            Vector2 currentScreen = new Vector2(Screen.width, Screen.height);
            if (currentSafe == lastSafeArea && currentScreen == lastScreen) return;

            lastSafeArea = currentSafe;
            lastScreen = currentScreen;
            ApplySafeArea(Root);
        }

        /// <summary>Query and wire this screen's own elements. Called once after styles are applied.</summary>
        protected abstract void Bind(VisualElement root);

        protected T Find<T>(string name) where T : VisualElement
            => Root == null ? null : UQueryExtensions.Query<T>(Root, name).First();

        protected static void SetText(Label label, string value)
        {
            if (label != null) label.text = value;
        }

        public Task ShowAsync(CancellationToken token = default)
        {
            Root?.RemoveFromClassList("screen-hidden");
            return Task.CompletedTask;
        }

        public Task HideAsync(CancellationToken token = default)
        {
            Root?.AddToClassList("screen-hidden");
            return Task.CompletedTask;
        }

        void ApplySafeArea(VisualElement root)
        {
            if (safeArea == null || root == null || Screen.width <= 0 || Screen.height <= 0) return;

            SafeAreaInsets insets = SafeAreaSolver.Compute(
                Screen.width, Screen.height,
                Screen.safeArea.x, Screen.safeArea.y,
                Screen.safeArea.width, Screen.safeArea.height);

            // PanelSettings scales reference pixels to screen pixels; convert the OS insets back
            // into the same reference space so padding matches the USS sizes.
            Vector2Int reference = document.panelSettings != null
                ? document.panelSettings.referenceResolution
                : new Vector2Int(320, 480);
            float scaleX = reference.x / (float)Screen.width;
            float scaleY = reference.y / (float)Screen.height;

            safeArea.style.paddingLeft = insets.Left * scaleX;
            safeArea.style.paddingRight = insets.Right * scaleX;
            safeArea.style.paddingTop = insets.Top * scaleY;
            safeArea.style.paddingBottom = insets.Bottom * scaleY;
        }
    }
}
