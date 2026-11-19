using UnityEngine.InputSystem;

namespace BlindDelivery.Gameplay.Input
{
    /// <summary>
    /// Fallback action set for the InputService seam until a level authors its own InputActionAsset.
    /// Mirrors the action names InputService looks up; Tap and Hold use Input System interactions
    /// so a long press no longer races a tap on the same pointer press.
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
        { ""name"": ""PointerPress"", ""type"": ""Button"" },
        { ""name"": ""Tap"", ""type"": ""Button"" },
        { ""name"": ""Hold"", ""type"": ""Button"" }
      ],
      ""bindings"": [
        { ""path"": ""<Pointer>/position"", ""action"": ""PointerPosition"" },
        { ""path"": ""<Pointer>/press"", ""action"": ""PointerPress"" },
        { ""path"": ""<Pointer>/press"", ""action"": ""Tap"", ""interactions"": ""Tap"" },
        { ""path"": ""<Pointer>/press"", ""action"": ""Hold"", ""interactions"": ""Hold(duration=0.5)"" }
      ]
    }
  ]
}";

        public static InputActionAsset Create() => InputActionAsset.FromJson(Json);
    }
}
