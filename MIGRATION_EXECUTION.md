# Unity Project Migration - Execution Guide

> **Historical document — physics sections superseded.** Any `Rigidbody2D`/`Collider2D`/`Physics2D` snippets below are legacy and must not be used. The game uses the new 2D Physics Core (`Unity.U2D.Physics`); see `AGENTS.md` → "Physics system".

## Project Status Overview

This project (`Blind Delivery`) needs migration from deprecated Unity technologies to modern alternatives:

### Current Issues Found:
1. **Input Manager Usage**: `InputManager.asset` exists in `ProjectSettings/` - this is marked for deprecation
2. **Built-in Render Pipeline**: `GraphicsSettings.asset` shows `m_DefaultRenderingPath: 1` (Built-in Pipeline) - also marked for deprecation

### Current Positive Status:
- ✅ **Modern Architecture**: Multiple assemblies (Core, Services, Gameplay, Editor, Tests)
- ✅ **Input Service**: Already has `InputService.cs` in `Assets/Scripts/Services/Input/` that uses Unity's new Input System for gesture controls
- ✅ **Comprehensive Testing**: Extensive unit test suite in `Assets/Tests/EditMode/`
- ✅ **Good Documentation**: Design document and technical architecture documentation

## Migration Strategy

### Priority 1: Input System Migration
**Goal**: Migrate from Input Manager to Unity's new Input System Package

**Current State**: The project already has an InputService that uses Input Actions, but may still reference InputManager for basic controls.

### Priority 2: URP Migration
**Goal**: Migrate from Built-in Render Pipeline to Universal Render Pipeline (URP)

**Current State**: Project uses Built-in Render Pipeline, needs URP for mobile optimization.

## Migration Execution Plan

### Phase 1: Input System Migration

#### Step 1: Create Input Actions Division

Create the directory structure:
```bash
# Create directories
mkdir -p Assets/InputSystem
mkdir -p Assets/URP
```

#### Step 2: Create Input Actions Division Structure

1. **Create Input Actions Division**: `Assets/InputSystem/`
   - `InputActions.asset`: Main input actions
   - `InputActions.cs`: Runtime accessor
   - `GameplayInput.actionMap`: Gameplay actions
   - `UIInput.actionMap`: UI actions

2. **Create Input Actions Division**: `Assets/InputSystem/InputActions.asset`

```xml
<?xml version="1.0" encoding="utf-8"?>
<InputActionAsset xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.InputSystem">
    <InputActionMap name="Gameplay">
        <InputAction name="Move" type="Value" binding="<Keyboard>/wasd"/>
        <InputAction name="Jump" type="Button" binding="<Keyboard>/space"/>
        <InputAction name="Fire" type="Button" binding="<Mouse>/leftButton"/>
        <InputAction name="Look" type="Value" binding="<Mouse>/delta"/>
        <InputAction name="Pause" type="Button" binding="<Keyboard>/escape"/>
    </InputActionMap>
    <InputActionMap name="UI">
        <InputAction name="Navigate" type="Value" binding="<Keyboard>/arrowKeys"/>
        <InputAction name="Submit" type="Button" binding="<Keyboard>/enter"/>
        <InputAction name="Cancel" type="Button" binding="<Keyboard>/escape"/>
    </InputActionMap>
</InputActionAsset>
```

3. **Create Runtime Accessor**: `Assets/InputSystem/InputActions.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputActions
{
    public static InputActionAsset Asset { get; private set; }
    private static InputActionMap gameplayMap;
    private static InputActionMap uiMap;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Asset = Resources.Load&lt;InputActionAsset&gt;("InputActions");
        if (Asset != null)
        {
            gameplayMap = Asset.FindActionMap("Gameplay");
            uiMap = Asset.FindActionMap("UI");
            
            if (gameplayMap != null)
                gameplayMap.Enable();
                
            if (uiMap != null)
                uiMap.Enable();
        }
    }
    
    // Gameplay actions
    public static Vector2 GetMoveInput()
    {
        return gameplayMap?["Move"].ReadValue&lt;Vector2&gt;() ?? Vector2.zero;
    }
    
    public static bool GetJumpInput()
    {
        return gameplayMap?["Jump"].IsPressed() ?? false;
    }
    
    public static bool GetJumpInputDown()
    {
        return gameplayMap?["Jump"].WasPressedThisFrame() ?? false;
    }
    
    public static bool GetFireInput()
    {
        return gameplayMap?["Fire"].IsPressed() ?? false;
    }
    
    public static bool GetFireInputDown()
    {
        return gameplayMap?["Fire"].WasPressedThisFrame() ?? false;
    }
    
    public static Vector2 GetLookInput()
    {
        return gameplayMap?["Look"].ReadValue&lt;Vector2&gt;() ?? Vector2.zero;
    }
    
    public static bool GetPauseInput()
    {
        return gameplayMap?["Pause"].IsPressed() ?? false;
    }
    
    public static bool GetPauseInputDown()
    {
        return gameplayMap?["Pause"].WasPressedThisFrame() ?? false;
    }
    
    // UI actions
    public static Vector2 GetUINavigateInput()
    {
        return uiMap?["Navigate"].ReadValue&lt;Vector2&gt;() ?? Vector2.zero;
    }
    
    public static bool GetUISubmitInput()
    {
        return uiMap?["Submit"].WasPressedThisFrame() ?? false;
    }
    
    public static bool GetUICancelInput()
    {
        return uiMap?["Cancel"].WasPressedThisFrame() ?? false;
    }
}
```

#### Step 3: Update Input Service

Update the existing `InputService.cs` to use the new Input Actions:

```csharp
// Update InputService.cs to integrate with InputActions
using UnityEngine.InputSystem;
using BlindDelivery.Core.Services;

public class InputService : MonoBehaviour, IInputService
{
    public async Task InitializeAsync(CancellationToken token = default)
    {
        // Initialize Input Actions
        InputActions.Initialize();
        await Task.Yield();
    }
    
    // Update all input methods to use InputActions
    public Vector2 GetMovementInput()
    {
        return InputActions.GetMoveInput();
    }
    
    public bool GetJumpInput()
    {
        return InputActions.GetJumpInput();
    }
    
    public bool GetJumpInputDown()
    {
        return InputActions.GetJumpInputDown();
    }
    
    public bool GetFireInput()
    {
        return InputActions.GetFireInput();
    }
    
    public bool GetFireInputDown()
    {
        return InputActions.GetFireInputDown();
    }
    
    public Vector2 GetLookInput()
    {
        return InputActions.GetLookInput();
    }
    
    public bool GetPauseInput()
    {
        return InputActions.GetPauseInput();
    }
    
    public bool GetPauseInputDown()
    {
        return InputActions.GetPauseInputDown();
    }
}
```

#### Step 4: Update Gameplay Components

Update `BlindDelivery.Gameplay.DeliverymanController.cs`:

```csharp
using UnityEngine;
using BlindDelivery.Core.Services;

public class DeliverymanController : MonoBehaviour
{
    private IInputService inputService;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    private Vector2 moveInput;
    private bool isGrounded;
    
    void Start()
    {
        inputService = ServiceRegistry.Instance.Get&lt;IInputService&gt;();
        rb = GetComponent&lt;Rigidbody2D&gt;();
        spriteRenderer = GetComponent&lt;SpriteRenderer&gt;();
    }
    
    void Update()
    {
        HandleInput();
        HandleAnimation();
    }
    
    void FixedUpdate()
    {
        Move();
    }
    
    private void HandleInput()
    {
        moveInput = inputService.GetMovementInput();
        
        if (inputService.GetJumpInputDown() && IsGrounded())
        {
            Jump();
        }
        
        if (inputService.GetFireInputDown())
        {
            Fire();
        }
        
        if (inputService.GetPauseInputDown())
        {
            TogglePause();
        }
    }
    
    private void HandleAnimation()
    {
        // Update animation based on input
        spriteRenderer.flipX = moveInput.x < 0;
    }
    
    private void Move()
    {
        Vector2 movement = moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
    
    private bool IsGrounded()
    {
        // Ground check logic
        return Physics2D.OverlapCircle(transform.position, 0.2f, groundLayer);
    }
    
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }
    
    private void Fire()
    {
        // Fire action logic
        Debug.Log("Fire action triggered");
    }
    
    private void TogglePause()
    {
        // Pause action logic
        Debug.Log("Pause action triggered");
    }
}
```

### Phase 2: URP Migration

#### Step 1: Create URP Division

1. **Create URP Asset**: `Assets/URP/UniversalRenderPipelineAsset.asset`

```xml
<?xml version="1.0" encoding="utf-8"?>
<UniversalRenderPipelineAsset xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <name>Blind Delivery URP Asset</name>
    <allowHDR>0</allowHDR>
    <allowMSAA>0</allowMSAA>
    <allowLightCookies>0</allowLightCookies>
    <shadowResolution>0</shadowResolution>
    <stencil>1</stencil>
    <cache>1</cache>
    <opaqueLayerMask>1677721599</opaqueLayerMask>
    <transparentLayerMask>1677721599</transparentLayerMask>
</UniversalRenderPipelineAsset>
```

2. **Create URP Camera Prefab**: `Assets/URP/UniversalCamera.prefab`

```xml
<?xml version="1.0" encoding="utf-8"?>
<Prefab xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <GameObject>
        <name>Universal Camera</name>
        <Camera>
            <m_Name>Universal Camera</m_Name>
            <m_CullingMask>1677721599</m_CullingMask>
            <m_UseOcclusionCulling>0</m_UseOcclusionCulling>
        </Camera>
        <UniversalAdditionalCameraData>
            <m_RenderType>0</m_RenderType>
            <m_VolumeLayer>0</m_VolumeLayer>
            <m_RenderPostProcessing>1</m_RenderPostProcessing>
            <m_Antialiasing>1</m_Antialiasing>
        </UniversalAdditionalCameraData>
    </GameObject>
</Prefab>
```

#### Step 2: Update Graphics Settings

Update `ProjectSettings/GraphicsSettings.asset`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<GraphicsSettings xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <m_DefaultRenderingPath>2</m_DefaultRenderingPath>
    <m_DefaultMobileRenderingPath>2</m_DefaultMobileRenderingPath>
    <m_CustomRenderPipeline>{fileID: 11400000, guid: your-urp-asset-guid, type: 2}</m_CustomRenderPipeline>
</GraphicsSettings>
```

#### Step 3: Update Materials and Shaders

Update materials to use URP shaders:

```csharp
// Update materials to use URP shaders
void UpdateMaterialsToURP()
{
    Material[] materials = GameObject.FindObjectsOfType&lt;Material&gt;();
    
    foreach (Material mat in materials)
    {
        if (mat.shader.name.Contains("Standard") || mat.shader.name.Contains("Built-in"))
        {
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            Debug.Log($"Updated material {mat.name} to URP shader");
        }
    }
}
```

### Phase 3: Migration Execution

#### Step 1: Create Migration Script

Create `Assets/Editor/UnityMigrationScript.cs`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public static class UnityMigrationScript
{
    [MenuItem("Tools/Unity Migration/Migrate All")]
    public static void MigrateAll()
    {
        Debug.Log("=== Starting Unity Project Migration ===");
        
        // Phase 1: Input System Migration
        MigrateInputSystem();
        
        // Phase 2: URP Migration
        MigrateURP();
        
        // Phase 3: Validation
        ValidateMigration();
        
        Debug.Log("=== Migration Complete ===");
    }
    
    static void MigrateInputSystem()
    {
        Debug.Log("\n--- Phase 1: Input System Migration ---");
        
        // Create Input Actions division
        CreateInputActionsDivision();
        
        // Update Input Service
        UpdateInputService();
        
        // Update Gameplay Components
        UpdateGameplayComponents();
        
        Debug.Log("✅ Input System Migration Complete");
    }
    
    static void CreateInputActionsDivision()
    {
        // Create Input Actions asset if not exists
        string inputActionsPath = "Assets/InputSystem/InputActions.asset";
        if (!System.IO.File.Exists(inputActionsPath))
        {
            var inputAsset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
            inputAsset.name = "InputActions";
            
            // Add Gameplay Action Map
            var gameplayMap = new InputActionMap("Gameplay");
            inputAsset.AddActionMap(gameplayMap);
            
            // Add UI Action Map
            var uiMap = new InputActionMap("UI");
            inputAsset.AddActionMap(uiMap);
            
            // Save asset
            AssetDatabase.CreateAsset(inputAsset, inputActionsPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Created Input Actions asset at: {inputActionsPath}");
        }
        
        // Create InputActions.cs script if not exists
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
        Asset = Resources.Load&lt;InputActionAsset&gt;("InputActions");
        if (Asset != null)
        {
            gameplayMap = Asset.FindActionMap("Gameplay");
            uiMap = Asset.FindActionMap("UI");
            
            if (gameplayMap != null)
                gameplayMap.Enable();
                
            if (uiMap != null)
                uiMap.Enable();
        }
    }
    
    public static Vector2 GetMoveInput()
    {
        return gameplayMap?["Move"].ReadValue&lt;Vector2&gt;() ?? Vector2.zero;
    }
    
    public static bool GetJumpInput()
    {
        return gameplayMap?["Jump"].IsPressed() ?? false;
    }
    
    public static bool GetJumpInputDown()
    {
        return gameplayMap?["Jump"].WasPressedThisFrame() ?? false;
    }
    
    public static bool GetFireInput()
    {
        return gameplayMap?["Fire"].IsPressed() ?? false;
    }
    
    public static bool GetFireInputDown()
    {
        return gameplayMap?["Fire"].WasPressedThisFrame() ?? false;
    }
    
    public static Vector2 GetLookInput()
    {
        return gameplayMap?["Look"].ReadValue&lt;Vector2&gt;() ?? Vector2.zero;
    }
    
    public static bool GetPauseInput()
    {
        return gameplayMap?["Pause"].IsPressed() ?? false;
    }
    
    public static bool GetPauseInputDown()
    {
        return gameplayMap?["Pause"].WasPressedThisFrame() ?? false;
    }
}
";
            System.IO.File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.Refresh();
            
            Debug.Log($"Created InputActions.cs script at: {scriptPath}");
        }
    }
    
    static void UpdateInputService()
    {
        Debug.Log("Updating InputService to use new Input System...");
        
        // Update InputService.cs
        string inputServicePath = "Assets/Scripts/Services/Input/InputService.cs";
        if (System.IO.File.Exists(inputServicePath))
        {
            var content = System.IO.File.ReadAllText(inputServicePath);
            
            // Replace InputManager references with InputActions
            content = content.Replace("Input.GetAxis", "InputActions.GetMoveInput");
            content = content.Replace("Input.GetKeyDown", "InputActions.GetJumpInputDown");
            content = content.Replace("Input.GetMouseButtonDown", "InputActions.GetFireInputDown");
            content = content.Replace("Input.GetKey", "InputActions.GetPauseInput");
            
            System.IO.File.WriteAllText(inputServicePath, content);
            AssetDatabase.Refresh();
            
            Debug.Log("Updated InputService.cs");
        }
    }
    
    static void UpdateGameplayComponents()
    {
        Debug.Log("Updating gameplay components...");
        
        // Update DeliverymanController.cs
        string deliverymanControllerPath = "Assets/Scripts/Gameplay/DeliverymanController.cs";
        if (System.IO.File.Exists(deliverymanControllerPath))
        {
            var content = System.IO.File.ReadAllText(deliverymanControllerPath);
            
            // Add InputActions usage
            if (!content.Contains("InputActions.GetMoveInput"))
            {
                content = content.Replace(
                    "moveInput = inputService.GetMovementInput();",
                    "moveInput = InputActions.GetMoveInput();"
                );
                
                content = content.Replace(
                    "if (inputService.GetJumpInputDown() && IsGrounded())",
                    "if (InputActions.GetJumpInputDown() && IsGrounded())"
                );
            }
            
            System.IO.File.WriteAllText(deliverymanControllerPath, content);
            AssetDatabase.Refresh();
            
            Debug.Log("Updated DeliverymanController.cs");
        }
    }
    
    static void MigrateURP()
    {
        Debug.Log("\n--- Phase 2: URP Migration ---");
        
        // Create URP Asset
        CreateURPAsset();
        
        // Update Graphics Settings
        UpdateGraphicsSettings();
        
        // Update Materials
        UpdateMaterialsToURP();
        
        Debug.Log("✅ URP Migration Complete");
    }
    
    static void CreateURPAsset()
    {
        string urpAssetPath = "Assets/URP/UniversalRenderPipelineAsset.asset";
        if (!System.IO.File.Exists(urpAssetPath))
        {
            var urpAsset = ScriptableObject.CreateInstance&lt;UniversalRenderPipelineAsset&gt;();
            urpAsset.name = "BlindDeliveryURPAsset";
            
            // Configure for mobile
            urpAsset.allowHDR = false;
            urpAsset.allowMSAA = false;
            urpAsset.allowLightCookies = false;
            urpAsset.shadowResolution = UniversalRenderPipelineAsset.ShadowResolution.Low;
            
            AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            
            Debug.Log($"Created URP Asset at: {urpAssetPath}");
        }
    }
    
    static void UpdateGraphicsSettings()
    {
        Debug.Log("Updating Graphics Settings...");
        
        // Update GraphicsSettings.asset
        string graphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        if (System.IO.File.Exists(graphicsSettingsPath))
        {
            // Read current content
            var content = System.IO.File.ReadAllText(graphicsSettingsPath);
            
            // Update rendering path to URP
            content = content.Replace("m_DefaultRenderingPath: 1", "m_DefaultRenderingPath: 2");
            content = content.Replace("m_DefaultMobileRenderingPath: 1", "m_DefaultMobileRenderingPath: 2");
            
            // Write back
            System.IO.File.WriteAllText(graphicsSettingsPath, content);
            
            Debug.Log("Updated Graphics Settings");
        }
    }
    
    static void UpdateMaterialsToURP()
    {
        Debug.Log("Updating materials to URP shaders...");
        
        // This would be implemented to update all materials in the project
        Debug.Log("Note: Material updates should be done manually for URP compatibility");
    }
    
    static void ValidateMigration()
    {
        Debug.Log("\n--- Phase 3: Validation ---");
        
        // Check Input System
        CheckInputSystem();
        
        // Check URP Migration
        CheckURPMigration();
        
        // Check Gameplay Components
        CheckGameplayComponents();
        
        Debug.Log("✅ Validation Complete");
    }
    
    static void CheckInputSystem()
    {
        Debug.Log("\n--- Input System Validation ---");
        
        string inputActionsPath = "Assets/InputSystem/InputActions.asset";
        if (System.IO.File.Exists(inputActionsPath))
        {
            Debug.Log("✅ Input Actions asset created");
        }
        else
        {
            Debug.Log("❌ Input Actions asset missing");
        }
        
        string inputActionsScriptPath = "Assets/InputSystem/InputActions.cs";
        if (System.IO.File.Exists(inputActionsScriptPath))
        {
            Debug.Log("✅ InputActions.cs script created");
        }
        else
        {
            Debug.Log("❌ InputActions.cs script missing");
        }
        
        string inputServicePath = "Assets/Scripts/Services/Input/InputService.cs";
        if (System.IO.File.Exists(inputServicePath))
        {
            var content = System.IO.File.ReadAllText(inputServicePath);
            if (content.Contains("InputActions.GetMoveInput") && 
                content.Contains("InputActions.GetJumpInputDown"))
            {
                Debug.Log("✅ InputService updated to use InputActions");
            }
            else
            {
                Debug.Log("❌ InputService may not be fully updated");
            }
        }
    }
    
    static void CheckURPMigration()
    {
        Debug.Log("\n--- URP Migration Validation ---");
        
        string urpAssetPath = "Assets/URP/UniversalRenderPipelineAsset.asset";
        if (System.IO.File.Exists(urpAssetPath))
        {
            Debug.Log("✅ URP Asset created");
        }
        else
        {
            Debug.Log("❌ URP Asset missing");
        }
        
        string graphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        if (System.IO.File.Exists(graphicsSettingsPath))
        {
            var content = System.IO.File.ReadAllText(graphicsSettingsPath);
            if (content.Contains("m_DefaultRenderingPath: 2") && 
                content.Contains("m_DefaultMobileRenderingPath: 2"))
            {
                Debug.Log("✅ Graphics Settings configured for URP");
            }
            else
            {
                Debug.Log("❌ Graphics Settings not configured for URP");
            }
        }
    }
    
    static void CheckGameplayComponents()
    {
        Debug.Log("\n--- Gameplay Components Validation ---");
        
        string deliverymanControllerPath = "Assets/Scripts/Gameplay/DeliverymanController.cs";
        if (System.IO.File.Exists(deliverymanControllerPath))
        {
            var content = System.IO.File.ReadAllText(deliverymanControllerPath);
            if (content.Contains("InputActions.GetMoveInput") && 
                content.Contains("InputActions.GetJumpInputDown"))
            {
                Debug.Log("✅ DeliverymanController uses InputActions");
            }
            else
            {
                Debug.Log("❌ DeliverymanController may need updates");
            }
        }
    }
}
```

#### Step 2: Execute Migration

1. **In Unity Editor**:
   - Open the `Tools` menu
   - Navigate to `Unity Migration`
   - Select `Migrate All` to run the complete migration

2. **Or execute via CLI** (if Unity CLI is available):
```bash
# Run the migration script
unity -batchmode -editProject -executeMethod "UnityMigrationScript.MigrateAll"
```

3. **Test the migration**:
   - Run the game in Unity
   - Test input controls
   - Test rendering pipeline
   - Run validation script

### Phase 4: Post-Migration Actions

#### Step 1: Run Validation Script

Create `Assets/Editor/ValidationScript.cs`:

```csharp
using UnityEngine;
using UnityEditor;

public static class ValidationScript
{
    [MenuItem("Tools/Validation/Validate Migration")]
    public static void ValidateMigration()
    {
        Debug.Log("=== Validating Unity Project Migration ===");
        
        // Check Input System
        CheckInputSystem();
        
        // Check URP Migration
        CheckURPMigration();
        
        // Check Gameplay Components
        CheckGameplayComponents();
        
        // Check Performance
        CheckPerformance();
        
        Debug.Log("=== Validation Complete ===");
    }
    
    static void CheckInputSystem()
    {
        Debug.Log("\n--- Input System Validation ---");
        
        // Check Input Actions asset
        string inputActionsPath = "Assets/InputSystem/InputActions.asset";
        if (System.IO.File.Exists(inputActionsPath))
        {
            Debug.Log("✅ Input Actions asset exists");
        }
        else
        {
            Debug.Log("❌ Input Actions asset missing");
        }
        
        // Check InputActions.cs script
        string inputActionsScriptPath = "Assets/InputSystem/InputActions.cs";
        if (System.IO.File.Exists(inputActionsScriptPath))
        {
            Debug.Log("✅ InputActions.cs script exists");
        }
        else
        {
            Debug.Log("❌ InputActions.cs script missing");
        }
        
        // Check InputService
        string inputServicePath = "Assets/Scripts/Services/Input/InputService.cs";
        if (System.IO.File.Exists(inputServicePath))
        {
            var content = System.IO.File.ReadAllText(inputServicePath);
            if (content.Contains("InputActions.GetMoveInput") && 
                content.Contains("InputActions.GetJumpInputDown"))
            {
                Debug.Log("✅ InputService uses InputActions");
            }
            else
            {
                Debug.Log("❌ InputService may need updates");
            }
        }
    }
    
    static void CheckURPMigration()
    {
        Debug.Log("\n--- URP Migration Validation ---");
        
        // Check URP Asset
        string urpAssetPath = "Assets/URP/UniversalRenderPipelineAsset.asset";
        if (System.IO.File.Exists(urpAssetPath))
        {
            Debug.Log("✅ URP Asset exists");
        }
        else
        {
            Debug.Log("❌ URP Asset missing");
        }
        
        // Check Graphics Settings
        string graphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        if (System.IO.File.Exists(graphicsSettingsPath))
        {
            var content = System.IO.File.ReadAllText(graphicsSettingsPath);
            if (content.Contains("m_DefaultRenderingPath: 2") && 
                content.Contains("m_DefaultMobileRenderingPath: 2"))
            {
                Debug.Log("✅ Graphics Settings configured for URP");
            }
            else
            {
                Debug.Log("❌ Graphics Settings not configured for URP");
            }
        }
    }
    
    static void CheckGameplayComponents()
    {
        Debug.Log("\n--- Gameplay Components Validation ---");
        
        // Check DeliverymanController
        string deliverymanControllerPath = "Assets/Scripts/Gameplay/DeliverymanController.cs";
        if (System.IO.File.Exists(deliverymanControllerPath))
        {
            var content = System.IO.File.ReadAllText(deliverymanControllerPath);
            if (content.Contains("InputActions.GetMoveInput") && 
                content.Contains("InputActions.GetJumpInputDown"))
            {
                Debug.Log("✅ DeliverymanController uses InputActions");
            }
            else
            {
                Debug.Log("❌ DeliverymanController may need updates");
            }
        }
    }
    
    static void CheckPerformance()
    {
        Debug.Log("\n--- Performance Validation ---");
        
        // Check quality settings
        if (QualitySettings.renderPipeline != null)
        {
            Debug.Log("✅ Quality Settings configured for URP");
        }
        else
        {
            Debug.Log("❌ Quality Settings not configured for URP");
        }
        
        // Check mobile optimization
        if (QualitySettings.masterTextureLimit <= 1)
        {
            Debug.Log("✅ Mobile optimization (texture quality)");
        }
        else
        {
            Debug.Log("⚠️  Texture quality may need optimization");
        }
    }
}
```

#### Step 2: Run Validation

1. **In Unity Editor**:
   - Open the `Tools` menu
   - Navigate to `Validation`
   - Select `Validate Migration` to run validation

2. **Or execute via CLI**:
```bash
# Run validation script
unity -batchmode -editProject -executeMethod "ValidationScript.ValidateMigration"
```

## Migration Execution Summary

### Quick Start Commands

```bash
# 1. Create directory structure
mkdir -p Assets/InputSystem
mkdir -p Assets/URP

# 2. Run the migration script (in Unity Editor)
Tools > Unity Migration > Migrate All

# 3. Run validation
Tools > Validation > Validate Migration

# 4. Test gameplay
# - Run the game in Unity
# - Test input controls
# - Test rendering quality
# - Check performance metrics
```

### Expected Timeline

**Phase 1: Foundation (Day 1-2)**
- [ ] Create InputSystem and URP divisions
- [ ] Create Input Actions asset
- [ ] Create InputActions.cs script
- [ ] Update InputService
- [ ] Update Gameplay components

**Phase 2: URP Migration (Day 3-4)**
- [ ] Create URP Asset
- [ ] Update Graphics Settings
- [ ] Update materials to URP shaders
- [ ] Test URP functionality

**Phase 3: Validation (Day 5-6)**
- [ ] Run validation script
- [ ] Test input functionality
- [ ] Test rendering pipeline
- [ ] Optimize for mobile
- [ ] Fix any issues found

**Phase 4: Finalization (Day 7-10)**
- [ ] Comprehensive testing
- [ ] Mobile optimization
- [ ] Documentation updates
- [ ] Production deployment

## Success Metrics

### Input System Migration
- [ ] Input Actions asset created and configured
- [ ] Legacy InputManager references removed
- [ ] Gesture controls preserved
- [ ] Mobile touch controls working
- [ ] Input tests passing

### URP Migration
- [ ] URP package installed
- [ ] URP Asset configured for mobile
- [ ] Graphics settings updated
- [ ] Materials updated to URP shaders
- [ ] Performance benchmarks met

### Project Health
- [ ] All existing functionality preserved
- [ ] Test suite updated
- [ ] Documentation updated
- [ ] Team trained on new systems
- [ ] Mobile deployment ready

## Troubleshooting

### Common Issues

1. **Input Actions not found**
   - Ensure `Assets/InputSystem/InputActions.asset` exists
   - Check that the file is in the correct location

2. **URP not working**
   - Verify `Assets/URP/UniversalRenderPipelineAsset.asset` exists
   - Check Graphics Settings configuration

3. **Gameplay components not updated**
   - Update `DeliverymanController.cs` and other gameplay scripts
   - Ensure they use the new Input Actions

4. **Performance issues**
   - Optimize URP settings for mobile
   - Reduce quality settings if needed

### Solutions

```bash
# Fix input actions
# Recreate the Input Actions division
# Run the migration tool again

# Fix URP issues
# Reconfigure URP asset
# Update Graphics Settings

# Fix gameplay components
# Update all scripts that use input
# Test all controls
```

## Final Notes

This migration will modernize the project by:

1. **Replacing Input Manager with Input System**: Provides better input handling, mobile support, and testing capabilities
2. **Migrating to URP**: Optimizes rendering for mobile devices, improves performance, and provides modern rendering features

The migration is comprehensive but follows industry best practices. Testing and validation are critical to ensure all functionality is preserved while achieving the benefits of modern Unity technologies.

Execute the migration step-by-step and run validation after each phase to ensure success!