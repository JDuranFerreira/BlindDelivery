using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class UnityMigrationTool
{
    [MenuItem("Tools/Unity Migration/Setup Input System")]
    public static void SetupInputSystem()
    {
        Debug.Log("Setting up Input System for Blind Delivery...");
        
        // Create Input Actions Asset if not exists
        string inputActionsPath = "Assets/InputSystem/InputActions.asset";
        if (!System.IO.File.Exists(inputActionsPath))
        {
            var inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            inputAsset.name = "InputActions";
            
            // Add Gameplay Action Map
            var gameplayMap = new InputActionMap("Gameplay");
            inputAsset.AddActionMap(gameplayMap);
            
            // Add UI Action Map
            var uiMap = new InputActionMap("UI");
            inputAsset.AddActionMap(uiMap);
            
            AssetDatabase.CreateAsset(inputAsset, inputActionsPath);
            AssetDatabase.SaveAssets();
        }
        
        // Create InputActions script if not exists
        string scriptPath = "Assets/InputSystem/InputActions.cs";
        if (!System.IO.File.Exists(scriptPath))
        {
            string scriptContent = @"using UnityEngine;
using UnityEngine.InputSystem;

public static class InputActions
{
    public static InputActionAsset Asset { get; private set; }
    private static InputActionMap gameplayMap;
    private static InputActionMap uiMap;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Asset = Resources.Load<InputActionAsset>(""InputActions"");
        if (Asset != null)
        {
            gameplayMap = Asset.FindActionMap(""Gameplay"");
            uiMap = Asset.FindActionMap(""UI"");
            
            if (gameplayMap != null)
                gameplayMap.Enable();
                
            if (uiMap != null)
                uiMap.Enable();
        }
    }
    
    // Gameplay actions
    public static Vector2 GetMoveInput()
    {
        return gameplayMap?[""Move""].ReadValue<Vector2>() ?? Vector2.zero;
    }
    
    public static bool GetJumpInput()
    {
        return gameplayMap?[""Jump""].IsPressed() ?? false;
    }
    
    public static bool GetJumpInputDown()
    {
        return gameplayMap?[""Jump""].WasPressedThisFrame() ?? false;
    }
    
    public static bool GetFireInput()
    {
        return gameplayMap?[""Fire""].IsPressed() ?? false;
    }
    
    public static bool GetFireInputDown()
    {
        return gameplayMap?[""Fire""].WasPressedThisFrame() ?? false;
    }
    
    public static Vector2 GetLookInput()
    {
        return gameplayMap?[""Look""].ReadValue<Vector2>() ?? Vector2.zero;
    }
    
    public static bool GetPauseInput()
    {
        return gameplayMap?[""Pause""].IsPressed() ?? false;
    }
    
    public static bool GetPauseInputDown()
    {
        return gameplayMap?[""Pause""].WasPressedThisFrame() ?? false;
    }
}
";
            System.IO.File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.Refresh();
        }
        
        Debug.Log("Input System setup completed!");
    }
    
    [MenuItem("Tools/Unity Migration/Setup URP")]
    public static void SetupURP()
    {
        Debug.Log("Setting up URP for Blind Delivery...");
        Debug.LogWarning("[UnityMigration] Legacy path — it hand-authors Assets/URP/UniversalRenderPipelineAsset.asset, which Unity cannot load. Prefer Tools/Blind Delivery/URP (URPMigrationTool), which creates assets through the URP API.");

        // Check if URP is available
        var urpType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
        if (urpType == null)
        {
            Debug.LogError("URP package not installed. Please install 'Universal Render Pipeline' package from Package Manager first.");
            Debug.Log("You can install it via: Window > Package Manager > Unity Registry > Universal RP");
            return;
        }
        
        // Create URP Asset if not exists
        string urpAssetPath = "Assets/URP/UniversalRenderPipelineAsset.asset";
        if (!System.IO.File.Exists(urpAssetPath))
        {
            var urpAsset = ScriptableObject.CreateInstance(urpType);
            urpAsset.name = "BlindDeliveryURPAsset";
            
            // Configure for mobile using reflection
            urpType.GetProperty("allowHDR")?.SetValue(urpAsset, false);
            urpType.GetProperty("allowMSAA")?.SetValue(urpAsset, false);
            urpType.GetProperty("allowLightCookies")?.SetValue(urpAsset, false);
            
            // Set shadow resolution
            var shadowResolutionEnum = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset+ShadowResolution, Unity.RenderPipelines.Universal.Runtime");
            if (shadowResolutionEnum != null)
            {
                var lowValue = System.Enum.Parse(shadowResolutionEnum, "Low");
                urpType.GetProperty("shadowResolution")?.SetValue(urpAsset, lowValue);
            }
            
            AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            
            // Add to Graphics Settings
            UpdateGraphicsSettings(urpAssetPath, urpAsset);
        }
        
        Debug.Log("URP setup completed!");
    }
    
    static void UpdateGraphicsSettings(string urpAssetPath, object urpAsset)
    {
        // UnityEngine.Wrapping does not exist in Unity 6, and a raw mutated
        // settings object was never persisted anyway. Apply through the real
        // GraphicsSettings API, same approach as URPMigrationTool step 2.
        var settings = UnityEngine.Rendering.GraphicsSettings.GetGraphicsSettings();
        if (settings == null)
        {
            Debug.LogError("[UnityMigration] GraphicsSettings object not available.");
            return;
        }
        var so = new SerializedObject(settings);
        var customRP = so.FindProperty("m_CustomRenderPipeline");
        if (customRP == null)
        {
            Debug.LogError("[UnityMigration] m_CustomRenderPipeline not found on GraphicsSettings.");
            return;
        }
        if (urpAsset == null)
        {
            Debug.LogWarning("[UnityMigration] No URP asset to assign.");
            return;
        }
        customRP.objectReferenceValue = urpAsset as UnityEngine.Object;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[UnityMigration] GraphicsSettings now point at the URP asset.");
    }
    
    [MenuItem("Tools/Unity Migration/Migrate All")]
    public static void MigrateAll()
    {
        SetupInputSystem();
        SetupURP();
        Debug.Log("Full Unity migration completed!");
    }
}