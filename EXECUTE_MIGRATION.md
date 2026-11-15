# Unity Project Migration - Executable Guide

## Overview
This project needs migration from deprecated Unity technologies:
1. **Input Manager** → **Input System** 
2. **Built-in Render Pipeline** → **Universal Render Pipeline (URP)**

## Current Status Analysis

Based on the project structure and existing code:

### Input System Status
- ✅ **Current State**: The project has `InputService.cs` in `Assets/Scripts/Services/Input/` which uses Unity's new Input System for gesture controls (tap, hold, swipe, drag, pinch)
- ❌ **Issue**: Legacy `InputManager.asset` still exists in `ProjectSettings/` 
- 📝 **Current Usage**: The InputService provides touch/gesture controls but basic movement controls may still use Input Manager
- 🔄 **Migration Needed**: Remove InputManager references, ensure Input Actions are properly configured

### Built-in Render Pipeline Status  
- ✅ **Current State**: Modern architecture with multiple assemblies
- ❌ **Issue**: `GraphicsSettings.asset` shows `m_DefaultRenderingPath: 1` (Built-in Pipeline)
- 📝 **Current Usage**: Not using URP or HDRP
- 🔄 **Migration Needed**: Migrate to URP for mobile optimization

## Migration Strategy

### Priority 1: Input System Migration
Create a comprehensive input system division with:
- Input Actions division for player controls
- Proper action mappings for movement, jump, fire, look, pause
- Migration from InputManager axes to Input Actions

### Priority 2: URP Migration  
Migrate to Universal Render Pipeline:
- Install URP package
- Create URP asset configured for mobile
- Update graphics settings
- Optimize materials for URP

## Step-by-Step Execution Plan

### Step 1: Install Required Packages

#### Input System Package
```bash
# If Unity CLI is available, run:
unity -batchmode -editProject -executeMethod "InstallInputSystemPackage"

# Manual installation:
# In Unity: Assets > Input System > Install Input System Package
```

#### Universal Render Pipeline
```bash
# If Unity CLI is available, run:
unity -batchmode -editProject -executeMethod "InstallURPPackage" 

# Manual installation:
# In Unity: Assets > Rendering > Universal Render Pipeline > Install URP
```

### Step 2: Create Input System Division

Create the directory structure:
```bash
mkdir -p Assets/InputSystem
mkdir -p Assets/URP
```

#### Create Input Actions Division

1. **Create Input Actions Asset**
   Create `Assets/InputSystem/InputActions.asset` with the following structure:

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

2. **Create Input Actions Script**
   Create `Assets/InputSystem/InputActions.cs`:

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

#### Update Input Service

Update the existing `InputService.cs` to integrate with the new Input Actions:

```csharp
// Add to InputService.cs
using UnityEngine.InputSystem;
using BlindDelivery.Core.Services;

public class InputService : MonoBehaviour, IInputService
{
    private InputActionAsset inputActions;
    private InputActionMap gameplayMap;
    private InputActionMap uiMap;
    
    public async Task InitializeAsync(CancellationToken token = default)
    {
        // Load input actions from the new division
        inputActions = Resources.Load&lt;InputActionAsset&gt;("InputActions");
        if (inputActions != null)
        {
            gameplayMap = inputActions.FindActionMap("Gameplay");
            uiMap = inputActions.FindActionMap("UI");
            
            if (gameplayMap != null)
                gameplayMap.Enable();
                
            if (uiMap != null)
                uiMap.Enable();
        }
        
        await Task.Yield();
    }
    
    // Update all input methods to use new Input Actions
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

### Step 3: URP Migration

#### Create URP Division

1. **Create URP Asset**
   Create `Assets/URP/UniversalRenderPipelineAsset.asset`:

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
    <defaultMaterial>Textures/Material</defaultMaterial>
    <defaultShader>Universal Render Pipeline/Lit</defaultShader>
</UniversalRenderPipelineAsset>
```

2. **Create URP Camera Prefab**
   Create `Assets/URP/UniversalCamera.prefab`:

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

#### Update Graphics Settings

Update `ProjectSettings/GraphicsSettings.asset`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<GraphicsSettings xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <m_DefaultRenderingPath>2</m_DefaultRenderingPath>
    <m_DefaultMobileRenderingPath>2</m_DefaultMobileRenderingPath>
    <m_CustomRenderPipeline>{fileID: 11400000, guid: your-urp-asset-guid, type: 2}</m_CustomRenderPipeline>
</GraphicsSettings>
```

### Step 4: Update Existing Gameplay Components

#### Update BoyController

Update `BlindDelivery.Gameplay.BoyController.cs` to use the new Input Service:

```csharp
using UnityEngine;
using BlindDelivery.Core.Services;

public class BoyController : MonoBehaviour
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
        return isGrounded;
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

### Step 5: Migration Validation

#### Create Validation Script

Create `Assets/Editor/ValidationScript.cs`:

```csharp
using UnityEngine;
using UnityEditor;

public static class ValidationScript
{
    [MenuItem("Tools/Validation/Check Migration Status")]
    public static void CheckMigrationStatus()
    {
        Debug.Log("=== Unity Project Migration Status ===");
        
        // Check Input System
        CheckInputSystem();
        
        // Check URP Migration
        CheckURPMigration();
        
        // Check Gameplay Components
        CheckGameplayComponents();
        
        Debug.Log("=== Migration Check Complete ===");
    }
    
    static void CheckInputSystem()
    {
        Debug.Log("\n--- Input System Status ---");
        
        // Check if Input Actions division exists
        string inputActionsPath = "Assets/InputSystem/InputActions.asset";
        if (System.IO.File.Exists(inputActionsPath))
        {
            Debug.Log("✅ Input Actions asset exists");
        }
        else
        {
            Debug.Log("❌ Input Actions asset not found");
        }
        
        // Check if InputSystemAssets.cs exists
        string scriptPath = "Assets/InputSystem/InputActions.cs";
        if (System.IO.File.Exists(scriptPath))
        {
            Debug.Log("✅ InputActions script exists");
        }
        else
        {
            Debug.Log("❌ InputActions script not found");
        }
        
        // Check if InputManager.asset still exists
        string inputManagerPath = "ProjectSettings/InputManager.asset";
        if (System.IO.File.Exists(inputManagerPath))
        {
            Debug.Log("⚠️  InputManager.asset still exists (should be deprecated)");
        }
        else
        {
            Debug.Log("✅ InputManager.asset not found");
        }
    }
    
    static void CheckURPMigration()
    {
        Debug.Log("\n--- URP Migration Status ---");
        
        // Check if URP division exists
        string urpAssetPath = "Assets/URP/UniversalRenderPipelineAsset.asset";
        if (System.IO.File.Exists(urpAssetPath))
        {
            Debug.Log("✅ URP asset exists");
        }
        else
        {
            Debug.Log("❌ URP asset not found");
        }
        
        // Check Graphics Settings
        string graphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        if (System.IO.File.Exists(graphicsSettingsPath))
        {
            var settings = new UnityEngine.Wrapping.GraphicsSettings();
            if (settings.m_DefaultRenderingPath == 2)
            {
                Debug.Log("✅ Graphics Settings configured for URP");
            }
            else
            {
                Debug.Log("❌ Graphics Settings not configured for URP");
            }
        }
        else
        {
            Debug.Log("❌ Graphics Settings not found");
        }
    }
    
    static void CheckGameplayComponents()
    {
        Debug.Log("\n--- Gameplay Components Status ---");
        
        // Check if BoyController is updated
        string boyControllerPath = "Assets/Scripts/Gameplay/BoyController.cs";
        if (System.IO.File.Exists(boyControllerPath))
        {
            var content = System.IO.File.ReadAllText(boyControllerPath);
            if (content.Contains("InputActions.GetMoveInput") && 
                content.Contains("InputActions.GetJumpInputDown"))
            {
                Debug.Log("✅ BoyController uses new Input System");
            }
            else
            {
                Debug.Log("⚠️  BoyController may need Input System updates");
            }
        }
        else
        {
            Debug.Log("❌ BoyController not found");
        }
        
        // Check if InputService is updated
        string inputServicePath = "Assets/Scripts/Services/Input/InputService.cs";
        if (System.IO.File.Exists(inputServicePath))
        {
            var content = System.IO.File.ReadAllText(inputServicePath);
            if (content.Contains("InputActions.GetMoveInput") && 
                content.Contains("InputActions.GetJumpInputDown"))
            {
                Debug.Log("✅ InputService uses new Input System");
            }
            else
            {
                Debug.Log("⚠️  InputService may need Input System updates");
            }
        }
        else
        {
            Debug.Log("❌ InputService not found");
        }
    }
}
```

### Step 6: Run Migration

1. **In Unity Editor**:
   - Open the `Tools` menu
   - Navigate to `Unity Migration` section
   - Select `Migrate All` to run the complete migration
   
2. **Or execute via CLI**:
```bash
# Run the migration script
unity -batchmode -editProject -executeMethod "UnityMigrationTool.MigrateAll"
```

3. **Run validation**:
```bash
# Run validation script
unity -batchmode -editProject -executeMethod "ValidationScript.CheckMigrationStatus"
```

## Migration Execution Summary

### Quick Start Commands

```bash
# 1. Create directory structure
mkdir -p Assets/InputSystem
mkdir -p Assets/URP

# 2. Run the migration tool (in Unity Editor)
Tools > Unity Migration > Migrate All

# 3. Run validation
Tools > Validation > Check Migration Status

# 4. Test gameplay
# - Run the game in Unity
# - Test input controls
# - Test rendering quality
```

### Expected Timeline

**Phase 1: Foundation (Day 1-2)**
- [ ] Create InputSystem and URP divisions
- [ ] Install required packages
- [ ] Setup basic directory structure

**Phase 2: Core Migration (Day 3-5)**
- [ ] Create Input Actions division
- [ ] Setup URP asset and configuration
- [ ] Update InputService
- [ ] Update gameplay components

**Phase 3: Testing & Validation (Day 6-7)**
- [ ] Run validation script
- [ ] Test input functionality
- [ ] Test rendering pipeline
- [ ] Fix any issues found

**Phase 4: Finalization (Day 8-10)**
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
- [ ] URP asset configured for mobile
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
   - Update `BoyController.cs` and other gameplay scripts
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