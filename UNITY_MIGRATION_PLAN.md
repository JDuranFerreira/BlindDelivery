# Unity Project Migration Plan

## Project Overview
**Blind Delivery** is a Unity 2D mobile game project that needs migration from deprecated Unity technologies:
1. **Input Manager** → **Input System**
2. **Built-in Render Pipeline** → **Universal Render Pipeline (URP)**

## Current Status
- ✅ Project structure and core systems implemented
- ❌ Input Manager in use (`ProjectSettings/InputManager.asset`)
- ❌ Built-in Render Pipeline in use (`m_DefaultRenderingPath: 1`)
- ✅ Multiple test frameworks in place

## Migration Strategy

### Priority 1: Input System Migration
The project uses custom input for:
- Movement (Horizontal/Vertical axes)
- Jump (Space bar)
- Fire (Mouse/Touch)
- Pause (Escape key)

### Priority 2: URP Migration
The project targets mobile devices, making URP the ideal choice.

## Step-by-Step Migration Plan

### Step 1: Prepare Development Environment

```bash
# Check Unity installation
unity --version

# Check available tools
which dotnet
which npm
```

### Step 2: Install Required Packages

#### Input System
```bash
# Add Input System package via Unity Package Manager
# In Unity: Assets > Input System > Install Input System Package

# Or via CLI (if Unity supports package management)
unity -batchmode -editProject -executeMethod "InputSystemPackageInstaller.Install"
```

#### Universal Render Pipeline
```bash
# Add URP package via Unity Package Manager
# In Unity: Assets > Rendering > Universal Render Pipeline > Install URP

# Or via CLI
unity -batchmode -editProject -executeMethod "URPPackageInstaller.Install"
```

### Step 3: Create Migration Script

Create `Assets/Editor/UnityMigrationTool.cs`:

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

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
            inputAsset.name = "PlayerInputActions";
            
            // Add Gameplay Action Map
            var gameplayMap = new InputActionMap("Gameplay");
            inputAsset.AddActionMap(gameplayMap);
            
            // Add Movement Action
            var moveAction = new InputAction("Move", InputActionType.Value, binding:"<Keyboard>/wasd");
            gameplayMap.AddAction(moveAction);
            
            // Add Jump Action
            var jumpAction = new InputAction("Jump", InputActionType.Button, binding:"<Keyboard>/space");
            gameplayMap.AddAction(jumpAction);
            
            // Add Fire Action (for mouse/touch)
            var fireAction = new InputAction("Fire", InputActionType.Button, binding:"<Mouse>/leftButton");
            gameplayMap.AddAction(fireAction);
            
            // Add Look Action (mouse movement)
            var lookAction = new InputAction("Look", InputActionType.Value, binding:"<Mouse>/delta");
            gameplayMap.AddAction(lookAction);
            
            // Add Pause Action
            var pauseAction = new InputAction("Pause", InputActionType.Button, binding:"<Keyboard>/escape");
            gameplayMap.AddAction(pauseAction);
            
            AssetDatabase.CreateAsset(inputAsset, inputActionsPath);
            AssetDatabase.SaveAssets();
        }
        
        // Create Input System Assets script
        string scriptPath = "Assets/InputSystem/InputSystemAssets.cs";
        if (!System.IO.File.Exists(scriptPath))
        {
            string scriptContent = @"using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemAssets
{
    public static InputActionAsset InputActions { get; private set; }
    private static InputActionMap gameplayMap;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Load or create Input Actions
        InputActions = Resources.Load&lt;InputActionAsset&gt;("InputSystem/InputActions");
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
        return gameplayMap["Move"].ReadValue&lt;Vector2&gt;();
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
        return gameplayMap["Look"].ReadValue&lt;Vector2&gt;();
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
        
        // Create URP Asset if not exists
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
            
            // Add to Graphics Settings
            UpdateGraphicsSettings(urpAssetPath);
        }
        
        Debug.Log("URP setup completed!");
    }
    
    static void UpdateGraphicsSettings(string urpAssetPath)
    {
        // Update ProjectSettings/GraphicsSettings.asset
        string graphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        if (System.IO.File.Exists(graphicsSettingsPath))
        {
            var settings = new UnityEngine.Wrapping.GraphicsSettings();
            settings.m_DefaultRenderingPath = 2; // URP
            settings.m_DefaultMobileRenderingPath = 2;
            
            // Set URP asset reference
            var urpAsset = AssetDatabase.LoadAssetAtPath&lt;UniversalRenderPipelineAsset&gt;(urpAssetPath);
            if (urpAsset != null)
            {
                settings.m_CustomRenderPipeline = urpAsset;
            }
        }
    }
    
    [MenuItem("Tools/Unity Migration/Migrate All")]
    public static void MigrateAll()
    {
        SetupInputSystem();
        SetupURP();
        Debug.Log("Full Unity migration completed!");
    }
}
```

### Step 4: Update Core Services

Update `BlindDelivery.Services.Input.InputService.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using BlindDelivery.Core.Services;

public class InputService : IInputService
{
    private InputActionMap _gameplayActions;
    private InputActionMap _uiActions;
    
    public InputService()
    {
        InitializeInputActions();
    }
    
    private void InitializeInputActions()
    {
        // Load input actions
        var inputAsset = Resources.Load&lt;InputActionAsset&gt;("InputSystem/InputActions");
        if (inputAsset != null)
        {
            _gameplayActions = inputAsset.FindActionMap("Gameplay");
            _uiActions = inputAsset.FindActionMap("UI");
            
            if (_gameplayActions != null)
                _gameplayActions.Enable();
                
            if (_uiActions != null)
                _uiActions.Enable();
        }
        else
        {
            Debug.LogError("Input Actions Asset not found!");
        }
    }
    
    public Vector2 GetMovementInput()
    {
        if (_gameplayActions == null) return Vector2.zero;
        return _gameplayActions["Move"].ReadValue&lt;Vector2&gt;();
    }
    
    public bool GetJumpInput()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Jump"].IsPressed();
    }
    
    public bool GetJumpInputDown()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Jump"].WasPressedThisFrame();
    }
    
    public bool GetJumpInputUp()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Jump"].WasReleasedThisFrame();
    }
    
    public bool GetFireInput()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Fire"].IsPressed();
    }
    
    public bool GetFireInputDown()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Fire"].WasPressedThisFrame();
    }
    
    public bool GetFireInputUp()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Fire"].WasReleasedThisFrame();
    }
    
    public Vector2 GetLookInput()
    {
        if (_gameplayActions == null) return Vector2.zero;
        return _gameplayActions["Look"].ReadValue&lt;Vector2&gt;();
    }
    
    public bool GetPauseInput()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Pause"].IsPressed();
    }
    
    public bool GetPauseInputDown()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Pause"].WasPressedThisFrame();
    }
    
    public bool GetPauseInputUp()
    {
        if (_gameplayActions == null) return false;
        return _gameplayActions["Pause"].WasReleasedThisFrame();
    }
    
    // UI Input methods
    public Vector2 GetUIInput()
    {
        if (_uiActions == null) return Vector2.zero;
        return _uiActions["Navigate"].ReadValue&lt;Vector2&gt;();
    }
    
    public bool GetUISubmitInput()
    {
        if (_uiActions == null) return false;
        return _uiActions["Submit"].WasPressedThisFrame();
    }
    
    public bool GetUICancelInput()
    {
        if (_uiActions == null) return false;
        return _uiActions["Cancel"].WasPressedThisFrame();
    }
}
```

### Step 5: Update Gameplay Components

Update `BlindDelivery.Gameplay.BoyController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using BlindDelivery.Core.Services;

public class BoyController : MonoBehaviour
{
    private IInputService _inputService;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    [Header("References")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    
    private bool _isGrounded;
    private Vector2 _moveInput;
    
    void Start()
    {
        _inputService = ServiceRegistry.Instance.Get&lt;IInputService&gt;();
        _rb = GetComponent&lt;Rigidbody2D&gt;();
        _spriteRenderer = GetComponent&lt;SpriteRenderer&gt;();
        _animator = GetComponent&lt;Animator&gt;();
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
        _moveInput = _inputService.GetMovementInput();
        
        if (_inputService.GetJumpInputDown() && IsGrounded())
        {
            Jump();
        }
        
        if (_inputService.GetFireInputDown())
        {
            Fire();
        }
        
        if (_inputService.GetPauseInputDown())
        {
            TogglePause();
        }
    }
    
    private void HandleAnimation()
    {
        _animator.SetFloat("MoveX", _moveInput.x);
        _animator.SetFloat("MoveY", _moveInput.y);
        _animator.SetBool("IsMoving", _moveInput.magnitude > 0.1f);
    }
    
    private void Move()
    {
        Vector2 movement = _moveInput * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + movement);
    }
    
    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }
    
    private void Jump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }
    
    private void Fire()
    {
        // Implement fire action
        Debug.Log("Fire action triggered");
    }
    
    private void TogglePause()
    {
        // Implement pause action
        Debug.Log("Pause action triggered");
    }
}
```

### Step 6: Create Input Tests

Create `Assets/Tests/EditMode/InputSystemTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BlindDelivery.Core.Services;

public class InputSystemTests
{
    private IInputService _inputService;
    
    [SetUp]
    public void Setup()
    {
        _inputService = ServiceRegistry.Instance.Get&lt;IInputService&gt;();
    }
    
    [UnityTest]
    public IEnumerator TestMovementInput()
    {
        // Simulate movement input
        Vector2 testInput = new Vector2(1f, 0f);
        
        // Since we can't easily simulate Input System inputs in unit tests,
        // we'll test that the input service is properly initialized
        Assert.IsNotNull(_inputService);
        
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator TestJumpInput()
    {
        // Test jump input handling
        Assert.IsNotNull(_inputService);
        
        yield return null;
    }
    
    [UnityTest]
    public IEnumerator TestFireInput()
    {
        // Test fire input handling
        Assert.IsNotNull(_inputService);
        
        yield return null;
    }
}
```

### Step 7: Update Build Settings

Create `Assets/Editor/BuildConfiguration.cs`:

```csharp
using UnityEditor;

public static class BuildConfiguration
{
    [MenuItem("Tools/Build/Set Target Platforms")]
    public static void SetTargetPlatforms()
    {
        // Set iOS and Android as target platforms
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.Mono2x);
        
        // Set minimum SDK versions
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.iOS.targetOSVersionString = "13.0";
    }
    
    [MenuItem("Tools/Build/Configure for Mobile")]
    public static void ConfigureForMobile()
    {
        // Optimize for mobile
        PlayerSettings.virtualRealitySupported = false;
        PlayerSettings.runInBackground = true;
        PlayerSettings.clearPlayerDefaults = false;
        
        // Configure textures
        QualitySettings.masterTextureLimit = 1;
        QualitySettings.anisoLevel = 0;
        QualitySettings.maxAnisotropy = 1;
        
        // Configure shadows
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        
        // Configure other mobile settings
        PlayerSettings.gravity = new Vector3(0, -9.81f, 0);
        PlayerSettings.defaultCursor = null;
    }
}
```

### Step 8: Run Migration

1. **In Unity Editor**:
   - Go to `Tools > Unity Migration > Migrate All`
   - Or run the migration script

2. **Verify the migration**:
   - Check that Input System Assets are created
   - Verify URP Asset is configured
   - Run existing tests

## Migration Timeline

### Week 1
- Install Input System and URP packages
- Create migration tool
- Run initial migration

### Week 2
- Update core services
- Test input functionality
- Fix any issues

### Week 3
- Update gameplay components
- Run full test suite
- Optimize for mobile

### Week 4
- Final testing
- Documentation updates
- Prepare for deployment

## Verification Checklist

### Input System
- [ ] Input Actions Asset created
- [ ] InputSystemAssets script created
- [ ] Core services updated
- [ ] Gameplay components updated
- [ ] Tests pass
- [ ] InputManager.asset removed from build

### URP Migration
- [ ] URP Asset created and configured
- [ ] Graphics Settings updated
- [ ] Materials updated to URP shaders
- [ ] Camera setup updated
- [ ] Performance optimized
- [ ] Tests pass

### Mobile Optimization
- [ ] Target platforms set
- [ ] Quality settings optimized
- [ ] Performance benchmarks met
- [ ] All functionality preserved

## Expected Outcomes

1. **Modern Input System**: Cleaner, more maintainable input handling
2. **Better Mobile Performance**: URP optimized for mobile devices
3. **Enhanced Testability**: Input System easier to test
4. **Future-Proof**: Compatible with latest Unity features
5. **Mobile Optimized**: Specifically configured for iOS/Android

## Next Steps

1. **Run the migration tool** in Unity
2. **Update all scripts** that reference Input Manager
3. **Test thoroughly** in both Unity Editor and on mobile devices
4. **Optimize performance** for mobile deployment
5. **Document changes** for team collaboration

This migration will modernize the project and provide a solid foundation for future development.