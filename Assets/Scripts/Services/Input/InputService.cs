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
    /// <summary>
    /// Wraps the new Input System into the IInputService seam. Pure EditorMode-testable logic
    /// (threshold math, UI filtering) lives in InputProcessor; this class handles the
    /// InputActionAsset binding and platform quirks.
    /// </summary>
    [AddComponentMenu("BlindDelivery/Input/Input Service")]
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

        InputAction pinchAction;
        InputAction pointerPositionAction;
        InputAction pointerPressAction;

        BlindDelivery.Core.Input.Vector2? lastPointerPosition;
        BlindDelivery.Core.Input.Vector2? pressStartPosition;
        float pressStartTime;
        float lastTapTime;
        BlindDelivery.Core.Input.Vector2? lastTapPosition;
        bool isPointerDown;
        bool wasPointerDown;
        bool holdRaised;
        BlindDelivery.Core.Input.Vector2? lastDragPosition;
        int touchCount;
        bool overUiLastFrame;

        // DPI-scaled thresholds (set in InitializeAsync)
        float scaledSwipeMinDistance;
        float scaledDragThreshold;

        public string Name => ServiceName;

        public event Action<BlindDelivery.Core.Input.Vector2> OnTap;
        public event Action<BlindDelivery.Core.Input.Vector2> OnDoubleTap;
        public event Action<BlindDelivery.Core.Input.Vector2> OnHold;
        public event Action<BlindDelivery.Core.Input.Vector2> OnSwipe;
        public event Action<BlindDelivery.Core.Input.Vector2> OnDrag;
        public event Action<float> OnPinch;

        public BlindDelivery.Core.Input.Vector2? PointerPosition => lastPointerPosition;
        public bool IsPointerDown => isPointerDown;

        public async Task InitializeAsync(CancellationToken token = default)
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"[{ServiceName}] No InputActionAsset assigned â€” input will not work.");
                return;
            }

            // Scale thresholds by DPI so gestures feel the same on all devices
            float dpiScale = Screen.dpi > 0f ? 160f / Screen.dpi : 1f; // 160 dpi baseline
            scaledSwipeMinDistance = swipeMinDistancePixels * dpiScale;
            scaledDragThreshold = dragThresholdPixels * dpiScale;

            // Find actions by name (Input System convention)
            pinchAction = inputActions.FindAction("Pinch");
            pointerPositionAction = inputActions.FindAction("PointerPosition");
            pointerPressAction = inputActions.FindAction("PointerPress");

            // Enable all
            inputActions.Enable();

            // Tap/Hold/Drag/Swipe are derived from press + position in Update. Interaction-based
            // actions are device-sensitive (mouse vs. simulator touch), so the same gesture logic
            // must run for every pointer device. Pinch still comes from its own action.
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

            // EventSystem must be queried on the main frame, never from inside an InputAction
            // callback (it would read stale UI state and spam a warning).
            overUiLastFrame = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            bool down = isPointerDown;
            if (down && !wasPointerDown)
            {
                pressStartTime = Time.unscaledTime;
                pressStartPosition = lastPointerPosition;
                lastDragPosition = lastPointerPosition;
                holdRaised = false;
            }

            if (down)
            {
                float held = Time.unscaledTime - pressStartTime;
                BlindDelivery.Core.Input.Vector2 position = lastPointerPosition ?? BlindDelivery.Core.Input.Vector2.Zero;

                if (!holdRaised && held >= holdThresholdSeconds && !IsOverUI())
                {
                    holdRaised = true;
                    OnHold?.Invoke(position);
                }

                if (pressStartPosition.HasValue && lastPointerPosition.HasValue &&
                    (position - pressStartPosition.Value).Magnitude >= scaledDragThreshold &&
                    lastDragPosition.HasValue && !IsOverUI())
                {
                    BlindDelivery.Core.Input.Vector2 delta = position - lastDragPosition.Value;
                    if (delta.Magnitude > 0f) OnDrag?.Invoke(delta);
                }

                lastDragPosition = lastPointerPosition;
            }
            else if (wasPointerDown)
            {
                float held = Time.unscaledTime - pressStartTime;
                BlindDelivery.Core.Input.Vector2 position = lastPointerPosition ?? BlindDelivery.Core.Input.Vector2.Zero;

                if (!IsOverUI())
                {
                    if (!holdRaised && held <= tapMaxDurationSeconds &&
                        (!pressStartPosition.HasValue ||
                         (position - pressStartPosition.Value).Magnitude <= scaledDragThreshold))
                    {
                        RaiseTap(position);
                    }
                    else if (pressStartPosition.HasValue &&
                             (position - pressStartPosition.Value).Magnitude >= scaledSwipeMinDistance)
                    {
                        OnSwipe?.Invoke((position - pressStartPosition.Value).Normalized);
                    }
                }

                holdRaised = false;
                pressStartPosition = null;
                lastDragPosition = null;
            }

            wasPointerDown = down;
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
            lastPointerPosition = new BlindDelivery.Core.Input.Vector2(pos.x, pos.y);
        }

        void RaiseTap(BlindDelivery.Core.Input.Vector2 pos)
        {
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

        void OnPinchPerformed(InputAction.CallbackContext ctx)
        {
            if (IsOverUI()) return;

            // Pinch is bound to two touch positions, so its value type depends on how
            // the asset is authored: a Scale processor yields a scalar (1 = no change),
            // while raw position bindings yield a Vector2. Read defensively so a
            // mis-authored asset degrades to "no pinch" instead of throwing
            // InvalidOperationException from ReadValue<float>.
            float scale;
            if (ctx.control != null && ctx.control.valueType == typeof(float))
            {
                scale = ctx.ReadValue<float>();
            }
            else
            {
                UnityEngine.Vector2 raw = ctx.ReadValue<UnityEngine.Vector2>();
                scale = 1f + raw.magnitude;
            }
            if (Math.Abs(scale - 1f) >= pinchThreshold)
            {
                OnPinch?.Invoke(scale);
            }
        }

        bool IsOverUI() => overUiLastFrame;
    }
}

