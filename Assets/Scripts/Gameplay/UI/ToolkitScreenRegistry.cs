using BlindDelivery.Services.UI;
using UnityEngine;

namespace BlindDelivery.Gameplay.UI
{
    /// <summary>
    /// Scene-local composer: hands every ToolkitScreenView in this scene to the existing UIManager
    /// service. Views own rendering; the manager owns navigation. Registration is idempotent so a
    /// scene reload cannot double-register a screen id.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToolkitScreenRegistry : MonoBehaviour
    {
        void Awake()
        {
            UIManager manager = null;
            if (AppBootstrap.Instance != null)
            {
                AppBootstrap.Instance.Services.TryGet<UIManager>(out manager);
            }

            if (manager == null)
            {
                Debug.LogError("[ToolkitScreenRegistry] UIManager is not registered.", this);
                return;
            }

            // This service survives scene reloads; its views do not. Drop stale registrations
            // before wiring this scene's screens so replay never shows a destroyed view.
            manager.ResetScreens();

            foreach (ToolkitScreenView view in FindObjectsByType<ToolkitScreenView>())
            {
                manager.TryRegisterScreen(view);
            }
        }
    }
}
