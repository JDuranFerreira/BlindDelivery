using System.Threading;
using System.Threading.Tasks;
using Template.Core.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace Template.Services.UI
{
    /// <summary>
    /// The engine half of a transition: a full-screen cover on its own top canvas, a black image
    /// that fades to opaque and back plus a filled bar that reports
    /// <see cref="ISceneTransitionOverlay.SetProgress"/> between the two (goal system #2). All
    /// fading runs on unscaled time, so a transition cannot be frozen by whatever the loading
    /// scene does to the clock. The bar only shows while the cover hides the screen, so it never
    /// winks at the player mid-load.
    /// </summary>
    public sealed class SceneFadeOverlay : MonoBehaviour, ISceneTransitionOverlay
    {
        const int OverlaySortOrder = 1000;
        const float ReportEpsilon = 0.001f;

        [SerializeField] float fadeSeconds = 0.35f;

        CanvasGroup cover;
        Image fillBar;

        public bool IsVisible { get; private set; }

        public float FadeSeconds
        {
            get => fadeSeconds;
            set => fadeSeconds = Mathf.Max(0f, value);
        }

        void Awake() => Build();

        /// <summary>Creates the cover lazily but only once, so tests and games can reuse one.</summary>
        void Build()
        {
            if (cover != null)
            {
                return;
            }

            GameObject root = new GameObject("SceneTransitionCanvas");
            root.transform.SetParent(transform, false);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortOrder;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            cover = root.AddComponent<CanvasGroup>();
            cover.alpha = 0f;
            cover.interactable = false;
            cover.blocksRaycasts = false;

            GameObject shadeRect = new GameObject("Cover");
            shadeRect.transform.SetParent(root.transform, false);
            Image shade = shadeRect.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 1f);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;

            GameObject fillRect = new GameObject("Progress");
            fillRect.transform.SetParent(shadeRect.transform, false);
            fillBar = fillRect.AddComponent<Image>();
            fillBar.color = Color.white;
            fillBar.type = Image.Type.Filled;
            fillBar.fillMethod = Image.FillMethod.Horizontal;
            fillBar.fillAmount = 0f;
            fillBar.rectTransform.anchorMin = Vector2.zero;
            fillBar.rectTransform.anchorMax = new Vector2(1f, 0f);
            fillBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            fillBar.rectTransform.anchoredPosition = new Vector2(0f, 180f);
            fillBar.rectTransform.sizeDelta = new Vector2(0f, 12f);
        }

        public async Task FadeOutAsync(CancellationToken token = default)
        {
            Build();
            IsVisible = true;
            cover.blocksRaycasts = true;
            fillBar.enabled = true;
            fillBar.fillAmount = 0f;

            await FadeAlphaAsync(from: 0f, to: 1f, token).ConfigureAwait(true);
        }

        public void SetProgress(float progress01)
        {
            if (fillBar == null || !IsVisible)
            {
                return;
            }

            fillBar.fillAmount = Mathf.Clamp01(progress01);
        }

        public async Task FadeInAsync(CancellationToken token = default)
        {
            // Bar full at this point is a lie the played scene will not keep; park it at 100% for
            // exactly the instant the cover is still edge to edge, and stop drawing it after.
            fillBar.fillAmount = 1f;

            await FadeAlphaAsync(from: 1f, to: 0f, token).ConfigureAwait(true);

            IsVisible = false;
            cover.blocksRaycasts = false;
            fillBar.enabled = false;
            fillBar.fillAmount = 0f;
        }

        async Task FadeAlphaAsync(float from, float to, CancellationToken token)
        {
            float alpha = from;
            cover.alpha = alpha;
            float direction = to >= from ? 1f : -1f;

            while (direction * (cover.alpha - to) < 0f)
            {
                token.ThrowIfCancellationRequested();

                // Unscaled time: the final stretch of a transition often runs while the loading
                // scene has not yet re-applied its own timeScale.
                float step = fadeSeconds > 0f
                    ? Time.unscaledDeltaTime / fadeSeconds
                    : 1f;

                alpha = Mathf.MoveTowards(alpha, to, step);
                cover.alpha = alpha;

                if (Mathf.Abs(cover.alpha - to) < ReportEpsilon || step >= 1f)
                {
                    break;
                }

                await Awaitable.NextFrameAsync(token);
            }

            if (cover != null)
            {
                cover.alpha = to;
            }
        }
    }
}