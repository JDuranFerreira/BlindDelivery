# Input System Migration Guide for Blind Delivery Project

## Issue Summary
The project currently uses the deprecated **Input Manager** which is being phased out in favor of the **Input System Package**.

## Current Status
- ✅ **Project Structure**: Unity project with multiple assemblies
- ❌ **Input System**: Using legacy Input Manager (`ProjectSettings/InputManager.asset`)
- 📋 **Action Required**: Migration to new Input System

## Migration Steps

### Step 1: Install Input System Package
```bash
# In Unity Project window
Assets > Input System > Install Input System Package

# Or via Package Manager
Edit > Project Settings > Package Manager
- Click "+" > Add package from git URL
- URL: "com.unity.inputsystem"
```

### Step 2: Update Input Actions
Create `Assets/InputSystem/InputActions.asset`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<InputActionAsset xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.InputSystem">
    <InputActionMap name="Gameplay">
        <InputAction name="Move" type="Value" binding="&amp;lt;Keyboard&amp;gt;/wasd"/>
        <InputAction name="Jump" type="Button" binding="&amp;lt;Keyboard&amp;gt;/space"/>
        <InputAction name="Fire" type="Button" binding="&amp;lt;Keyboard&amp;gt;/mouse0"/>
        <InputAction name="Look" type="Value" binding="&amp;lt;Mouse&amp;gt;/delta"/>
        <InputAction name="Pause" type="Button" binding="&amp;lt;Keyboard&amp;gt;/escape"/>
    </InputActionMap>
    <InputActionMap name="UI">
        <InputAction name="Navigate" type="Value" binding="&amp;lt;Keyboard&amp;gt;/arrowKeys"/>
        <InputAction name="Submit" type="Button" binding="&amp;lt;Keyboard&amp;gt;/enter"/>
        <InputAction name="Cancel" type="Button" binding="&amp;lt;Keyboard&amp;gt;/escape"/>
    </InputActionMap>
</InputActionAsset>
```

### Step 3: Create Input System Assets Script
Create `Assets/InputSystem/InputSystemAssets.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputSystemAssets
{
    public static PlayerInputActions PlayerActions { get; private set; }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        PlayerActions = new PlayerInputActions();
        PlayerActions.Enable();
    }
}
```

### Step 4: Update Existing Input References

Replace all Input Manager usage with Input System:

**Before (Input Manager):**
```csharp
void Update()
{
    float moveInput = Input.GetAxis("Horizontal");
    if (Input.GetKeyDown("space")) { Jump(); }
    if (Input.GetMouseButtonDown(0)) { Fire(); }
}
```

**After (Input System):**
```csharp
void Update()
{
    float moveInput = InputSystemAssets.PlayerActions.Gameplay.Move.ReadValue&lt;float&gt;();
    if (InputSystemAssets.PlayerActions.Gameplay.Jump.WasPressedThisFrame()) { Jump(); }
    if (InputSystemAssets.PlayerActions.Gameplay.Fire.WasPressedThisFrame()) { Fire(); }
}
```

### Step 5: Migration Script
Create `Assets/Editor/InputSystemMigrationTool.cs`:

```csharp
using UnityEditor;
using UnityEngine.InputSystem;

public static class InputSystemMigrationTool
{
    [MenuItem("Tools/Input System/Migrate Blind Delivery")]
    public static void MigrateProject()
    {
        // Create Input Actions asset if not exists
        string inputActionsPath = "Assets/InputSystem/InputActions.asset";
        if (!System.IO.File.Exists(inputActionsPath))
        {
            InputActionAsset asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
            AssetDatabase.CreateAsset(asset, inputActionsPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created Input Actions asset at: " + inputActionsPath);
        }
        
        // Create InputSystemAssets script if not exists
        string scriptPath = "Assets/InputSystem/InputSystemAssets.cs";
        if (!System.IO.File.Exists(scriptPath))
        {
            string scriptContent = @"using UnityEngine;
using UnityEngine.InputSystem;

public static class InputSystemAssets
{
    public static PlayerInputActions PlayerActions { get; private set; }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        PlayerActions = new PlayerInputActions();
        PlayerActions.Enable();
    }
}";
            System.IO.File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.Refresh();
            Debug.Log("Created InputSystemAssets script at: " + scriptPath);
        }
        
        Debug.Log("Input System migration completed for Blind Delivery!");
    }
}
```

### Step 6: Update Project Settings

1. **Create Input Provider** in `Assets/InputSystem/`:
   - `InputActions.asset` - Main input actions
   - `InputSystemAssets.cs` - Runtime accessor

2. **Remove InputManager.asset** reference:
   - Delete or backup the old `InputManager.asset`
   - Unity will create a new `PlayerInput.prefab` reference

## Testing the Migration

### Unit Tests
Update existing input tests to use Input System:

```csharp
[UnityTest]
public IEnumerator TestInputSystem()
{
    // Enable input actions
    InputSystemAssets.PlayerActions.Enable();
    
    // Test move input
    InputSystemAssets.PlayerActions.Gameplay.Move.WriteValue(1.0f);
    float moveValue = InputSystemAssets.PlayerActions.Gameplay.Move.ReadValue<float>();
    Assert.AreEqual(1.0f, moveValue);
    
    yield return null;
}
```

### Integration Tests
```csharp
[UnityTest]
public IEnumerator TestGameplayInput()
{
    // Simulate player input
    InputSystemAssets.PlayerActions.Gameplay.Jump.Trigger();
    
    // Verify game state changes
    yield return new WaitForSeconds(0.1f);
    
    // Assert jump was processed
}
```

## Required Code Changes

### Core Services
Update `BlindDelivery.Services.Input` to use Input System:

```csharp
using UnityEngine.InputSystem;

public class InputService : IInputService
{
    private PlayerInputActions _inputActions;
    
    public InputService()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
    }
    
    public Vector2 GetMovementInput()
    {
        return _inputActions.Gameplay.Move.ReadValue<Vector2>();
    }
    
    public bool GetJumpInput()
    {
        return _inputActions.Gameplay.Jump.IsPressed();
    }
    
    public bool GetFireInput()
    {
        return _inputActions.Gameplay.Fire.WasPressedThisFrame();
    }
}
```

## Expected Timeline

1. **Week 1**: Install Input System, create Input Actions asset
2. **Week 2**: Update core services and test basic functionality  
3. **Week 3**: Migrate all gameplay scripts and test integration
4. **Week 4**: Update tests and resolve any issues

## Verification Checklist

- [ ] Input System package installed
- [ ] Input Actions asset created and configured
- [ ] All existing input references updated
- [ ] Runtime tests pass
- [ ] Integration tests updated
- [ ] Input Manager removed from build settings
- [ ] Mobile compatibility verified

## Benefits of Migration

1. **Modern API**: Cleaner, more intuitive input handling
2. **Better Testing**: Easier to mock and test inputs
3. **Mobile Support**: Native mobile touch and gesture support
4. **Unity Integration**: Better with new Unity features
5. **Future-Proof**: Ongoing development direction of Unity

## Next Steps

1. Run the migration tool
2. Update all input-related scripts
3. Test the changes thoroughly
4. Update documentation
5. Commit the changes

This migration will make the project more maintainable and future-proof while maintaining all existing input functionality.