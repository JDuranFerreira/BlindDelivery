using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Template.Core.Input;
using Template.Core.Services;

namespace Template.Services.Input
{
    /// <summary>
    /// Wraps the new Input System into the IInputService seam. Pure EditorMode-testable logic
    /// (threshold math, UI filtering) lives in InputProcessor; this class handles the
    /// InputActionAsset binding and platform quirks.
    /// </summary>
    [AddComponentMenu("Template/Input/Input Service")]
    public sealed class InputService : MonoBehaviour, IInputService
    {
        public const string ServiceName = "Input";

        [Header("Input Actions")]
        [Tooltip("Input Action Asset with Tap, Hold, Swipe, Drag, Pinch actions.")]
        public InputActionAsset inputActions;

        [Header("Thresholds (auto-scaled by DPI at runtime)")]
        [SerializeField, Min(0f)] float tapMaxDurationSeconds = 0.2f;
        [SerializeField, Min(0f)] float holdThresholdSeconds = 0.5f;
        [SerializeField, Min(0f)] float swipeMinDistancePixels = 50f;
        [SerializeField, Min(0f)] float dragThresholdPixels = 10f;
        [SerializeField, Min(0f)] float pinchThreshold = 0.05f;

        InputAction tapAction;
        InputAction holdAction;
        InputAction swipeAction;
        InputAction dragAction;
        InputAction pinchAction;
        InputAction pointerPositionAction;
        InputAction pointerPressAction;

        Template.Core.Input.Vector2? lastPointerPosition;
        Template.Core.Input.Vector2? pressStartPosition;
        float pressStartTime;
        float lastTapTime;
        Template.Core.Input.Vector2? lastTapPosition;
        bool isPointerDown;
        int touchCount;

        // DPI-scaled thresholds (set in InitializeAsync)
        float scaledSwipeMinDistance;
        float scaledDragThreshold;

        public string Name => ServiceName;

        public event Action<Template.Core.Input.Vector2> OnTap;
        public event Action<Template.Core.Input.Vector2> OnDoubleTap;
        public event Action<Template.Core.Input.Vector2> OnHold;
        public event Action<Template.Core.Input.Vector2> OnSwipe;
        public event Action<Template.Core.Input.Vector2> OnDrag;
        public event Action<float> OnPinch;

        public Template.Core.Input.Vector2? PointerPosition => lastPointerPosition;
        public bool IsPointerDown => isPointerDown;

        public async Task InitializeAsync(CancellationToken token = default)
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"[{ServiceName}] No InputActionAsset assigned — input will not work.");
                return;
            }

            // Scale thresholds by DPI so gestures feel the same on all devices
            float dpiScale = Screen.dpi > 0f ? 160f / Screen.dpi : 1f; // 160 dpi baseline
            scaledSwipeMinDistance = swipeMinDistancePixels * dpiScale;
            scaledDragThreshold = dragThresholdPixels * dpiScale;

            // Find actions by name (Input System convention)
            tapAction = inputActions.FindAction("Tap");
            holdAction = inputActions.FindAction("Hold");
            swipeAction = inputActions.FindAction("Swipe");
            dragAction = inputActions.FindAction("Drag");
            pinchAction = inputActions.FindAction("Pinch");
            pointerPositionAction = inputActions.FindAction("PointerPosition");
            pointerPressAction = inputActions.FindAction("PointerPress");

            // Enable all
            inputActions.Enable();

            // Subscribe
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

            await Task.Yield(); // keep signature async for consistency
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

        void Update()
        {
            // Poll pinch (continuous) and touch count for multi-touch awareness
            touchCount = Touchscreen.current?.touches.Count ?? 0;
        }

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
            UnityEngine.Vector2 pos = ctx.ReadValue<UnityEngine.Vector2>();
            lastPointerPosition = new Template.Core.Input.Vector2(pos.x, pos.y);
        }

        void OnTapPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;

            Template.Core.Input.Vector2 pos = lastPointerPosition ?? Template.Core.Input.Vector2.Zero;
            float now = Time.unscaledTime;

            // Double-tap detection
            if (lastTapPosition.HasValue &&
                now - lastTapTime <= tapMaxDurationSeconds * 2f &&
                (pos - lastTapPosition.Value).Magnitude <= scaledDragThreshold)
            {
                OnDoubleTap?.Invoke(pos);
                lastTapTime = 0f; // consume
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
                OnHold?.Invoke(lastPointerPosition ?? Template.Core.Input.Vector2.Zero);
            }
        }

        void OnSwipePerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;

            UnityEngine.Vector2 delta = ctx.ReadValue<UnityEngine.Vector2>();
            if (delta.magnitude >= scaledSwipeMinDistance)
            {
                OnSwipe?.Invoke(new Template.Core.Input.Vector2(delta.x, delta.y).Normalized);
            }
        }

        void OnDragPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;

            UnityEngine.Vector2 delta = ctx.ReadValue<UnityEngine.Vector2>();
            if (delta.magnitude >= scaledDragThreshold)
            {
                OnDrag?.Invoke(new Template.Core.Input.Vector2(delta.x, delta.y));
            }
        }

        void OnPinchPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;

            float scale = ctx.ReadValue<float>();
            if (Math.Abs(scale - 1f) >= pinchThreshold)
            {
                OnPinch?.Invoke(scale);
            }
        }

        bool IsOverUI()
        {
            // New Input System UI check
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}