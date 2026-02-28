using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessRunner
{
    public class InputRouter : MonoBehaviour
    {
        public event Action MoveLeft;
        public event Action MoveRight;
        public event Action TouchReleased;

        public float Horizontal { get; private set; }

#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputActionReference moveLeftAction;
        [SerializeField] private InputActionReference moveRightAction;
#endif
        [SerializeField] private bool useKeyboardFallback = true;
        [SerializeField] private bool useTouchInput = true;
        [SerializeField] private bool simulateTouchWithMouse = true;
        [SerializeField] private bool touchMoveByScreenPosition = true;
        [SerializeField] private bool touchMoveByScreenHalf = true;
        [SerializeField, Range(0f, 0.4f)] private float touchCenterDeadZone = 0.08f;
        [SerializeField] private float swipeHorizontalRange = 120f;
        [SerializeField] private bool scaleSwipeRangeWithScreenWidth = true;
        [SerializeField, Range(0.01f, 0.4f)] private float swipeRangeWidthRatio = 0.11f;
        [SerializeField, Min(1f)] private float minSwipeHorizontalRange = 90f;
        [SerializeField, Min(1f)] private float maxSwipeHorizontalRange = 220f;
        [Header("Debug")]
        [SerializeField] private bool showDebugOverlayInEditor = true;
        [SerializeField] private Vector2 debugOverlayOffset = new Vector2(20f, 80f);
        [SerializeField, Range(18, 64)] private int debugFontSize = 30;

        private Vector2 pointerStart;
        private bool pointerActive;
        private GUIStyle debugStyle;

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (moveLeftAction != null && moveLeftAction.action != null)
            {
                moveLeftAction.action.Enable();
            }

            if (moveRightAction != null && moveRightAction.action != null)
            {
                moveRightAction.action.Enable();
            }
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (moveLeftAction != null && moveLeftAction.action != null)
            {
                moveLeftAction.action.Disable();
            }

            if (moveRightAction != null && moveRightAction.action != null)
            {
                moveRightAction.action.Disable();
            }
#endif
        }

        private void Update()
        {
            if (useTouchInput && UpdateTouchInput())
            {
                return;
            }

            UpdateActionInput();
            if (useKeyboardFallback)
            {
                UpdateKeyboardInput();
            }
        }

        private void UpdateActionInput()
        {
#if ENABLE_INPUT_SYSTEM
            float axis = 0f;
            bool hasAxis = false;

            if (moveLeftAction != null && moveLeftAction.action != null)
            {
                if (moveLeftAction.action.IsPressed())
                {
                    axis -= 1f;
                }

                hasAxis = true;
            }

            if (moveRightAction != null && moveRightAction.action != null)
            {
                if (moveRightAction.action.IsPressed())
                {
                    axis += 1f;
                }

                hasAxis = true;
            }

            if (hasAxis)
            {
                Horizontal = Mathf.Clamp(axis, -1f, 1f);
            }
            else if (!useKeyboardFallback)
            {
                Horizontal = 0f;
            }
#endif
        }

        private void UpdateKeyboardInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return;
            }

            float axis = 0f;
            bool leftPressed = Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;
            bool rightPressed = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;

            if (leftPressed)
            {
                axis -= 1f;
            }

            if (rightPressed)
            {
                axis += 1f;
            }

            Horizontal = Mathf.Clamp(axis, -1f, 1f);

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                MoveLeft?.Invoke();
            }

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                MoveRight?.Invoke();
            }

#else
            float axis = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                axis -= 1f;
            }

            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                axis += 1f;
            }

            Horizontal = Mathf.Clamp(axis, -1f, 1f);

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                MoveLeft?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                MoveRight?.Invoke();
            }

#endif
        }

        private bool UpdateTouchInput()
        {
            if (TryUpdatePointer(out Vector2 position, out bool isPressed))
            {
                if (isPressed)
                {
                    if (!pointerActive)
                    {
                        pointerActive = true;
                        pointerStart = position;
                    }

                    Horizontal = GetTouchMoveAxis(position);

                    return true;
                }

                if (pointerActive)
                {
                    pointerActive = false;
                    Horizontal = 0f;
                    TouchReleased?.Invoke();
                }
                else
                {
                    Horizontal = 0f;
                }

                return true;
            }

            if (pointerActive)
            {
                pointerActive = false;
                Horizontal = 0f;
                TouchReleased?.Invoke();
                return true;
            }

            return false;
        }

        private bool TryUpdatePointer(out Vector2 position, out bool isPressed)
        {
            position = Vector2.zero;
            isPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                isPressed = touch.press.isPressed;
                position = touch.position.ReadValue();

                // Only consume input when touch is active (or we need to emit release),
                // otherwise allow mouse simulation in Editor.
                if (isPressed || pointerActive)
                {
                    return true;
                }
            }

            if (simulateTouchWithMouse && Mouse.current != null)
            {
                isPressed = Mouse.current.leftButton.isPressed;
                position = Mouse.current.position.ReadValue();
                return isPressed || pointerActive;
            }

            return false;
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                position = touch.position;
                isPressed = touch.phase != UnityEngine.TouchPhase.Canceled && touch.phase != UnityEngine.TouchPhase.Ended;
                return true;
            }

            if (simulateTouchWithMouse && Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
                isPressed = true;
                return true;
            }

            return false;
#endif
        }

        private float GetTouchMoveAxis(Vector2 position)
        {
            if (touchMoveByScreenPosition)
            {
                float width = Mathf.Max(1f, Screen.width);
                float centered = Mathf.Clamp01(position.x / width) - 0.5f;
                float deadZone = Mathf.Clamp(touchCenterDeadZone, 0f, 0.49f);
                float absCentered = Mathf.Abs(centered);
                if (absCentered <= deadZone)
                {
                    return 0f;
                }

                // Map touch X to a smooth axis [-1, 1], so movement speed follows finger position.
                float magnitude = (absCentered - deadZone) / Mathf.Max(0.0001f, 0.5f - deadZone);
                return Mathf.Sign(centered) * Mathf.Clamp01(magnitude);
            }

            if (touchMoveByScreenHalf)
            {
                float width = Mathf.Max(1f, Screen.width);
                float normalized = position.x / width - 0.5f;
                if (Mathf.Abs(normalized) <= touchCenterDeadZone)
                {
                    return 0f;
                }

                return normalized > 0f ? 1f : -1f;
            }

            float swipeRange = GetEffectiveSwipeHorizontalRange();
            float horizontal = (position.x - pointerStart.x) / swipeRange;
            return Mathf.Clamp(horizontal, -1f, 1f);
        }

        private float GetEffectiveSwipeHorizontalRange()
        {
            if (!scaleSwipeRangeWithScreenWidth)
            {
                return Mathf.Max(1f, swipeHorizontalRange);
            }

            float baseRange = Mathf.Max(1f, Screen.width * swipeRangeWidthRatio);
            float minRange = Mathf.Max(1f, minSwipeHorizontalRange);
            float maxRange = Mathf.Max(minRange, maxSwipeHorizontalRange);
            return Mathf.Clamp(baseRange, minRange, maxRange);
        }

        private void OnGUI()
        {
#if UNITY_EDITOR
            if (!showDebugOverlayInEditor)
            {
                return;
            }

            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = debugFontSize,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white }
                };
                debugStyle.padding = new RectOffset(16, 16, 8, 8);
            }

            debugStyle.fontSize = debugFontSize;
            Rect box = new Rect(debugOverlayOffset.x, debugOverlayOffset.y, 420f, 54f);
            string text = $"Input Horizontal: {Horizontal:F2}  |  TouchActive: {pointerActive}";
            GUI.Box(box, text, debugStyle);
#endif
        }

    }
}
