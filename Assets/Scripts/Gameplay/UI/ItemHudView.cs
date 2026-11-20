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
        Label phase;
        Label cooldown;
        VisualElement cooldownFill;
        VisualElement pointsFill;
        readonly VisualElement[] toolCooldownFills = new VisualElement[6];
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
            phase = Find<Label>("Phase");
            cooldown = Find<Label>("Cooldown");
            cooldownFill = Find<VisualElement>("CooldownFill");
            pointsFill = Find<VisualElement>("PointsFill");
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int index = i;
                toolButtons[i] = Find<Button>("Tool" + i);
                toolImages[i] = Find<Image>("ToolIcon" + i);
                toolCooldownFills[i] = Find<VisualElement>("ToolCooldown" + i);
                if (toolButtons[i] == null) continue;

                toolButtons[i].clicked += () => OnToolClicked(index);
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

            if (pointsFill != null)
            {
                float pool = state.MaxPoints > 0 ? Mathf.Clamp01((float)state.Points / state.MaxPoints) : 0f;
                pointsFill.style.width = new StyleLength(Length.Percent(pool * 100f));
            }
            if (phase != null)
            {
                if (state.IsPreparing)
                {
                    phase.text = "PREP " + Mathf.CeilToInt(state.PrepRemainingSeconds);
                    phase.AddToClassList("preparing");
                }
                else
                {
                    phase.text = "GO!";
                    phase.RemoveFromClassList("preparing");
                }
            }

            SetText(tool, "Tool: " + (state.SelectedTool.HasValue ? state.SelectedTool.Value.ToString() : "none"));

            float remaining = state.ToolCooldownSeconds;
            string message = state.SelectedTool.HasValue ? "Tap map to place" : "Select a tool";
            if (remaining > 0f) message = "Cooldown " + remaining.ToString("0.0") + "s";
            SetText(cooldown, message);

            if (cooldownFill != null)
            {
                float ratio = state.ToolCooldownRatio;
                cooldownFill.style.width = new StyleLength(Length.Percent(ratio * 100f));
                if (remaining > 0f) cooldownFill.AddToClassList("cooling");
                else cooldownFill.RemoveFromClassList("cooling");
            }

            for (int i = 0; i < toolButtons.Length; i++)
            {
                if (toolButtons[i] == null) continue;

                if (i >= state.Tools.Count)
                {
                    // This level does not grant this slot — hide it instead of showing a dimmed
                    // icon for an item the player can never use.
                    toolButtons[i].style.display = DisplayStyle.None;
                    if (toolCooldownFills[i] != null)
                        toolCooldownFills[i].style.width = new StyleLength(Length.Percent(0f));
                    continue;
                }

                toolButtons[i].style.display = DisplayStyle.Flex;
                ItemType slotType = state.Tools[i];
                if (toolImages[i] != null) toolImages[i].sprite = IconFor(slotType);

                // Availability comes from the factory built in ItemPlacementController.Start(),
                // which runs AFTER this view's OnEnable binds the buttons. Refresh every frame so
                // buttons unlock as soon as the tools exist instead of staying disabled forever.
                bool available = state.IsToolAvailable(slotType);
                toolButtons[i].SetEnabled(available);
                if (available) toolButtons[i].RemoveFromClassList("unavailable");
                else toolButtons[i].AddToClassList("unavailable");

                if (state.SelectedTool == slotType) toolButtons[i].AddToClassList("selected");
                else toolButtons[i].RemoveFromClassList("selected");

                if (toolCooldownFills[i] != null)
                {
                    float fill = state.CooldownRatioFor(slotType);
                    toolCooldownFills[i].style.width = new StyleLength(Length.Percent(fill * 100f));
                }
            }
        }

        Sprite IconFor(ItemType type)
        {
            int index = (int)type;
            return index >= 0 && index < toolIcons.Length ? toolIcons[index] : null;
        }

        void OnToolClicked(int index)
        {
            lastUiClick = Time.unscaledTime;
            state?.SelectTool(index);
        }
    }
}
