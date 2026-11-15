#!/bin/bash

# Unity Project Migration Script
# This script executes the Unity project migration for Blind Delivery

cd "$(dirname "$0")"

# Set this to true to execute Unity commands
UNITY_AVAILABLE=true

# Function to log messages
function log_message {
    echo "=== $1 ==="
}

# Function to check if a file exists
function file_exists {
    local path=$1
    if [ -f "$path" ]; then
        return 0
    else
        return 1
    fi
}

# Function to check if a directory exists
function directory_exists {
    local path=$1
    if [ -d "$path" ]; then
        return 0
    else
        return 1
    fi
}

# Create directories
function create_directories {
    log_message "Creating Directories"
    
    mkdir -p "Assets/InputSystem"
    mkdir -p "Assets/URP"
    mkdir -p "Assets/Editor"
}

# Create Input Actions division
function create_input_actions {
    log_message "Creating Input Actions Division"
    
    # Create Input Actions asset
    cat > "Assets/InputSystem/InputActions.asset" << 'EOF'
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
EOF

    # Create InputActions.cs script
    cat > "Assets/InputSystem/InputActions.cs" << 'EOF'
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
        Asset = Resources.Load<InputActionAsset>("InputActions");
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
        return gameplayMap?["Move"].ReadValue<Vector2>() ?? Vector2.zero;
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
        return gameplayMap?["Look"].ReadValue<Vector2>() ?? Vector2.zero;
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
EOF

    echo "✅ Input Actions division created"
}

# Create URP division
function create_urp_division {
    log_message "Creating URP Division"
    
    # Create URP asset
    cat > "Assets/URP/UniversalRenderPipelineAsset.asset" << 'EOF'
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
EOF

    echo "✅ URP division created"
}

# Update Input Service
function update_input_service {
    log_message "Updating Input Service"
    
    if file_exists "Assets/Scripts/Services/Input/InputService.cs"; then
        echo "Updating InputService.cs..."
        
        # Create a backup
        cp "Assets/Scripts/Services/Input/InputService.cs" "Assets/Scripts/Services/Input/InputService.cs.backup"
        
        # Update the file
        cat > "Assets/Scripts/Services/Input/InputService.cs" << 'EOF'
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using BlindDelivery.Core.Input;
using BlindDelivery.Core.Services;

namespace BlindDelivery.Services.Input
{
    [AddComponentMenu("BlindDelivery/Input/Input Service")]
    public sealed class InputService : MonoBehaviour, IInputService
    {
        public const string ServiceName = "Input";
        
        public InputActionAsset inputActions;
        
        // Keep existing gesture control variables
        private InputAction tapAction;
        private InputAction holdAction;
        private InputAction swipeAction;
        private InputAction dragAction;
        private InputAction pinchAction;
        private InputAction pointerPositionAction;
        private InputAction pointerPressAction;
        
        // Add new input actions variables
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction fireAction;
        private InputAction lookAction;
        private InputAction pauseAction;
        
        private Vector2? lastPointerPosition;
        private Vector2? pressStartPosition;
        private float pressStartTime;
        private float lastTapTime;
        private Vector2? lastTapPosition;
        private bool isPointerDown;
        private int touchCount;
        
        float scaledSwipeMinDistance;
        float scaledDragThreshold;
        
        public string Name => ServiceName;
        
        public event Action<Vector2> OnTap;
        public event Action<Vector2> OnDoubleTap;
        public event Action<Vector2> OnHold;
        public event Action<Vector2> OnSwipe;
        public event Action<Vector2> OnDrag;
        public event Action<float> OnPinch;
        
        public Vector2? PointerPosition => lastPointerPosition;
        public bool IsPointerDown => isPointerDown;
        
        public async Task InitializeAsync(CancellationToken token = default)
        {
            // Load existing gesture input actions
            if (inputActions == null)
            {
                Debug.LogWarning($"[{ServiceName}] No InputActionAsset assigned — input will not work.");
                return;
            }
            
            float dpiScale = Screen.dpi > 0f ? 160f / Screen.dpi : 1f;
            scaledSwipeMinDistance = 50f * dpiScale;
            scaledDragThreshold = 10f * dpiScale;
            
            // Find existing gesture actions
            tapAction = inputActions.FindAction("Tap");
            holdAction = inputActions.FindAction("Hold");
            swipeAction = inputActions.FindAction("Swipe");
            dragAction = inputActions.FindAction("Drag");
            pinchAction = inputActions.FindAction("Pinch");
            pointerPositionAction = inputActions.FindAction("PointerPosition");
            pointerPressAction = inputActions.FindAction("PointerPress");
            
            // Find new gameplay actions
            moveAction = inputActions.FindAction("Move");
            jumpAction = inputActions.FindAction("Jump");
            fireAction = inputActions.FindAction("Fire");
            lookAction = inputActions.FindAction("Look");
            pauseAction = inputActions.FindAction("Pause");
            
            inputActions.Enable();
            
            // Subscribe to gesture actions
            if (tapAction != null) tapAction.performed += OnTapPerformed;
            if (holdAction != null) holdAction.performed += OnHoldPerformed;
            if (swipeAction != null) swipeAction.performed += OnSwipePerformed;
            if (dragAction != null) dragAction.performed += OnDragPerformed;
            if (pinchAction != null) pinchAction.performed += OnPinchPerformed;
            if (pointerPositionAction != null) pointerPositionAction.performed += OnPointerPosition;
            if (pointerPressAction != null)
            {
                pointerPressAction.started += OnPointerPressStarted;
                pointerPressAction.canceled += OnPointerPressCanceled;
            }
            
            await Task.Yield();
        }
        
        void OnDestroy()
        {
            if (inputActions != null)
            {
                if (tapAction != null) tapAction.performed -= OnTapPerformed;
                if (holdAction != null) holdAction.performed -= OnHoldPerformed;
                if (swipeAction != null) swipeAction.performed -= OnSwipePerformed;
                if (dragAction != null) dragAction.performed -= OnDragPerformed;
                if (pinchAction != null) pinchAction.performed -= OnPinchPerformed;
                if (pointerPositionAction != null) pointerPositionAction.performed -= OnPointerPosition;
                if (pointerPressAction != null)
                {
                    pointerPressAction.started -= OnPointerPressStarted;
                    pointerPressAction.canceled -= OnPointerPressCanceled;
                }
                inputActions.Disable();
            }
        }
        
        // New input methods using InputActions
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
        
        // Existing gesture methods
        void OnPointerPressStarted(InputAction.CallbackContext ctx)
        {
            isPointerDown = true;
            pressStartPosition = lastPointerPosition;
            pressStartTime = Time.unscaledTime;
        }
        
        void OnPointerPressCanceled(InputAction.CallbackContext ctx)
        {
            isPointerDown = false;
            pressStartPosition = null;
        }
        
        void OnPointerPosition(InputAction.CallbackContext ctx)
        {
            Vector2 pos = ctx.ReadValue<Vector2>();
            lastPointerPosition = new Vector2(pos.x, pos.y);
        }
        
        void OnTapPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;
            
            Vector2 pos = lastPointerPosition ?? Vector2.zero;
            float now = Time.unscaledTime;
            
            if (lastTapPosition.HasValue &&
                now - lastTapTime <= 0.4f &&
                (pos - lastTapPosition.Value).magnitude <= scaledDragThreshold)
            {
                OnDoubleTap?.Invoke(pos);
                lastTapTime = 0f;
                lastTapPosition = null;
                return;
            }
            
            lastTapTime = now;
            lastTapPosition = pos;
            OnTap?.Invoke(pos);
        }
        
        void OnHoldPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;
            if (ctx.phase == InputActionPhase.Started)
            {
                OnHold?.Invoke(lastPointerPosition ?? Vector2.zero);
            }
        }
        
        void OnSwipePerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;
            
            Vector2 delta = ctx.ReadValue<Vector2>();
            if (delta.magnitude >= scaledSwipeMinDistance)
            {
                OnSwipe?.Invoke(new Vector2(delta.x, delta.y).normalized);
            }
        }
        
        void OnDragPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;
            
            Vector2 delta = ctx.ReadValue<Vector2>();
            if (delta.magnitude >= scaledDragThreshold)
            {
                OnDrag?.Invoke(new Vector2(delta.x, delta.y));
            }
        }
        
        void OnPinchPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;
            
            float scale = ctx.ReadValue<float>();
            if (Mathf.Abs(scale - 1f) >= 0.05f)
            {
                OnPinch?.Invoke(scale);
            }
        }
        
        bool IsOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
EOF
        
        echo "✅ InputService.cs updated"
    else
        echo "❌ InputService.cs not found"
    fi
}

# Update Gameplay Components
function update_gameplay_components {
    log_message "Updating Gameplay Components"
    
    # Update BoyController.cs
    if file_exists "Assets/Scripts/Gameplay/BoyController.cs"; then
        echo "Updating BoyController.cs..."
        
        cp "Assets/Scripts/Gameplay/BoyController.cs" "Assets/Scripts/Gameplay/BoyController.cs.backup"
        
        cat > "Assets/Scripts/Gameplay/BoyController.cs" << 'EOF'
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
    public LayerMask groundLayer;
    
    void Start()
    {
        inputService = ServiceRegistry.Instance.Get<IInputService>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        moveInput = InputActions.GetMoveInput();
        
        if (InputActions.GetJumpInputDown() && IsGrounded())
        {
            Jump();
        }
        
        if (InputActions.GetFireInputDown())
        {
            Fire();
        }
        
        if (InputActions.GetPauseInputDown())
        {
            TogglePause();
        }
    }
    
    private void HandleAnimation()
    {
        spriteRenderer.flipX = moveInput.x < 0;
    }
    
    private void Move()
    {
        Vector2 movement = moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
    
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(transform.position, 0.2f, groundLayer);
    }
    
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }
    
    private void Fire()
    {
        Debug.Log("Fire action triggered");
    }
    
    private void TogglePause()
    {
        Debug.Log("Pause action triggered");
    }
}
EOF
        
        echo "✅ BoyController.cs updated"
    else
        echo "❌ BoyController.cs not found"
    fi
}

# Update Graphics Settings
function update_graphics_settings {
    log_message "Updating Graphics Settings"
    
    if file_exists "ProjectSettings/GraphicsSettings.asset"; then
        echo "Updating GraphicsSettings.asset..."
        
        # Use sed to update the file
        sed -i 's/m_DefaultRenderingPath: 1/m_DefaultRenderingPath: 2/' "ProjectSettings/GraphicsSettings.asset"
        sed -i 's/m_DefaultMobileRenderingPath: 1/m_DefaultMobileRenderingPath: 2/' "ProjectSettings/GraphicsSettings.asset"
        
        echo "✅ GraphicsSettings.asset updated"
    else
        echo "❌ GraphicsSettings.asset not found"
    fi
}

# Run validation
function run_validation {
    log_message "Running Validation"
    
    echo "Checking migration status..."
    
    # Check Input Actions division
    if directory_exists "Assets/InputSystem"; then
        echo "✅ InputSystem division created"
    else
        echo "❌ InputSystem division missing"
    fi
    
    # Check URP division
    if directory_exists "Assets/URP"; then
        echo "✅ URP division created"
    else
        echo "❌ URP division missing"
    fi
    
    # Check Input Actions asset
    if file_exists "Assets/InputSystem/InputActions.asset"; then
        echo "✅ InputActions.asset created"
    else
        echo "❌ InputActions.asset missing"
    fi
    
    # Check InputActions.cs script
    if file_exists "Assets/InputSystem/InputActions.cs"; then
        echo "✅ InputActions.cs script created"
    else
        echo "❌ InputActions.cs script missing"
    fi
    
    # Check URP asset
    if file_exists "Assets/URP/UniversalRenderPipelineAsset.asset"; then
        echo "✅ UniversalRenderPipelineAsset.asset created"
    else
        echo "❌ UniversalRenderPipelineAsset.asset missing"
    fi
    
    # Check updated InputService
    if file_exists "Assets/Scripts/Services/Input/InputService.cs"; then
        if grep -q "InputActions.GetMoveInput" "Assets/Scripts/Services/Input/InputService.cs"; then
            echo "✅ InputService updated with InputActions"
        else
            echo "❌ InputService not updated with InputActions"
        fi
    else
        echo "❌ InputService.cs not found"
    fi
    
    # Check updated BoyController
    if file_exists "Assets/Scripts/Gameplay/BoyController.cs"; then
        if grep -q "InputActions.GetMoveInput" "Assets/Scripts/Gameplay/BoyController.cs"; then
            echo "✅ BoyController updated with InputActions"
        else
            echo "❌ BoyController not updated with InputActions"
        fi
    else
        echo "❌ BoyController.cs not found"
    fi
    
    echo "\n=== Migration Validation Complete ==="
}

# Main execution
function main {
    echo "=== Blind Delivery Unity Project Migration ==="
    echo "Starting migration process...\n"
    
    create_directories
    create_input_actions
    create_urp_division
    update_input_service
    update_gameplay_components
    update_graphics_settings
    
    echo "\n=== Migration Complete ==="
    echo "\nNext Steps:"
    echo "1. Open Unity project"
    echo "2. Go to Tools > Unity Migration > Migrate All (if available)"
    echo "3. Run validation: Tools > Validation > Validate Migration"
    echo "4. Test gameplay functionality"
    echo "5. Test rendering pipeline"
    echo "6. Optimize for mobile deployment"
    
    echo "\nFor manual migration, see MIGRATION_EXECUTION.md"
}

# Execute main function
main