namespace BlindDelivery.Core.UI
{
    /// <summary>
    /// A narrow contract for overlays that must swallow world pointer input (a HUD toolbar, a
    /// pause menu, a modal popup). Gameplay asks this instead of depending on a concrete view.
    /// </summary>
    public interface IPointerBlocker
    {
        bool BlocksWorldInput { get; }
    }
}
