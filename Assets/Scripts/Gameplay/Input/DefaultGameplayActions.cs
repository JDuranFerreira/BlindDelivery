using UnityEngine.InputSystem;

namespace BlindDelivery.Gameplay.Input
{
    /// <summary>
    /// Fallback action set for the InputService seam until a level authors its own InputActionAsset.
    /// InputService derives Tap, DoubleTap, Hold, Drag and Swipe from PointerPress + PointerPosition
    /// in Update, so mouse and touch behave identically; no Tap/Hold actions are needed.
    /// </summary>
    public static class DefaultGameplayActions
    {
        const string Json = @"{
  ""name"": ""DefaultGameplayActions"",
  ""maps"": [
    {
      ""name"": ""Gameplay"",
      ""actions"": [
        { ""name"": ""PointerPosition"", ""type"": ""Value"", ""expectedControlType"": ""Vector2"" },
        { ""name"": ""PointerPress"", ""type"": ""Button"" }
      ],
      ""bindings"": [
        { ""path"": ""<Pointer>/position"", ""action"": ""PointerPosition"" },
        { ""path"": ""<Pointer>/press"", ""action"": ""PointerPress"" }
      ]
    }
  ]
}";

        public static InputActionAsset Create() => InputActionAsset.FromJson(Json);
    }
}
