using BlindDelivery.Core.Balance;
using BlindDelivery.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlindDelivery.Gameplay.UI
{
    /// <summary>
    /// The item toolbar screen. All reusable plumbing lives in ToolkitScreenView; this class only
    /// renders IItemHudState and reports whether the pointer is over its toolbar.
    /// </summary>
    public sealed class ItemHudView : ToolkitScreenView, IPointerBlocker
    {
        [SerializeField] UnityEngine.Object stateSource;
        [SerializeField] Sprite[] toolIcons = new Sprite[6];

        readonly Button[] toolButtons = new Button[6];
        readonly Image[] toolImages = new Image[6];
        Label crates;
        Label points;
        Label tool;
        Label cooldown;
        VisualElement cooldownFill;
        IItemHudState state;
        bool pointerOverToolbar;
        float lastUiClick;

        public bool BlocksWorldInput => pointerOverToolbar || Time.unscaledTime - lastUiClick < 0.25f;

        protected override void Bind(VisualElement root)
        {
            state = stateSource as IItemHudState;
            if (state == null)
            {
                foreach (MonoBehaviour candidate in FindObjectsByType<MonoBehaviour>())
                {
                    if (candidate is IItemHudState found)
                    {
                        state = found;
                        break;
                    }
                }
            }

            crates = Find<Label>("Crates");
            points = Find<Label>("Points");
            tool = Find<Label>("Tool");
            cooldown = Find<Label>("Cooldown");
            cooldownFill = Find<VisualElement>("CooldownFill");
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int index = i;
                toolButtons[i] = Find<Button>("Tool" + i);
                toolImages[i] = Find<Image>("ToolIcon" + i);
                if (toolButtons[i] == null) continue;

                if (toolImages[i] != null && i < toolIcons.Length) toolImages[i].sprite = toolIcons[i];
                toolButtons[i].clicked += () => OnToolClicked(index);

                if (state != null && index < state.Tools.Count)
                {
                    bool available = state.IsToolAvailable(state.Tools[index]);
                    toolButtons[i].SetEnabled(available);
                    if (!available) toolButtons[i].AddToClassList("unavailable");
                }
            }

            VisualElement toolbar = UQueryExtensions.Query(root, "Toolbar").First();
            if (toolbar != null)
            {
                toolbar.RegisterCallback<PointerEnterEvent>(_ => pointerOverToolbar = true);
                toolbar.RegisterCallback<PointerLeaveEvent>(_ => pointerOverToolbar = false);
            }
        }

        void Update()
        {
            if (state == null) return;

            SetText(crates, "Crates " + state.Crates);
            SetText(points, "Points " + state.Points);
            SetText(tool, "Tool: " + (state.SelectedTool.HasValue ? state.SelectedTool.Value.ToString() : "none"));

            float remaining = state.ToolCooldownSeconds;
            string message = state.SelectedTool.HasValue ? "Tap map to place" : "Select a tool";
            if (remaining > 0f) message = "Cooldown " + remaining.ToString("0.0") + "s";
            SetText(cooldown, message);

            if (cooldownFill != null)
            {
                float ratio = remaining > 0f ? Mathf.Clamp01(remaining / 2f) : 1f;
                cooldownFill.style.width = new StyleLength(Length.Percent(ratio * 100f));
                if (remaining > 0f) cooldownFill.AddToClassList("cooling");
                else cooldownFill.RemoveFromClassList("cooling");
            }

            for (int i = 0; i < toolButtons.Length; i++)
            {
                if (toolButtons[i] == null || i >= state.Tools.Count) continue;
                if (state.SelectedTool == state.Tools[i]) toolButtons[i].AddToClassList("selected");
                else toolButtons[i].RemoveFromClassList("selected");
            }
        }

        void OnToolClicked(int index)
        {
            lastUiClick = Time.unscaledTime;
            state?.SelectTool(index);
        }
    }
}
