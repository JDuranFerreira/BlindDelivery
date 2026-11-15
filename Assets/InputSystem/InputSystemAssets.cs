using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemAssets
{
    public static InputActionAsset InputActions { get; private set; }
    private static InputActionMap gameplayMap;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Load or create Input Actions
        InputActions = Resources.Load<InputActionAsset>("InputSystem/InputActions");
        if (InputActions == null)
        {
            Debug.LogError("Input Actions Asset not found! Please create it in Assets/InputSystem/");
            return;
        }
        
        gameplayMap = InputActions.FindActionMap("Gameplay");
        if (gameplayMap != null)
        {
            gameplayMap.Enable();
        }
    }
    
    // Helper methods for common input operations
    public static Vector2 GetMovementInput()
    {
        if (gameplayMap == null) return Vector2.zero;
        return gameplayMap["Move"].ReadValue<Vector2>();
    }
    
    public static bool GetJumpInput()
    {
        if (gameplayMap == null) return false;
        return gameplayMap["Jump"].IsPressed();
    }
    
    public static bool GetJumpInputDown()
    {
        if (gameplayMap == null) return false;
        return gameplayMap["Jump"].WasPressedThisFrame();
    }
    
    public static bool GetFireInput()
    {
        if (gameplayMap == null) return false;
        return gameplayMap["Fire"].IsPressed();
    }
    
    public static bool GetFireInputDown()
    {
        if (gameplayMap == null) return false;
        return gameplayMap["Fire"].WasPressedThisFrame();
    }
    
    public static Vector2 GetLookInput()
    {
        if (gameplayMap == null) return Vector2.zero;
        return gameplayMap["Look"].ReadValue<Vector2>();
    }
    
    public static bool GetPauseInput()
    {
        if (gameplayMap == null) return false;
        return gameplayMap["Pause"].IsPressed();
    }
    
    public static bool GetPauseInputDown()
    {
        if (gameplayMap == null) return false;
        return gameplayMap["Pause"].WasPressedThisFrame();
    }
}