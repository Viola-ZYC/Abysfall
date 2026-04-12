using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UGUISelectable = UnityEngine.UI.Selectable;
using UITKButton = UnityEngine.UIElements.Button;
using UITKDropdownField = UnityEngine.UIElements.DropdownField;
using UITKScroller = UnityEngine.UIElements.Scroller;
using UITKScrollView = UnityEngine.UIElements.ScrollView;
using UITKSlider = UnityEngine.UIElements.Slider;

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
        private bool uiPointerActive;
        private int activeTouchId = -1;
        private bool activePointerUsesMouse;
        private GUIStyle debugStyle;
        private PointerEventData uiPointerEventData;
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

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
            if (TryGetUiBlockingPointer(out _))
            {
                pointerActive = false;
                activeTouchId = -1;
                activePointerUsesMouse = false;
                uiPointerActive = true;
                Horizontal = 0f;
                return true;
            }

            if (TryGetMovementPointer(out Vector2 position, out int touchId, out bool usesMouse))
            {
                uiPointerActive = false;

                if (!pointerActive || activeTouchId != touchId || activePointerUsesMouse != usesMouse)
                {
                    pointerActive = true;
                    activeTouchId = touchId;
                    activePointerUsesMouse = usesMouse;
                    pointerStart = position;
                }

                Horizontal = GetTouchMoveAxis(position);
                return true;
            }

            if (uiPointerActive)
            {
                uiPointerActive = false;
                Horizontal = 0f;
                return true;
            }

            if (pointerActive)
            {
                pointerActive = false;
                activeTouchId = -1;
                activePointerUsesMouse = false;
                Horizontal = 0f;
                TouchReleased?.Invoke();
                return true;
            }

            return false;
        }

        private bool IsScreenPositionOverUi(Vector2 position)
        {
            if (EventSystem.current != null)
            {
                uiPointerEventData ??= new PointerEventData(EventSystem.current);
                uiPointerEventData.Reset();
                uiPointerEventData.position = position;

                uiRaycastResults.Clear();
                EventSystem.current.RaycastAll(uiPointerEventData, uiRaycastResults);
                for (int i = 0; i < uiRaycastResults.Count; i++)
                {
                    if (IsInteractiveRaycastResult(uiRaycastResults[i]))
                    {
                        return true;
                    }
                }
            }

            return IsScreenPositionOverUiToolkit(position);
        }

        private static bool IsInteractiveRaycastResult(RaycastResult result)
        {
            GameObject go = result.gameObject;
            return go != null && go.GetComponentInParent<UGUISelectable>() != null;
        }

        private static bool IsScreenPositionOverUiToolkit(Vector2 screenPosition)
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                UIDocument document = documents[i];
                if (document == null || !document.isActiveAndEnabled)
                {
                    continue;
                }

                VisualElement root = document.rootVisualElement;
                IPanel panel = root?.panel;
                if (panel == null)
                {
                    continue;
                }

                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
                if (TryFindInteractiveElementAtPanelPosition(root, panel, panelPosition) != null)
                {
                    return true;
                }

                Vector2 fallbackPanelPosition = ConvertScreenToPanelByScale(root, screenPosition);
                if ((fallbackPanelPosition - panelPosition).sqrMagnitude > 0.25f &&
                    TryFindInteractiveElementAtPanelPosition(root, panel, fallbackPanelPosition) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ConvertScreenToPanelByScale(VisualElement root, Vector2 screenPosition)
        {
            float screenWidth = Mathf.Max(Screen.width, 1);
            float screenHeight = Mathf.Max(Screen.height, 1);
            Rect rootBounds = root.worldBound;
            float panelWidth = rootBounds.width > 0f ? rootBounds.width : root.resolvedStyle.width;
            float panelHeight = rootBounds.height > 0f ? rootBounds.height : root.resolvedStyle.height;

            if (panelWidth <= 0f)
            {
                panelWidth = screenWidth;
            }

            if (panelHeight <= 0f)
            {
                panelHeight = screenHeight;
            }

            float x = rootBounds.xMin + Mathf.Clamp01(screenPosition.x / screenWidth) * panelWidth;
            float y = rootBounds.yMin + (1f - Mathf.Clamp01(screenPosition.y / screenHeight)) * panelHeight;
            return new Vector2(x, y);
        }

        private static VisualElement TryFindInteractiveElementAtPanelPosition(VisualElement root, IPanel panel, Vector2 panelPosition)
        {
            VisualElement interactive = FindInteractiveAncestor(panel.Pick(panelPosition));
            if (interactive != null)
            {
                return interactive;
            }

            return FindInteractiveElementByWorldBound(root, panelPosition);
        }

        private static VisualElement FindInteractiveElementByWorldBound(VisualElement element, Vector2 panelPosition)
        {
            if (element == null)
            {
                return null;
            }

            for (int i = 0; i < element.childCount; i++)
            {
                VisualElement child = element.ElementAt(i);
                VisualElement match = FindInteractiveElementByWorldBound(child, panelPosition);
                if (match != null)
                {
                    return match;
                }
            }

            return IsInteractiveElement(element) && element.worldBound.Contains(panelPosition)
                ? element
                : null;
        }

        private static VisualElement FindInteractiveAncestor(VisualElement element)
        {
            while (element != null)
            {
                if (IsInteractiveElement(element))
                {
                    return element;
                }

                element = element.parent;
            }

            return null;
        }

        private static bool IsInteractiveElement(VisualElement element)
        {
            if (element == null || !element.enabledInHierarchy || !element.visible || element.resolvedStyle.display == DisplayStyle.None)
            {
                return false;
            }

            return element is UITKButton ||
                   element is UITKSlider ||
                   element is UITKScroller ||
                   element is UITKScrollView ||
                   element is UITKDropdownField;
        }

        private bool TryGetUiBlockingPointer(out Vector2 position)
        {
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (!touch.press.isPressed)
                    {
                        continue;
                    }

                    Vector2 touchPosition = touch.position.ReadValue();
                    if (!IsScreenPositionOverUi(touchPosition))
                    {
                        continue;
                    }

                    position = touchPosition;
                    return true;
                }
            }

            if (simulateTouchWithMouse && Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                if (IsScreenPositionOverUi(mousePosition))
                {
                    position = mousePosition;
                    return true;
                }
            }

            return false;
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                bool isPressed = touch.phase != UnityEngine.TouchPhase.Canceled && touch.phase != UnityEngine.TouchPhase.Ended;
                if (!isPressed || !IsScreenPositionOverUi(touch.position))
                {
                    continue;
                }

                position = touch.position;
                return true;
            }

            if (simulateTouchWithMouse && Input.GetMouseButton(0))
            {
                Vector2 mousePosition = Input.mousePosition;
                if (IsScreenPositionOverUi(mousePosition))
                {
                    position = mousePosition;
                    return true;
                }
            }

            return false;
#endif
        }

        private bool TryGetMovementPointer(out Vector2 position, out int touchId, out bool usesMouse)
        {
            position = Vector2.zero;
            touchId = -1;
            usesMouse = false;

#if ENABLE_INPUT_SYSTEM
            if (!activePointerUsesMouse &&
                pointerActive &&
                TryGetActiveTouchPositionById(activeTouchId, out Vector2 activeTouchPosition) &&
                !IsScreenPositionOverUi(activeTouchPosition))
            {
                position = activeTouchPosition;
                touchId = activeTouchId;
                return true;
            }

            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (!touch.press.isPressed)
                    {
                        continue;
                    }

                    Vector2 touchPosition = touch.position.ReadValue();
                    if (IsScreenPositionOverUi(touchPosition))
                    {
                        continue;
                    }

                    position = touchPosition;
                    touchId = touch.touchId.ReadValue();
                    return true;
                }
            }

            if (simulateTouchWithMouse && Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                if (!IsScreenPositionOverUi(mousePosition))
                {
                    position = mousePosition;
                    touchId = int.MinValue;
                    usesMouse = true;
                    return true;
                }
            }

            return false;
#else
            if (!activePointerUsesMouse &&
                pointerActive &&
                TryGetActiveTouchPositionById(activeTouchId, out Vector2 activeTouchPosition) &&
                !IsScreenPositionOverUi(activeTouchPosition))
            {
                position = activeTouchPosition;
                touchId = activeTouchId;
                return true;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                bool isPressed = touch.phase != TouchPhase.Canceled && touch.phase != TouchPhase.Ended;
                if (!isPressed || IsScreenPositionOverUi(touch.position))
                {
                    continue;
                }

                position = touch.position;
                touchId = touch.fingerId;
                return true;
            }

            if (simulateTouchWithMouse && Input.GetMouseButton(0))
            {
                Vector2 mousePosition = Input.mousePosition;
                if (!IsScreenPositionOverUi(mousePosition))
                {
                    position = mousePosition;
                    touchId = int.MinValue;
                    usesMouse = true;
                    return true;
                }
            }

            return false;
#endif
        }

        private bool TryGetActiveTouchPositionById(int touchId, out Vector2 position)
        {
            position = Vector2.zero;
            if (touchId < 0)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current == null)
            {
                return false;
            }

            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed || touch.touchId.ReadValue() != touchId)
                {
                    continue;
                }

                position = touch.position.ReadValue();
                return true;
            }

            return false;
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                bool isPressed = touch.phase != TouchPhase.Canceled && touch.phase != TouchPhase.Ended;
                if (!isPressed || touch.fingerId != touchId)
                {
                    continue;
                }

                position = touch.position;
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
            string text = $"Input Horizontal: {Horizontal:F2}  |  TouchActive: {pointerActive}  |  UIBlock: {uiPointerActive}";
            GUI.Box(box, text, debugStyle);
#endif
        }

    }
}
