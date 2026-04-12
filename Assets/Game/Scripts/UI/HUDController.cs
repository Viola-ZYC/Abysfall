using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UITKButton = UnityEngine.UIElements.Button;
using UITKDropdownField = UnityEngine.UIElements.DropdownField;
using UITKSlider = UnityEngine.UIElements.Slider;

namespace EndlessRunner
{
    public class HUDController : MonoBehaviour
    {
        [Serializable]
        private struct CharacterOption
        {
            public string displayName;
            [TextArea] public string description;
            public RunnerConfig runnerConfig;
            public Sprite characterSprite;
            public CharacterAbilityType abilityType;
            public float airJumpImpulse;
            public int airJumpCharges;
        }

        private struct ResolutionOption
        {
            public int width;
            public int height;
        }

        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RunnerController runner;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text healthText;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("UI Toolkit")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private bool createUiDocumentIfMissing = true;
        [SerializeField] private string visualTreeResource = "UI/GameUI";
        [SerializeField] private string panelSettingsResource = "UI/GamePanelSettings";
        [SerializeField] private string scoreLabelName = "score-label";
        [SerializeField] private string healthLabelName = "health-label";
        [SerializeField] private string speedLabelName = "speed-label";
        [SerializeField] private string accelerationLabelName = "acceleration-label";
        [SerializeField] private string pauseButtonName = "pause-button";
        [SerializeField] private string abilityActionContainerName = "ability-action";
        [SerializeField] private string abilityActionButtonName = "ability-action-button";
        [SerializeField] private string menuPanelName = "menu-panel";
        [SerializeField] private string pausePanelName = "pause-panel";
        [SerializeField] private string menuTitleLabelName = "menu-title-label";
        [SerializeField] private string characterSelectionName = "character-selection";
        [SerializeField] private string characterNameLabelName = "character-name-label";
        [SerializeField] private string characterDescLabelName = "character-desc-label";
        [SerializeField] private string characterPrevButtonName = "character-prev-button";
        [SerializeField] private string characterNextButtonName = "character-next-button";
        [SerializeField] private string gameOverPanelName = "gameover-panel";
        [SerializeField] private string safeAreaName = "safe-area";
        [SerializeField] private string menuContinueButtonName = "menu-continue-button";
        [SerializeField] private string menuStartButtonName = "menu-start-button";
        [SerializeField] private string menuLeaderboardButtonName = "menu-leaderboard-button";
        [SerializeField] private string menuCollectionButtonName = "menu-collection-button";
        [SerializeField] private string menuSettingsButtonName = "menu-settings-button";
        [SerializeField] private string menuExitButtonName = "menu-exit-button";
        [SerializeField] private string pauseMainMenuButtonName = "pause-mainmenu-button";
        [SerializeField] private string pauseExitButtonName = "pause-exit-button";
        [SerializeField] private string gameOverRestartButtonName = "gameover-restart-button";
        [SerializeField] private string gameOverMainMenuButtonName = "gameover-mainmenu-button";
        [SerializeField] private string gameOverExitButtonName = "gameover-exit-button";
        [SerializeField] private string menuHintLabelName = "menu-hint-label";
        [SerializeField] private string pauseHintLabelName = "pause-hint-label";
        [SerializeField] private CodexDatabase codexDatabase;
        [SerializeField] private string pauseCollectionButtonName = "pause-collection-button";
        [SerializeField] private string codexPanelName = "codex-panel";
        [SerializeField] private string codexProgressLabelName = "codex-progress-label";
        [SerializeField] private string codexListName = "codex-list";
        [SerializeField] private string codexCloseButtonName = "codex-close-button";
        [SerializeField] private string codexTabCreaturesButtonName = "codex-tab-creatures";
        [SerializeField] private string codexTabObstaclesButtonName = "codex-tab-obstacles";
        [SerializeField] private string codexTabCollectionsButtonName = "codex-tab-collections";
        [SerializeField] private string settingsPanelName = "settings-panel";
        [SerializeField] private string settingsVolumeSliderName = "settings-volume-slider";
        [SerializeField] private string settingsVolumeValueLabelName = "settings-volume-value-label";
        [SerializeField] private string settingsResolutionDropdownName = "settings-resolution-dropdown";
        [SerializeField] private string settingsResolutionHintLabelName = "settings-resolution-hint-label";
        [SerializeField] private string settingsApplyButtonName = "settings-apply-button";
        [SerializeField] private string settingsMainMenuButtonName = "settings-mainmenu-button";
        [SerializeField] private string settingsCloseButtonName = "settings-close-button";
        [SerializeField] private string fxHitFlashName = "fx-hit-flash";
        [SerializeField] private string fxLowHealthName = "fx-low-health";
        [SerializeField] private string fxAbilityPulseName = "fx-ability-pulse";
        [SerializeField] private CharacterOption[] characterOptions;
        [SerializeField] private int defaultCharacterIndex = 0;
        [SerializeField] private bool disableLegacyCanvasOnToolkit = true;
        [SerializeField] private bool applySafeArea = true;
        [SerializeField, Range(0.1f, 0.9f)] private float lowHealthThreshold = 0.35f;
        [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private float abilityPulseDuration = 0.2f;

        private Label scoreLabel;
        private Label healthLabel;
        private Label speedLabel;
        private Label accelerationLabel;
        private Label menuTitleLabel;
        private VisualElement abilityActionContainer;
        private VisualElement menuPanelElement;
        private VisualElement pausePanelElement;
        private VisualElement gameOverPanelElement;
        private VisualElement settingsPanelElement;
        private VisualElement safeAreaElement;
        private VisualElement characterSelectionElement;
        private VisualElement fxHitFlashElement;
        private VisualElement fxLowHealthElement;
        private VisualElement fxAbilityPulseElement;
        private Label characterNameLabel;
        private Label characterDescLabel;
        private Label menuHintLabel;
        private Label pauseHintLabel;
        private Label codexProgressLabel;
        private Label settingsVolumeValueLabel;
        private Label settingsResolutionHintLabel;
        private UITKButton pauseButton;
        private UITKButton abilityActionButton;
        private UITKButton characterPrevButton;
        private UITKButton characterNextButton;
        private UITKButton menuContinueButton;
        private UITKButton pauseCollectionButton;
        private UITKButton menuStartButton;
        private UITKButton menuLeaderboardButton;
        private UITKButton menuCollectionButton;
        private UITKButton menuSettingsButton;
        private UITKButton menuExitButton;
        private UITKButton pauseMainMenuButton;
        private UITKButton pauseExitButton;
        private UITKButton gameOverRestartButton;
        private UITKButton gameOverMainMenuButton;
        private UITKButton gameOverExitButton;
        private UITKButton settingsApplyButton;
        private UITKButton settingsMainMenuButton;
        private UITKButton settingsCloseButton;
        private UITKButton codexCloseButton;
        private UITKButton codexTabCreaturesButton;
        private UITKButton codexTabObstaclesButton;
        private UITKButton codexTabCollectionsButton;
        private UITKSlider settingsVolumeSlider;
        private UITKDropdownField settingsResolutionDropdown;
        private VisualElement codexPanelElement;
        private VisualElement codexListElement;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private Vector2 lastPanelSize = Vector2.zero;
        private Canvas legacyCanvas;
        private GraphicRaycaster legacyRaycaster;
        private bool useToolkit;
        private bool toolkitInitialized;
        private int toolkitRetryFrames;
        private bool toolkitErrorLogged;
        private bool pauseMenuRequested;
        private bool codexPanelRequested;
        private bool settingsPanelRequested;
        private bool settingsInitialized;
        private bool suppressSettingsEvents;
        private CodexCategory currentCodexCategory = CodexCategory.Creature;
        private int selectedResolutionIndex = -1;
        private int selectedCharacterIndex;
        private int lastHealth = -1;
        private int lastHealthMax = -1;
        private Coroutine hitFlashRoutine;
        private Coroutine abilityPulseRoutine;
        private readonly List<ResolutionOption> availableResolutions = new List<ResolutionOption>();
        private readonly List<string> availableResolutionLabels = new List<string>();
        private readonly List<UITKButton> trackedButtons = new List<UITKButton>();
        private Coroutine pendingPointerFallbackRoutine;
        private UITKButton lastTrackedPointerDownButton;
        private int lastTrackedPointerDownFrame = -1;
        private const string VisibleClass = "is-visible";
        private const string FxActiveClass = "is-active";
        private const string TabActiveClass = "is-active";
        private const string LockedClass = "is-locked";
        private const int ToolkitRetryLogFrame = 60;
        private const string MasterVolumePrefKey = "settings.master_volume";
        private const string ResolutionPrefKey = "settings.resolution_index";
        private const string AutoResolutionLabel = "Auto (Recommended)";
        private static readonly string[] FallbackCharacterNames = { "Balanced Loadout", "Mobility Module", "Defense Kit" };
        private static readonly string[] FallbackCharacterDescriptions =
        {
            "Standard setup for steady progress.",
            "Grants one air jump and refreshes on block stomp.",
            "A safer setup focused on survivability."
        };
        private static readonly CharacterAbilityType[] FallbackAbilityTypes =
        {
            CharacterAbilityType.None,
            CharacterAbilityType.SingleAirJumpOnBlock,
            CharacterAbilityType.None
        };
        private const string DefaultPausePanelName = "pause-panel";
        private const string DefaultPauseHintLabelName = "pause-hint-label";
        private const string DefaultPauseCollectionButtonName = "pause-collection-button";
        private const string DefaultCodexPanelName = "codex-panel";
        private const string DefaultCodexProgressLabelName = "codex-progress-label";
        private const string DefaultCodexListName = "codex-list";
        private const string DefaultCodexCloseButtonName = "codex-close-button";
        private const string DefaultCodexTabCreaturesButtonName = "codex-tab-creatures";
        private const string DefaultCodexTabObstaclesButtonName = "codex-tab-obstacles";
        private const string DefaultCodexTabCollectionsButtonName = "codex-tab-collections";

        private void Awake()
        {
            EnsureRuntimeDefaultFieldValues();
            EnsureEventSystem();
            ResolveReferences();
            legacyCanvas = GetComponent<Canvas>();
            legacyRaycaster = GetComponent<GraphicRaycaster>();
            LoadSavedMasterVolume();
            InitializeSelectedCharacterIndex();
            EnsureUiDocument();
        }

        private void OnEnable()
        {
            EnsureRuntimeDefaultFieldValues();
            EnsureEventSystem();
            ResolveReferences();
            CacheToolkitElements(false);
            InitializeToolkitIfNeeded();

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += OnScoreChanged;
            }

            if (gameManager != null)
            {
                gameManager.StateChanged += OnStateChanged;
            }

            if (abilityManager != null)
            {
                abilityManager.AbilityChanged += OnAbilityChanged;
            }

            if (runner != null)
            {
                runner.HealthChanged += OnHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (pendingPointerFallbackRoutine != null)
            {
                StopCoroutine(pendingPointerFallbackRoutine);
                pendingPointerFallbackRoutine = null;
            }

            UnbindMenuButtons();
            toolkitInitialized = false;
            toolkitRetryFrames = 0;
            toolkitErrorLogged = false;

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -= OnScoreChanged;
            }

            if (gameManager != null)
            {
                gameManager.StateChanged -= OnStateChanged;
            }

            if (abilityManager != null)
            {
                abilityManager.AbilityChanged -= OnAbilityChanged;
            }

            if (runner != null)
            {
                runner.HealthChanged -= OnHealthChanged;
            }
        }

        private void Start()
        {
            ResolveReferences();
            CacheToolkitElements(false);
            InitializeToolkitIfNeeded();
        }

        private void Update()
        {
            if (!useToolkit)
            {
                CacheToolkitElements(false);
                InitializeToolkitIfNeeded();
                if (!useToolkit && !toolkitErrorLogged)
                {
                    toolkitRetryFrames++;
                    if (toolkitRetryFrames >= ToolkitRetryLogFrame)
                    {
                        CacheToolkitElements(true);
                        toolkitErrorLogged = true;
                    }
                }
                return;
            }

            RefreshMotionMetrics();
            HandleOverlayBackInput();
            HandlePointerFallbackInput();

            if (!applySafeArea)
            {
                return;
            }

            ApplySafeAreaIfNeeded();
        }

        private void OnScoreChanged(int score)
        {
            if (scoreLabel != null)
            {
                scoreLabel.text = score.ToString();
            }
        }

        private void OnAbilityChanged(AbilityDefinition ability, AbilityManager.AbilityChangeType changeType, int stacks)
        {
            RefreshAbilityButtonState();
            if (changeType == AbilityManager.AbilityChangeType.Acquired)
            {
                TriggerAbilityPulse();
            }
        }

        private void RefreshAbilityButtonState()
        {
            if (abilityActionButton == null)
            {
                return;
            }

            AbilityDefinition ability = abilityManager != null ? abilityManager.CurrentAbility : null;
            if (ability == null)
            {
                abilityActionButton.text = "No Ability";
                abilityActionButton.SetEnabled(false);
                return;
            }

            bool isPassive = ability.isPassive || ability.activeEffect == null;
            abilityActionButton.text = isPassive
                ? $"{ability.displayName} (Passive)"
                : ability.displayName;
            abilityActionButton.SetEnabled(!isPassive);
        }

        private void OnAbilityActionClicked()
        {
            if (abilityManager == null)
            {
                return;
            }

            abilityManager.TryActivateCurrentAbility();
            TriggerAbilityPulse();
        }

        private void OnHealthChanged(int current, int max)
        {
            if (healthLabel != null)
            {
                healthLabel.text = $"HP {current}/{max}";
            }

            if (lastHealth >= 0 && current < lastHealth)
            {
                TriggerHitFlash();
            }

            lastHealth = current;
            lastHealthMax = max;
            UpdateLowHealthFx(current, max);
        }

        private void UpdateLowHealthFx(int current, int max)
        {
            if (fxLowHealthElement == null)
            {
                return;
            }

            if (max <= 0)
            {
                fxLowHealthElement.RemoveFromClassList(FxActiveClass);
                return;
            }

            float ratio = current / (float)max;
            bool isLow = ratio <= lowHealthThreshold;
            fxLowHealthElement.EnableInClassList(FxActiveClass, isLow);
        }

        private void TriggerHitFlash()
        {
            if (fxHitFlashElement == null)
            {
                return;
            }

            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
            }

            hitFlashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private void TriggerAbilityPulse()
        {
            if (fxAbilityPulseElement == null)
            {
                return;
            }

            if (abilityPulseRoutine != null)
            {
                StopCoroutine(abilityPulseRoutine);
            }

            abilityPulseRoutine = StartCoroutine(AbilityPulseRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            yield return FxRoutine(fxHitFlashElement, hitFlashDuration);
            hitFlashRoutine = null;
        }

        private IEnumerator AbilityPulseRoutine()
        {
            yield return FxRoutine(fxAbilityPulseElement, abilityPulseDuration);
            abilityPulseRoutine = null;
        }

        private IEnumerator FxRoutine(VisualElement element, float duration)
        {
            if (element == null)
            {
                yield break;
            }

            element.RemoveFromClassList(FxActiveClass);
            element.AddToClassList(FxActiveClass);
            yield return new WaitForSecondsRealtime(duration);
            element.RemoveFromClassList(FxActiveClass);
        }

        private void RefreshMotionMetrics()
        {
            if (speedLabel == null && accelerationLabel == null)
            {
                return;
            }

            Vector2 velocity = runner != null ? runner.CurrentVelocity : Vector2.zero;
            Vector2 acceleration = runner != null ? runner.CurrentAcceleration : Vector2.zero;

            float speed = velocity.magnitude;
            float accel = acceleration.magnitude;

            if (gameManager != null && gameManager.State != GameState.Running)
            {
                accel = 0f;
                if (speed < 0.001f)
                {
                    speed = 0f;
                }
            }

            if (speedLabel != null)
            {
                speedLabel.text = $"SPD {speed:0.00} m/s";
            }

            if (accelerationLabel != null)
            {
                accelerationLabel.text = $"ACC {accel:0.00} m/s2";
            }
        }

        private void HandleOverlayBackInput()
        {
            if (!IsEscapePressed())
            {
                return;
            }

            if (IsDiscoveryPopupVisible())
            {
                return;
            }

            if (codexPanelRequested)
            {
                OnCodexCloseClicked();
                return;
            }

            if (settingsPanelRequested)
            {
                OnSettingsCloseClicked();
            }
        }

        private bool IsDiscoveryPopupVisible()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                return false;
            }

            VisualElement popup = uiDocument.rootVisualElement.Q<VisualElement>("ability-acquired-panel");
            return popup != null && popup.ClassListContains(VisibleClass);
        }

        private void HandlePointerFallbackInput()
        {
            if (!TryGetFallbackButtonDownThisFrame(out UITKButton button))
            {
                return;
            }

            if (pendingPointerFallbackRoutine != null)
            {
                StopCoroutine(pendingPointerFallbackRoutine);
            }

            pendingPointerFallbackRoutine = StartCoroutine(InvokeFallbackButtonNextFrame(button, Time.frameCount));
        }

        private bool TryGetFallbackButtonDownThisFrame(out UITKButton button)
        {
            button = null;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (!touch.press.wasPressedThisFrame)
                    {
                        continue;
                    }

                    Vector2 touchPosition = touch.position.ReadValue();
                    if (TryGetFallbackButtonAtScreenPosition(touchPosition, out button))
                    {
                        return true;
                    }
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                return TryGetFallbackButtonAtScreenPosition(mousePosition, out button);
            }

            return false;
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                if (TryGetFallbackButtonAtScreenPosition(touch.position, out button))
                {
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Input.mousePosition;
                return TryGetFallbackButtonAtScreenPosition(mousePosition, out button);
            }

            return false;
#endif
        }

        private bool TryGetFallbackButtonAtScreenPosition(Vector2 screenPosition, out UITKButton button)
        {
            button = null;
            if (!useToolkit || uiDocument == null)
            {
                return false;
            }

            VisualElement root = uiDocument.rootVisualElement;
            IPanel panel = root?.panel;
            if (panel == null)
            {
                return false;
            }

            if (TryGetTrackedButtonAtScreenPosition(root, screenPosition, out button))
            {
                return true;
            }

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
            if (TryGetButtonAtPanelPosition(panel, panelPosition, out button))
            {
                return true;
            }

            Vector2 fallbackPanelPosition = ConvertScreenToPanelByScale(root, screenPosition);
            if ((fallbackPanelPosition - panelPosition).sqrMagnitude > 0.25f &&
                TryGetButtonAtPanelPosition(panel, fallbackPanelPosition, out button))
            {
                return true;
            }

            return false;
        }

        private bool TryGetTrackedButtonAtScreenPosition(VisualElement root, Vector2 screenPosition, out UITKButton button)
        {
            button = null;

            Rect rootBounds = root.worldBound;
            if (rootBounds.width <= 0f || rootBounds.height <= 0f)
            {
                return false;
            }

            float screenWidth = Mathf.Max(Screen.width, 1f);
            float screenHeight = Mathf.Max(Screen.height, 1f);
            float bestDistance = float.MaxValue;

            for (int i = 0; i < trackedButtons.Count; i++)
            {
                UITKButton trackedButton = trackedButtons[i];
                if (!IsTrackedButtonInteractive(trackedButton) ||
                    !TryConvertWorldBoundToScreenRect(rootBounds, trackedButton.worldBound, screenWidth, screenHeight, out Rect screenRect) ||
                    !screenRect.Contains(screenPosition))
                {
                    continue;
                }

                float distance = (screenPosition - screenRect.center).sqrMagnitude;
                if (button == null || distance < bestDistance)
                {
                    button = trackedButton;
                    bestDistance = distance;
                }
            }

            return button != null;
        }

        private bool TryGetButtonAtPanelPosition(IPanel panel, Vector2 panelPosition, out UITKButton button)
        {
            button = null;
            if (TryGetTrackedButtonAtPanelPosition(panelPosition, out button))
            {
                return true;
            }

            VisualElement picked = panel.Pick(panelPosition);
            while (picked != null)
            {
                if (picked is UITKButton pickedButton)
                {
                    button = pickedButton;
                    return true;
                }

                picked = picked.parent;
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

        private bool TryGetTrackedButtonAtPanelPosition(Vector2 panelPosition, out UITKButton button)
        {
            button = null;
            for (int i = 0; i < trackedButtons.Count; i++)
            {
                UITKButton trackedButton = trackedButtons[i];
                if (!IsTrackedButtonInteractive(trackedButton))
                {
                    continue;
                }

                if (!trackedButton.worldBound.Contains(panelPosition))
                {
                    continue;
                }

                button = trackedButton;
                return true;
            }

            return false;
        }

        private static bool IsTrackedButtonInteractive(UITKButton button)
        {
            if (button == null || !button.enabledInHierarchy)
            {
                return false;
            }

            for (VisualElement current = button; current != null; current = current.parent)
            {
                if (!current.visible || current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return button.worldBound.width > 0f && button.worldBound.height > 0f;
        }

        private static bool TryConvertWorldBoundToScreenRect(Rect rootBounds, Rect worldBound, float screenWidth, float screenHeight, out Rect screenRect)
        {
            screenRect = default;
            if (rootBounds.width <= 0f || rootBounds.height <= 0f || worldBound.width <= 0f || worldBound.height <= 0f)
            {
                return false;
            }

            float minX = ((worldBound.xMin - rootBounds.xMin) / rootBounds.width) * screenWidth;
            float maxX = ((worldBound.xMax - rootBounds.xMin) / rootBounds.width) * screenWidth;
            float minY = screenHeight - ((worldBound.yMax - rootBounds.yMin) / rootBounds.height) * screenHeight;
            float maxY = screenHeight - ((worldBound.yMin - rootBounds.yMin) / rootBounds.height) * screenHeight;
            screenRect = Rect.MinMaxRect(
                Mathf.Min(minX, maxX),
                Mathf.Min(minY, maxY),
                Mathf.Max(minX, maxX),
                Mathf.Max(minY, maxY));
            return true;
        }

        private IEnumerator InvokeFallbackButtonNextFrame(UITKButton button, int pointerDownFrame)
        {
            yield return null;
            pendingPointerFallbackRoutine = null;

            if (!isActiveAndEnabled || button == null)
            {
                yield break;
            }

            if (!ShouldForceTouchFallback(button) &&
                lastTrackedPointerDownButton == button &&
                lastTrackedPointerDownFrame >= pointerDownFrame)
            {
                yield break;
            }

            InvokeFallbackButton(button);
        }

        private bool ShouldForceTouchFallback(UITKButton button)
        {
            return button == pauseButton ||
                   button == pauseCollectionButton ||
                   button == menuContinueButton ||
                   button == pauseMainMenuButton ||
                   button == pauseExitButton ||
                   button == settingsApplyButton ||
                   button == settingsMainMenuButton ||
                   button == settingsCloseButton ||
                   button == codexCloseButton ||
                   button == codexTabCreaturesButton ||
                   button == codexTabObstaclesButton ||
                   button == codexTabCollectionsButton;
        }

        private void RegisterTrackedButton(UITKButton button)
        {
            if (button == null)
            {
                return;
            }

            if (!trackedButtons.Contains(button))
            {
                trackedButtons.Add(button);
            }

            button.UnregisterCallback<PointerDownEvent>(OnTrackedButtonPointerDown);
            button.RegisterCallback<PointerDownEvent>(OnTrackedButtonPointerDown);
        }

        private void UnregisterTrackedButton(UITKButton button)
        {
            if (button == null)
            {
                return;
            }

            trackedButtons.Remove(button);
            button.UnregisterCallback<PointerDownEvent>(OnTrackedButtonPointerDown);
        }

        private void OnTrackedButtonPointerDown(PointerDownEvent evt)
        {
            lastTrackedPointerDownButton = evt.currentTarget as UITKButton;
            lastTrackedPointerDownFrame = Time.frameCount;
        }

        private void InvokeFallbackButton(UITKButton button)
        {
            if (button == null || !button.enabledInHierarchy)
            {
                return;
            }

            if (button == pauseButton)
            {
                OnPauseClicked();
            }
            else if (button == abilityActionButton)
            {
                OnAbilityActionClicked();
            }
            else if (button == characterPrevButton)
            {
                OnCharacterPrevClicked();
            }
            else if (button == characterNextButton)
            {
                OnCharacterNextClicked();
            }
            else if (button == menuContinueButton)
            {
                OnContinueClicked();
            }
            else if (button == pauseCollectionButton || button == menuCollectionButton)
            {
                OnCollectionClicked();
            }
            else if (button == menuStartButton)
            {
                OnStartClicked();
            }
            else if (button == menuLeaderboardButton)
            {
                OnLeaderboardClicked();
            }
            else if (button == menuSettingsButton)
            {
                OnSettingsClicked();
            }
            else if (button == menuExitButton || button == pauseExitButton)
            {
                OnExitClicked();
            }
            else if (button == pauseMainMenuButton)
            {
                OnPauseMainMenuClicked();
            }
            else if (button == gameOverRestartButton)
            {
                OnGameOverRestartClicked();
            }
            else if (button == gameOverMainMenuButton)
            {
                OnGameOverMainMenuClicked();
            }
            else if (button == gameOverExitButton)
            {
                OnGameOverExitClicked();
            }
            else if (button == settingsApplyButton)
            {
                OnSettingsApplyClicked();
            }
            else if (button == settingsMainMenuButton)
            {
                OnSettingsMainMenuClicked();
            }
            else if (button == settingsCloseButton)
            {
                OnSettingsCloseClicked();
            }
            else if (button == codexCloseButton)
            {
                OnCodexCloseClicked();
            }
            else if (button == codexTabCreaturesButton)
            {
                OnCodexCreaturesClicked();
            }
            else if (button == codexTabObstaclesButton)
            {
                OnCodexObstaclesClicked();
            }
            else if (button == codexTabCollectionsButton)
            {
                OnCodexCollectionsClicked();
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (!useToolkit)
            {
                return;
            }

            bool showPauseMenu = pauseMenuRequested && state == GameState.Paused;
            bool showMainMenu = state == GameState.Menu;
            bool showMainMenuOnlyButtons = showMainMenu && IsMainMenuSceneActive();
            bool canShowSettings = state == GameState.Menu || state == GameState.Paused;
            bool canShowCodex = state == GameState.Menu || state == GameState.Paused;
            if (!canShowSettings)
            {
                settingsPanelRequested = false;
            }

            if (!canShowCodex)
            {
                codexPanelRequested = false;
            }

            SetAbilityButtonVisible(state == GameState.Running);
            RefreshMenuPanelVisibility();
            SetDisplay(gameOverPanelElement, state == GameState.GameOver);
            SetSettingsPanelVisible(canShowSettings && settingsPanelRequested);
            SetCodexPanelVisible(canShowCodex && codexPanelRequested);
            SetPauseButtonVisible(state == GameState.Running);
            SetContinueButtonVisible(showPauseMenu);
            SetPauseActionButtonsVisible(showPauseMenu);
            SetPauseCollectionButtonVisible(showPauseMenu);
            SetCharacterSelectionVisible(showMainMenu);
            SetMenuOptionButtonsVisible(showMainMenuOnlyButtons);
            SetStartButtonVisible(showMainMenu);
            SetMenuTitle(showPauseMenu ? "Paused" : "Loadout Select");

            if (showPauseMenu)
            {
                SetMenuHint("Game paused.");
            }
            else if (state == GameState.Menu)
            {
                SetMenuHint("Choose a loadout, then begin the descent.");
            }
        }

        private void CacheToolkitElements(bool logErrors)
        {
            useToolkit = false;
            EnsureRuntimeDefaultFieldValues();
            EnsureUiDocument();

            if (uiDocument == null)
            {
                return;
            }

            if (uiDocument.panelSettings == null && !string.IsNullOrEmpty(panelSettingsResource))
            {
                uiDocument.panelSettings = Resources.Load<PanelSettings>(panelSettingsResource);
            }

            if (uiDocument.visualTreeAsset == null && !string.IsNullOrEmpty(visualTreeResource))
            {
                uiDocument.visualTreeAsset = Resources.Load<VisualTreeAsset>(visualTreeResource);
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            scoreLabel = root.Q<Label>(scoreLabelName);
            healthLabel = root.Q<Label>(healthLabelName);
            speedLabel = root.Q<Label>(speedLabelName);
            accelerationLabel = root.Q<Label>(accelerationLabelName);
            pauseButton = root.Q<UITKButton>(pauseButtonName);
            abilityActionContainer = root.Q<VisualElement>(abilityActionContainerName);
            abilityActionButton = root.Q<UITKButton>(abilityActionButtonName);
            menuPanelElement = root.Q<VisualElement>(menuPanelName);
            pausePanelElement = root.Q<VisualElement>(pausePanelName);
            menuTitleLabel = root.Q<Label>(menuTitleLabelName);
            characterSelectionElement = root.Q<VisualElement>(characterSelectionName);
            characterNameLabel = root.Q<Label>(characterNameLabelName);
            characterDescLabel = root.Q<Label>(characterDescLabelName);
            characterPrevButton = root.Q<UITKButton>(characterPrevButtonName);
            characterNextButton = root.Q<UITKButton>(characterNextButtonName);
            gameOverPanelElement = root.Q<VisualElement>(gameOverPanelName);
            settingsPanelElement = root.Q<VisualElement>(settingsPanelName);
            safeAreaElement = root.Q<VisualElement>(safeAreaName);
            fxHitFlashElement = root.Q<VisualElement>(fxHitFlashName);
            fxLowHealthElement = root.Q<VisualElement>(fxLowHealthName);
            fxAbilityPulseElement = root.Q<VisualElement>(fxAbilityPulseName);
            menuHintLabel = root.Q<Label>(menuHintLabelName);
            pauseHintLabel = root.Q<Label>(pauseHintLabelName);
            menuContinueButton = root.Q<UITKButton>(menuContinueButtonName);
            pauseCollectionButton = root.Q<UITKButton>(pauseCollectionButtonName);
            menuStartButton = root.Q<UITKButton>(menuStartButtonName);
            menuLeaderboardButton = root.Q<UITKButton>(menuLeaderboardButtonName);
            menuCollectionButton = root.Q<UITKButton>(menuCollectionButtonName);
            menuSettingsButton = root.Q<UITKButton>(menuSettingsButtonName);
            menuExitButton = root.Q<UITKButton>(menuExitButtonName);
            pauseMainMenuButton = root.Q<UITKButton>(pauseMainMenuButtonName);
            pauseExitButton = root.Q<UITKButton>(pauseExitButtonName);
            gameOverRestartButton = root.Q<UITKButton>(gameOverRestartButtonName);
            gameOverMainMenuButton = root.Q<UITKButton>(gameOverMainMenuButtonName);
            gameOverExitButton = root.Q<UITKButton>(gameOverExitButtonName);
            settingsVolumeSlider = root.Q<UITKSlider>(settingsVolumeSliderName);
            settingsVolumeValueLabel = root.Q<Label>(settingsVolumeValueLabelName);
            settingsResolutionDropdown = root.Q<UITKDropdownField>(settingsResolutionDropdownName);
            settingsResolutionHintLabel = root.Q<Label>(settingsResolutionHintLabelName);
            settingsApplyButton = root.Q<UITKButton>(settingsApplyButtonName);
            settingsMainMenuButton = root.Q<UITKButton>(settingsMainMenuButtonName);
            settingsCloseButton = root.Q<UITKButton>(settingsCloseButtonName);
            codexPanelElement = root.Q<VisualElement>(codexPanelName);
            codexProgressLabel = root.Q<Label>(codexProgressLabelName);
            codexListElement = root.Q<VisualElement>(codexListName);
            codexCloseButton = root.Q<UITKButton>(codexCloseButtonName);
            codexTabCreaturesButton = root.Q<UITKButton>(codexTabCreaturesButtonName);
            codexTabObstaclesButton = root.Q<UITKButton>(codexTabObstaclesButtonName);
            codexTabCollectionsButton = root.Q<UITKButton>(codexTabCollectionsButtonName);

            List<string> missingElements = new List<string>();
            if (scoreLabel == null) missingElements.Add(scoreLabelName);
            if (healthLabel == null) missingElements.Add(healthLabelName);
            if (speedLabel == null) missingElements.Add(speedLabelName);
            if (accelerationLabel == null) missingElements.Add(accelerationLabelName);
            if (pauseButton == null) missingElements.Add(pauseButtonName);
            if (abilityActionContainer == null) missingElements.Add(abilityActionContainerName);
            if (abilityActionButton == null) missingElements.Add(abilityActionButtonName);
            if (menuPanelElement == null) missingElements.Add(menuPanelName);
            if (pausePanelElement == null) missingElements.Add(pausePanelName);
            if (menuTitleLabel == null) missingElements.Add(menuTitleLabelName);
            if (characterSelectionElement == null) missingElements.Add(characterSelectionName);
            if (characterNameLabel == null) missingElements.Add(characterNameLabelName);
            if (characterDescLabel == null) missingElements.Add(characterDescLabelName);
            if (characterPrevButton == null) missingElements.Add(characterPrevButtonName);
            if (characterNextButton == null) missingElements.Add(characterNextButtonName);
            if (gameOverPanelElement == null) missingElements.Add(gameOverPanelName);
            if (settingsPanelElement == null) missingElements.Add(settingsPanelName);
            if (menuContinueButton == null) missingElements.Add(menuContinueButtonName);
            if (menuStartButton == null) missingElements.Add(menuStartButtonName);
            if (menuLeaderboardButton == null) missingElements.Add(menuLeaderboardButtonName);
            if (menuCollectionButton == null) missingElements.Add(menuCollectionButtonName);
            if (menuSettingsButton == null) missingElements.Add(menuSettingsButtonName);
            if (menuExitButton == null) missingElements.Add(menuExitButtonName);
            if (pauseMainMenuButton == null) missingElements.Add(pauseMainMenuButtonName);
            if (pauseExitButton == null) missingElements.Add(pauseExitButtonName);
            if (gameOverRestartButton == null) missingElements.Add(gameOverRestartButtonName);
            if (gameOverMainMenuButton == null) missingElements.Add(gameOverMainMenuButtonName);
            if (gameOverExitButton == null) missingElements.Add(gameOverExitButtonName);
            if (settingsVolumeSlider == null) missingElements.Add(settingsVolumeSliderName);
            if (settingsVolumeValueLabel == null) missingElements.Add(settingsVolumeValueLabelName);
            if (settingsResolutionDropdown == null) missingElements.Add(settingsResolutionDropdownName);
            if (settingsResolutionHintLabel == null) missingElements.Add(settingsResolutionHintLabelName);
            if (settingsApplyButton == null) missingElements.Add(settingsApplyButtonName);
            if (settingsMainMenuButton == null) missingElements.Add(settingsMainMenuButtonName);
            if (settingsCloseButton == null) missingElements.Add(settingsCloseButtonName);

            useToolkit = missingElements.Count == 0;
            if (!useToolkit && logErrors)
            {
                Debug.LogError(
                    $"HUDController could not bind required UI Toolkit elements. Missing: {string.Join(", ", missingElements)}",
                    this);
            }
            else if (logErrors && !HasCodexUi())
            {
                Debug.LogWarning(
                    "HUDController initialized without complete codex UI. Pause/menu will still work, but Manual is unavailable until the codex panel elements are present.",
                    this);
            }

            if (useToolkit && lastHealth >= 0)
            {
                UpdateLowHealthFx(lastHealth, lastHealthMax);
            }
        }

        private void EnsureUiDocument()
        {
            if (uiDocument != null)
            {
                return;
            }

            GameObject gameUi = GameObject.Find("GameUI");
            if (gameUi != null)
            {
                uiDocument = gameUi.GetComponent<UIDocument>();
            }

            if (uiDocument != null)
            {
                return;
            }

            uiDocument = FindAnyObjectByType<UIDocument>();
            if (uiDocument != null || !createUiDocumentIfMissing)
            {
                return;
            }

            GameObject uiRoot = new GameObject("GameUI");
            uiDocument = uiRoot.AddComponent<UIDocument>();
            if (!string.IsNullOrEmpty(panelSettingsResource))
            {
                uiDocument.panelSettings = Resources.Load<PanelSettings>(panelSettingsResource);
            }
            if (!string.IsNullOrEmpty(visualTreeResource))
            {
                uiDocument.visualTreeAsset = Resources.Load<VisualTreeAsset>(visualTreeResource);
            }
        }

        private void ResolveReferences()
        {
            if (scoreManager == null)
            {
                scoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            }

            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }

            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }
        }

        private void EnsureRuntimeDefaultFieldValues()
        {
            if (string.IsNullOrWhiteSpace(pausePanelName))
            {
                pausePanelName = DefaultPausePanelName;
            }

            if (string.IsNullOrWhiteSpace(pauseHintLabelName))
            {
                pauseHintLabelName = DefaultPauseHintLabelName;
            }

            if (string.IsNullOrWhiteSpace(pauseCollectionButtonName))
            {
                pauseCollectionButtonName = DefaultPauseCollectionButtonName;
            }

            if (string.IsNullOrWhiteSpace(codexPanelName))
            {
                codexPanelName = DefaultCodexPanelName;
            }

            if (string.IsNullOrWhiteSpace(codexProgressLabelName))
            {
                codexProgressLabelName = DefaultCodexProgressLabelName;
            }

            if (string.IsNullOrWhiteSpace(codexListName))
            {
                codexListName = DefaultCodexListName;
            }

            if (string.IsNullOrWhiteSpace(codexCloseButtonName))
            {
                codexCloseButtonName = DefaultCodexCloseButtonName;
            }

            if (string.IsNullOrWhiteSpace(codexTabCreaturesButtonName))
            {
                codexTabCreaturesButtonName = DefaultCodexTabCreaturesButtonName;
            }

            if (string.IsNullOrWhiteSpace(codexTabObstaclesButtonName))
            {
                codexTabObstaclesButtonName = DefaultCodexTabObstaclesButtonName;
            }

            if (string.IsNullOrWhiteSpace(codexTabCollectionsButtonName))
            {
                codexTabCollectionsButtonName = DefaultCodexTabCollectionsButtonName;
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// Runtime hook: allows character manager to replace the active runner.
        /// Keeps health UI/event wiring in sync.
        /// </summary>
        public void SetRunner(RunnerController newRunner)
        {
            if (runner == newRunner)
            {
                return;
            }

            if (runner != null)
            {
                runner.HealthChanged -= OnHealthChanged;
            }

            runner = newRunner;

            if (isActiveAndEnabled && runner != null)
            {
                runner.HealthChanged += OnHealthChanged;
                OnHealthChanged(runner.CurrentHealth, runner.MaxHealth);
            }

            RefreshMotionMetrics();
        }

        private void InitializeToolkitIfNeeded()
        {
            if (!useToolkit || toolkitInitialized)
            {
                return;
            }

            BindMenuButtons();
            RefreshCharacterSelectionUI();
            EnsureSettingsInitialized();
            SetCodexCategory(currentCodexCategory);
            if (disableLegacyCanvasOnToolkit)
            {
                DisableLegacyUI();
            }

            OnScoreChanged(scoreManager != null ? scoreManager.Score : 0);
            if (runner != null)
            {
                OnHealthChanged(runner.CurrentHealth, runner.MaxHealth);
            }
            RefreshMotionMetrics();
            RefreshAbilityButtonState();

            if (gameManager != null)
            {
                OnStateChanged(gameManager.State);
            }
            else
            {
                ShowMenuFallback();
            }

            toolkitInitialized = true;
        }

        private void ShowMenuFallback()
        {
            SetDisplay(menuPanelElement, true);
            SetDisplay(pausePanelElement, false);
            SetDisplay(gameOverPanelElement, false);
            SetSettingsPanelVisible(false);
            SetCodexPanelVisible(false);
            SetPauseButtonVisible(false);
            SetContinueButtonVisible(false);
            SetPauseActionButtonsVisible(false);
            SetPauseCollectionButtonVisible(false);
            SetCharacterSelectionVisible(true);
            SetMenuOptionButtonsVisible(IsMainMenuSceneActive());
            SetStartButtonVisible(true);
            SetMenuTitle("Loadout Select");
            SetMenuHint("Choose a loadout, then begin the descent.");
        }

        private void DisableLegacyUI()
        {
            if (legacyCanvas != null)
            {
                legacyCanvas.enabled = false;
            }

            if (legacyRaycaster != null)
            {
                legacyRaycaster.enabled = false;
            }

            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }

            if (healthText != null)
            {
                healthText.gameObject.SetActive(false);
            }

            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void BindMenuButtons()
        {
            if (!useToolkit)
            {
                return;
            }

            pauseButton.clicked -= OnPauseClicked;
            pauseButton.clicked += OnPauseClicked;
            RegisterTrackedButton(pauseButton);

            if (abilityActionButton != null)
            {
                abilityActionButton.clicked -= OnAbilityActionClicked;
                abilityActionButton.clicked += OnAbilityActionClicked;
                RegisterTrackedButton(abilityActionButton);
            }

            characterPrevButton.clicked -= OnCharacterPrevClicked;
            characterPrevButton.clicked += OnCharacterPrevClicked;
            RegisterTrackedButton(characterPrevButton);

            characterNextButton.clicked -= OnCharacterNextClicked;
            characterNextButton.clicked += OnCharacterNextClicked;
            RegisterTrackedButton(characterNextButton);

            menuContinueButton.clicked -= OnContinueClicked;
            menuContinueButton.clicked += OnContinueClicked;
            RegisterTrackedButton(menuContinueButton);

            if (pauseCollectionButton != null)
            {
                pauseCollectionButton.clicked -= OnCollectionClicked;
                pauseCollectionButton.clicked += OnCollectionClicked;
                RegisterTrackedButton(pauseCollectionButton);
            }

            menuStartButton.clicked -= OnStartClicked;
            menuStartButton.clicked += OnStartClicked;
            RegisterTrackedButton(menuStartButton);

            menuLeaderboardButton.clicked -= OnLeaderboardClicked;
            menuLeaderboardButton.clicked += OnLeaderboardClicked;
            RegisterTrackedButton(menuLeaderboardButton);

            menuCollectionButton.clicked -= OnCollectionClicked;
            menuCollectionButton.clicked += OnCollectionClicked;
            RegisterTrackedButton(menuCollectionButton);

            menuSettingsButton.clicked -= OnSettingsClicked;
            menuSettingsButton.clicked += OnSettingsClicked;
            RegisterTrackedButton(menuSettingsButton);

            menuExitButton.clicked -= OnExitClicked;
            menuExitButton.clicked += OnExitClicked;
            RegisterTrackedButton(menuExitButton);

            pauseMainMenuButton.clicked -= OnPauseMainMenuClicked;
            pauseMainMenuButton.clicked += OnPauseMainMenuClicked;
            RegisterTrackedButton(pauseMainMenuButton);

            pauseExitButton.clicked -= OnPauseExitClicked;
            pauseExitButton.clicked += OnPauseExitClicked;
            RegisterTrackedButton(pauseExitButton);

            gameOverRestartButton.clicked -= OnGameOverRestartClicked;
            gameOverRestartButton.clicked += OnGameOverRestartClicked;
            RegisterTrackedButton(gameOverRestartButton);

            gameOverMainMenuButton.clicked -= OnGameOverMainMenuClicked;
            gameOverMainMenuButton.clicked += OnGameOverMainMenuClicked;
            RegisterTrackedButton(gameOverMainMenuButton);

            gameOverExitButton.clicked -= OnGameOverExitClicked;
            gameOverExitButton.clicked += OnGameOverExitClicked;
            RegisterTrackedButton(gameOverExitButton);

            settingsApplyButton.clicked -= OnSettingsApplyClicked;
            settingsApplyButton.clicked += OnSettingsApplyClicked;
            RegisterTrackedButton(settingsApplyButton);

            settingsMainMenuButton.clicked -= OnSettingsMainMenuClicked;
            settingsMainMenuButton.clicked += OnSettingsMainMenuClicked;
            RegisterTrackedButton(settingsMainMenuButton);

            settingsCloseButton.clicked -= OnSettingsCloseClicked;
            settingsCloseButton.clicked += OnSettingsCloseClicked;
            RegisterTrackedButton(settingsCloseButton);

            if (codexCloseButton != null)
            {
                codexCloseButton.clicked -= OnCodexCloseClicked;
                codexCloseButton.clicked += OnCodexCloseClicked;
                RegisterTrackedButton(codexCloseButton);
            }

            if (codexTabCreaturesButton != null)
            {
                codexTabCreaturesButton.clicked -= OnCodexCreaturesClicked;
                codexTabCreaturesButton.clicked += OnCodexCreaturesClicked;
                RegisterTrackedButton(codexTabCreaturesButton);
            }

            if (codexTabObstaclesButton != null)
            {
                codexTabObstaclesButton.clicked -= OnCodexObstaclesClicked;
                codexTabObstaclesButton.clicked += OnCodexObstaclesClicked;
                RegisterTrackedButton(codexTabObstaclesButton);
            }

            if (codexTabCollectionsButton != null)
            {
                codexTabCollectionsButton.clicked -= OnCodexCollectionsClicked;
                codexTabCollectionsButton.clicked += OnCodexCollectionsClicked;
                RegisterTrackedButton(codexTabCollectionsButton);
            }

            settingsVolumeSlider.UnregisterValueChangedCallback(OnSettingsVolumeChanged);
            settingsVolumeSlider.RegisterValueChangedCallback(OnSettingsVolumeChanged);

            settingsResolutionDropdown.UnregisterValueChangedCallback(OnSettingsResolutionChanged);
            settingsResolutionDropdown.RegisterValueChangedCallback(OnSettingsResolutionChanged);

            RefreshCharacterSelectionUI();
            EnsureSettingsInitialized();
            SetMenuHint("Choose a loadout, then begin the descent.");
            RefreshAbilityButtonState();
        }

        private void UnbindMenuButtons()
        {
            if (menuStartButton != null)
            {
                menuStartButton.clicked -= OnStartClicked;
                UnregisterTrackedButton(menuStartButton);
            }

            if (pauseButton != null)
            {
                pauseButton.clicked -= OnPauseClicked;
                UnregisterTrackedButton(pauseButton);
            }

            if (abilityActionButton != null)
            {
                abilityActionButton.clicked -= OnAbilityActionClicked;
                UnregisterTrackedButton(abilityActionButton);
            }

            if (characterPrevButton != null)
            {
                characterPrevButton.clicked -= OnCharacterPrevClicked;
                UnregisterTrackedButton(characterPrevButton);
            }

            if (characterNextButton != null)
            {
                characterNextButton.clicked -= OnCharacterNextClicked;
                UnregisterTrackedButton(characterNextButton);
            }

            if (menuContinueButton != null)
            {
                menuContinueButton.clicked -= OnContinueClicked;
                UnregisterTrackedButton(menuContinueButton);
            }

            if (pauseCollectionButton != null)
            {
                pauseCollectionButton.clicked -= OnCollectionClicked;
                UnregisterTrackedButton(pauseCollectionButton);
            }

            if (menuLeaderboardButton != null)
            {
                menuLeaderboardButton.clicked -= OnLeaderboardClicked;
                UnregisterTrackedButton(menuLeaderboardButton);
            }

            if (menuCollectionButton != null)
            {
                menuCollectionButton.clicked -= OnCollectionClicked;
                UnregisterTrackedButton(menuCollectionButton);
            }

            if (menuSettingsButton != null)
            {
                menuSettingsButton.clicked -= OnSettingsClicked;
                UnregisterTrackedButton(menuSettingsButton);
            }

            if (menuExitButton != null)
            {
                menuExitButton.clicked -= OnExitClicked;
                UnregisterTrackedButton(menuExitButton);
            }

            if (pauseMainMenuButton != null)
            {
                pauseMainMenuButton.clicked -= OnPauseMainMenuClicked;
                UnregisterTrackedButton(pauseMainMenuButton);
            }

            if (pauseExitButton != null)
            {
                pauseExitButton.clicked -= OnPauseExitClicked;
                UnregisterTrackedButton(pauseExitButton);
            }

            if (gameOverRestartButton != null)
            {
                gameOverRestartButton.clicked -= OnGameOverRestartClicked;
                UnregisterTrackedButton(gameOverRestartButton);
            }

            if (gameOverMainMenuButton != null)
            {
                gameOverMainMenuButton.clicked -= OnGameOverMainMenuClicked;
                UnregisterTrackedButton(gameOverMainMenuButton);
            }

            if (gameOverExitButton != null)
            {
                gameOverExitButton.clicked -= OnGameOverExitClicked;
                UnregisterTrackedButton(gameOverExitButton);
            }

            if (settingsApplyButton != null)
            {
                settingsApplyButton.clicked -= OnSettingsApplyClicked;
                UnregisterTrackedButton(settingsApplyButton);
            }

            if (settingsMainMenuButton != null)
            {
                settingsMainMenuButton.clicked -= OnSettingsMainMenuClicked;
                UnregisterTrackedButton(settingsMainMenuButton);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.clicked -= OnSettingsCloseClicked;
                UnregisterTrackedButton(settingsCloseButton);
            }

            if (codexCloseButton != null)
            {
                codexCloseButton.clicked -= OnCodexCloseClicked;
                UnregisterTrackedButton(codexCloseButton);
            }

            if (codexTabCreaturesButton != null)
            {
                codexTabCreaturesButton.clicked -= OnCodexCreaturesClicked;
                UnregisterTrackedButton(codexTabCreaturesButton);
            }

            if (codexTabObstaclesButton != null)
            {
                codexTabObstaclesButton.clicked -= OnCodexObstaclesClicked;
                UnregisterTrackedButton(codexTabObstaclesButton);
            }

            if (codexTabCollectionsButton != null)
            {
                codexTabCollectionsButton.clicked -= OnCodexCollectionsClicked;
                UnregisterTrackedButton(codexTabCollectionsButton);
            }

            if (settingsVolumeSlider != null)
            {
                settingsVolumeSlider.UnregisterValueChangedCallback(OnSettingsVolumeChanged);
            }

            if (settingsResolutionDropdown != null)
            {
                settingsResolutionDropdown.UnregisterValueChangedCallback(OnSettingsResolutionChanged);
            }
        }

        private void OnCharacterPrevClicked()
        {
            ShiftCharacterSelection(-1);
        }

        private void OnCharacterNextClicked()
        {
            ShiftCharacterSelection(1);
        }

        private void ShiftCharacterSelection(int delta)
        {
            int count = GetCharacterCount();
            if (count <= 1)
            {
                return;
            }

            selectedCharacterIndex = (selectedCharacterIndex + delta) % count;
            if (selectedCharacterIndex < 0)
            {
                selectedCharacterIndex += count;
            }

            RefreshCharacterSelectionUI();
        }

        private void OnPauseClicked()
        {
            if (gameManager == null || gameManager.State != GameState.Running)
            {
                Debug.LogWarning(
                    $"HUDController ignored pause click. GameManager present: {gameManager != null}, state: {(gameManager != null ? gameManager.State.ToString() : "null")}",
                    this);
                return;
            }

            CloseOverlaySubpanels();
            pauseMenuRequested = true;
            gameManager.RequestPause(this);
        }

        private void OnContinueClicked()
        {
            if (gameManager == null || gameManager.State != GameState.Paused)
            {
                return;
            }

            CloseOverlaySubpanels();
            pauseMenuRequested = false;
            gameManager.ReleasePause(this);
        }

        private void OnStartClicked()
        {
            CloseOverlaySubpanels();
            pauseMenuRequested = false;
            ApplySelectedCharacter();
            gameManager?.BeginRun();
        }

        private void OnLeaderboardClicked()
        {
            int bestScore = RunProgressStore.GetBestScore();
            int lastScore = RunProgressStore.GetLastScore();
            int totalRuns = RunProgressStore.GetTotalRuns();
            string modeName = RunProgressStore.GetModeDisplayName(RunProgressStore.GetSelectedModeId());
            SetMenuHint($"Mode {modeName} | Best {bestScore} | Last {lastScore} | Runs {totalRuns}");
        }

        private void OnCollectionClicked()
        {
            if (gameManager == null)
            {
                return;
            }

            if (!HasCodexUi())
            {
                SetMenuHint("Codex UI is unavailable in the current GameUI layout.");
                return;
            }

            if (gameManager.State != GameState.Paused && gameManager.State != GameState.Menu)
            {
                int unlockedCount = RunProgressStore.GetUnlockedCollectionCount();
                CodexDatabase database = GetCodexDatabase();
                int totalCount = database != null ? database.GetEntryCount(CodexCategory.Collection) : 0;
                string progress = totalCount > 0 ? $"{unlockedCount}/{totalCount}" : unlockedCount.ToString();
                SetMenuHint($"Codex unlocked: {progress}");
                return;
            }

            CloseSettingsPanel();
            codexPanelRequested = true;
            SetCodexCategory(currentCodexCategory);
            SetCodexPanelVisible(true);
            RefreshMenuPanelVisibility();
            SetMenuHint("Codex opened.");
        }

        private void OnSettingsClicked()
        {
            EnsureSettingsInitialized();
            CloseCodexPanel();
            settingsPanelRequested = true;
            RefreshSettingsPanel();
            SetSettingsPanelVisible(true);
            RefreshMenuPanelVisibility();
            SetMenuHint("Settings panel opened.");
        }

        private void OnExitClicked()
        {
            if (gameManager != null)
            {
                gameManager.QuitGame();
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnPauseMainMenuClicked()
        {
            pauseMenuRequested = false;
            CloseOverlaySubpanels();
            if (gameManager != null)
            {
                gameManager.LoadMainMenuScene();
                return;
            }

            if (!SceneTransitionOverlay.TryLoadScene("MainMenuScene"))
            {
                SceneManager.LoadScene("MainMenuScene");
            }
        }

        private void OnPauseExitClicked()
        {
            OnExitClicked();
        }

        private void OnCodexCloseClicked()
        {
            CloseCodexPanel();
            SetMenuHint(gameManager != null && gameManager.State == GameState.Paused ? "Game paused." : "Codex closed.");
        }

        private void OnCodexCreaturesClicked()
        {
            SetCodexCategory(CodexCategory.Creature);
        }

        private void OnCodexObstaclesClicked()
        {
            SetCodexCategory(CodexCategory.Obstacle);
        }

        private void OnCodexCollectionsClicked()
        {
            SetCodexCategory(CodexCategory.Collection);
        }

        private void OnGameOverRestartClicked()
        {
            if (gameManager != null)
            {
                // 需求：等价于重新点一次 Play，直接重载游戏场景。
                gameManager.LoadGameplayScene();
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && !SceneTransitionOverlay.TryLoadScene(active.name))
            {
                SceneManager.LoadScene(active.name);
            }
        }

        private void OnGameOverExitClicked()
        {
            OnExitClicked();
        }

        private void OnGameOverMainMenuClicked()
        {
            OnPauseMainMenuClicked();
        }

        private void OnSettingsApplyClicked()
        {
            if (settingsVolumeSlider != null)
            {
                ApplyMasterVolume(settingsVolumeSlider.value, true);
            }

            if (SupportsRuntimeResolutionSelection())
            {
                ResolutionOption resolution;
                if (selectedResolutionIndex >= 0 && selectedResolutionIndex < availableResolutions.Count)
                {
                    resolution = availableResolutions[selectedResolutionIndex];
                    PlayerPrefs.SetInt(ResolutionPrefKey, selectedResolutionIndex);
                }
                else
                {
                    resolution = GetAutoResolution();
                    PlayerPrefs.SetInt(ResolutionPrefKey, -1);
                }

                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
                if (selectedResolutionIndex >= 0)
                {
                    SetMenuHint($"Resolution applied: {resolution.width} x {resolution.height}");
                }
                else
                {
                    SetMenuHint($"Auto resolution applied: {resolution.width} x {resolution.height}");
                }
            }
            else if (Application.isMobilePlatform)
            {
                SetMenuHint("Volume saved. Mobile resolution is managed by the system.");
            }
            else
            {
                SetMenuHint("Settings saved.");
            }

            PlayerPrefs.Save();
            CloseSettingsPanel();
        }

        private void OnSettingsCloseClicked()
        {
            PlayerPrefs.Save();
            CloseSettingsPanel();
            if (gameManager != null && gameManager.State == GameState.Paused)
            {
                SetMenuHint("Game paused.");
            }
            else if (gameManager != null && gameManager.State == GameState.Menu)
            {
                SetMenuHint("Choose a loadout, then begin the descent.");
            }
            else
            {
                SetMenuHint("Settings closed.");
            }
        }

        private void OnSettingsMainMenuClicked()
        {
            OnPauseMainMenuClicked();
        }

        private void OnSettingsVolumeChanged(ChangeEvent<float> evt)
        {
            if (suppressSettingsEvents)
            {
                return;
            }

            ApplyMasterVolume(evt.newValue, true);
            RefreshVolumeValueText(evt.newValue);
        }

        private void OnSettingsResolutionChanged(ChangeEvent<string> evt)
        {
            if (suppressSettingsEvents)
            {
                return;
            }

            int dropdownIndex = availableResolutionLabels.IndexOf(evt.newValue);
            if (dropdownIndex <= 0)
            {
                selectedResolutionIndex = -1;
                return;
            }

            selectedResolutionIndex = dropdownIndex - 1;
            if (selectedResolutionIndex >= availableResolutions.Count)
            {
                selectedResolutionIndex = -1;
            }
        }

        private void BuildCodexPage()
        {
            if (codexListElement == null || codexProgressLabel == null)
            {
                return;
            }

            codexListElement.Clear();
            CodexDatabase database = GetCodexDatabase();
            IReadOnlyList<CodexEntry> entries = database != null ? database.GetEntries(currentCodexCategory) : null;
            int unlockedCount = 0;
            int totalCollectedCount = 0;
            int totalCount = entries != null ? entries.Count : 0;

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    CodexEntry entry = entries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    bool unlocked = RunProgressStore.IsCodexEntryUnlocked(currentCodexCategory, entry.id);
                    int ownedCount = RunProgressStore.GetCodexEntryCount(currentCodexCategory, entry.id);
                    if (unlocked)
                    {
                        unlockedCount++;
                    }

                    totalCollectedCount += ownedCount;
                    codexListElement.Add(CreateCodexEntryElement(entry, currentCodexCategory, unlocked, ownedCount));
                }
            }

            codexProgressLabel.text = currentCodexCategory == CodexCategory.Collection
                ? $"Unlocked {unlockedCount}/{Mathf.Max(1, totalCount)} | Total Collected: {totalCollectedCount}"
                : $"Unlocked {unlockedCount}/{Mathf.Max(1, totalCount)}";
        }

        private CodexDatabase GetCodexDatabase()
        {
            if (codexDatabase == null)
            {
                codexDatabase = CodexDatabase.Load();
            }

            return codexDatabase;
        }

        private void SetCodexCategory(CodexCategory category)
        {
            currentCodexCategory = category;
            UpdateCodexTabVisuals();
            BuildCodexPage();
        }

        private void UpdateCodexTabVisuals()
        {
            SetTabActive(codexTabCreaturesButton, currentCodexCategory == CodexCategory.Creature);
            SetTabActive(codexTabObstaclesButton, currentCodexCategory == CodexCategory.Obstacle);
            SetTabActive(codexTabCollectionsButton, currentCodexCategory == CodexCategory.Collection);
        }

        private static void SetTabActive(VisualElement button, bool active)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList(TabActiveClass, active);
        }

        private VisualElement CreateCodexEntryElement(CodexEntry entry, CodexCategory category, bool unlocked, int ownedCount)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("collection-entry");
            if (!unlocked)
            {
                row.AddToClassList(LockedClass);
            }

            string title = unlocked && !string.IsNullOrWhiteSpace(entry.title) ? entry.title : "???";
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("collection-entry-title");
            row.Add(titleLabel);

            string statusText;
            if (unlocked)
            {
                statusText = category == CodexCategory.Collection
                    ? $"Collected x{Mathf.Max(1, ownedCount)}"
                    : "Unlocked";
            }
            else
            {
                string unlockHint = string.IsNullOrWhiteSpace(entry.unlockHint)
                    ? "Keep exploring."
                    : entry.unlockHint;
                statusText = $"Locked - {unlockHint}";
            }

            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("collection-entry-status");
            row.Add(statusLabel);

            string description = unlocked
                ? (string.IsNullOrWhiteSpace(entry.description) ? "No description recorded yet." : entry.description)
                : "Keep exploring to unlock this entry.";
            Label descriptionLabel = new Label(description);
            descriptionLabel.AddToClassList("collection-entry-desc");
            row.Add(descriptionLabel);

            return row;
        }

        private void SetMenuHint(string message)
        {
            if (menuHintLabel != null)
            {
                menuHintLabel.text = message;
            }

            if (pauseHintLabel != null)
            {
                pauseHintLabel.text = message;
            }
        }

        private void SetPauseButtonVisible(bool visible)
        {
            if (pauseButton == null)
            {
                return;
            }

            pauseButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetAbilityButtonVisible(bool visible)
        {
            if (abilityActionContainer == null)
            {
                return;
            }

            abilityActionContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetContinueButtonVisible(bool visible)
        {
            if (menuContinueButton == null)
            {
                return;
            }

            menuContinueButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetPauseCollectionButtonVisible(bool visible)
        {
            if (pauseCollectionButton == null)
            {
                return;
            }

            pauseCollectionButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetStartButtonVisible(bool visible)
        {
            if (menuStartButton == null)
            {
                return;
            }

            menuStartButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetPauseActionButtonsVisible(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (pauseMainMenuButton != null)
            {
                pauseMainMenuButton.style.display = display;
            }

            if (pauseExitButton != null)
            {
                pauseExitButton.style.display = display;
            }
        }

        private void SetCodexPanelVisible(bool visible)
        {
            SetDisplay(codexPanelElement, visible);
        }

        private bool HasCodexUi()
        {
            return codexPanelElement != null &&
                   codexProgressLabel != null &&
                   codexListElement != null &&
                   codexCloseButton != null &&
                   codexTabCreaturesButton != null &&
                   codexTabObstaclesButton != null &&
                   codexTabCollectionsButton != null;
        }

        private void CloseSettingsPanel()
        {
            settingsPanelRequested = false;
            SetSettingsPanelVisible(false);
            RefreshMenuPanelVisibility();
        }

        private void CloseCodexPanel()
        {
            codexPanelRequested = false;
            SetCodexPanelVisible(false);
            RefreshMenuPanelVisibility();
        }

        private void CloseOverlaySubpanels()
        {
            CloseSettingsPanel();
            CloseCodexPanel();
        }

        private void RefreshMenuPanelVisibility()
        {
            if (gameManager == null)
            {
                return;
            }

            bool showPauseMenu = pauseMenuRequested && gameManager.State == GameState.Paused;
            bool showMainMenu = gameManager.State == GameState.Menu;
            bool overlayOpen = settingsPanelRequested || codexPanelRequested;
            SetDisplay(menuPanelElement, showMainMenu && !overlayOpen);
            SetDisplay(pausePanelElement, showPauseMenu && !overlayOpen);
        }

        private void SetMenuOptionButtonsVisible(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (menuLeaderboardButton != null)
            {
                menuLeaderboardButton.style.display = display;
            }

            if (menuCollectionButton != null)
            {
                menuCollectionButton.style.display = display;
            }

            if (menuSettingsButton != null)
            {
                menuSettingsButton.style.display = display;
            }

            if (menuExitButton != null)
            {
                menuExitButton.style.display = display;
            }
        }

        private void SetCharacterSelectionVisible(bool visible)
        {
            if (characterSelectionElement == null)
            {
                return;
            }

            characterSelectionElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetMenuTitle(string title)
        {
            if (menuTitleLabel == null)
            {
                return;
            }

            menuTitleLabel.text = title;
        }

        private void ApplySelectedCharacter()
        {
            if (runner == null)
            {
                return;
            }

            CharacterAbilityType abilityType = CharacterAbilityType.None;
            float airJumpImpulse = 8f;
            int airJumpCharges = 1;

            if (TryGetSelectedConfiguredCharacter(out CharacterOption option))
            {
                runner.ApplyCharacter(option.runnerConfig, option.characterSprite);
                abilityType = option.abilityType;
                if (option.airJumpImpulse > 0f)
                {
                    airJumpImpulse = option.airJumpImpulse;
                }

                if (option.airJumpCharges > 0)
                {
                    airJumpCharges = option.airJumpCharges;
                }
            }
            else
            {
                int fallbackIndex = Mathf.Clamp(selectedCharacterIndex, 0, FallbackAbilityTypes.Length - 1);
                abilityType = FallbackAbilityTypes[fallbackIndex];
            }

            CharacterAbilityController abilityController = EnsureRunnerAbilityController();
            abilityController.Configure(abilityType, airJumpImpulse, airJumpCharges);
        }

        private CharacterAbilityController EnsureRunnerAbilityController()
        {
            CharacterAbilityController abilityController = runner.GetComponent<CharacterAbilityController>();
            if (abilityController == null)
            {
                abilityController = runner.gameObject.AddComponent<CharacterAbilityController>();
            }

            return abilityController;
        }

        private void InitializeSelectedCharacterIndex()
        {
            selectedCharacterIndex = Mathf.Clamp(defaultCharacterIndex, 0, GetCharacterCount() - 1);
        }

        private bool HasConfiguredCharacterOptions()
        {
            return characterOptions != null && characterOptions.Length > 0;
        }

        private int GetCharacterCount()
        {
            if (HasConfiguredCharacterOptions())
            {
                return characterOptions.Length;
            }

            return FallbackCharacterNames.Length;
        }

        private bool TryGetSelectedConfiguredCharacter(out CharacterOption option)
        {
            option = default;
            if (!HasConfiguredCharacterOptions())
            {
                return false;
            }

            int index = Mathf.Clamp(selectedCharacterIndex, 0, characterOptions.Length - 1);
            option = characterOptions[index];
            return true;
        }

        private void RefreshCharacterSelectionUI()
        {
            if (characterNameLabel == null || characterDescLabel == null)
            {
                return;
            }

            int count = GetCharacterCount();
            if (count <= 0)
            {
                characterNameLabel.text = "Default Loadout";
                characterDescLabel.text = "No loadout configured. Using default ability.";
                if (characterPrevButton != null)
                {
                    characterPrevButton.SetEnabled(false);
                }

                if (characterNextButton != null)
                {
                    characterNextButton.SetEnabled(false);
                }

                return;
            }

            selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, count - 1);

            string displayName;
            string description;
            if (TryGetSelectedConfiguredCharacter(out CharacterOption option))
            {
                displayName = string.IsNullOrWhiteSpace(option.displayName) ? $"Runner {selectedCharacterIndex + 1}" : option.displayName;
                description = string.IsNullOrWhiteSpace(option.description) ? "Ready to dive." : option.description;
            }
            else
            {
                int fallbackIndex = Mathf.Clamp(selectedCharacterIndex, 0, FallbackCharacterNames.Length - 1);
                displayName = FallbackCharacterNames[fallbackIndex];
                description = FallbackCharacterDescriptions[fallbackIndex];
            }

            characterNameLabel.text = displayName;
            characterDescLabel.text = description;

            bool enableSwitch = count > 1;
            if (characterPrevButton != null)
            {
                characterPrevButton.SetEnabled(enableSwitch);
            }

            if (characterNextButton != null)
            {
                characterNextButton.SetEnabled(enableSwitch);
            }
        }

        private void EnsureSettingsInitialized()
        {
            if (settingsInitialized)
            {
                RefreshSettingsPanel();
                return;
            }

            PopulateResolutionOptions();

            if (PlayerPrefs.HasKey(ResolutionPrefKey))
            {
                int preferredResolutionIndex = PlayerPrefs.GetInt(ResolutionPrefKey, -1);
                if (preferredResolutionIndex >= 0 && preferredResolutionIndex < availableResolutions.Count)
                {
                    selectedResolutionIndex = preferredResolutionIndex;
                }
                else
                {
                    selectedResolutionIndex = -1;
                }
            }
            else
            {
                // 默认使用自动调整模式，适配不同显示器/窗口尺寸。
                selectedResolutionIndex = -1;
            }

            settingsInitialized = true;
            RefreshSettingsPanel();
        }

        private void LoadSavedMasterVolume()
        {
            float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f));
            AudioListener.volume = savedVolume;
        }

        private void ApplyMasterVolume(float volume, bool persist)
        {
            float clamped = Mathf.Clamp01(volume);
            AudioListener.volume = clamped;
            if (!persist)
            {
                return;
            }

            PlayerPrefs.SetFloat(MasterVolumePrefKey, clamped);
        }

        private void RefreshVolumeValueText(float volume)
        {
            if (settingsVolumeValueLabel == null)
            {
                return;
            }

            settingsVolumeValueLabel.text = $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
        }

        private void RefreshSettingsPanel()
        {
            if (settingsVolumeSlider == null || settingsResolutionDropdown == null)
            {
                return;
            }

            suppressSettingsEvents = true;

            float currentVolume = Mathf.Clamp01(AudioListener.volume);
            settingsVolumeSlider.SetValueWithoutNotify(currentVolume);
            RefreshVolumeValueText(currentVolume);

            settingsResolutionDropdown.choices = availableResolutionLabels;
            if (availableResolutionLabels.Count > 0)
            {
                string selectedLabel = AutoResolutionLabel;
                if (selectedResolutionIndex >= 0 && selectedResolutionIndex < availableResolutions.Count)
                {
                    int dropdownIndex = Mathf.Clamp(selectedResolutionIndex + 1, 1, availableResolutionLabels.Count - 1);
                    selectedLabel = availableResolutionLabels[dropdownIndex];
                }

                settingsResolutionDropdown.SetValueWithoutNotify(selectedLabel);
            }

            bool canChangeResolution = SupportsRuntimeResolutionSelection() && availableResolutionLabels.Count > 0;
            settingsResolutionDropdown.SetEnabled(canChangeResolution);
            if (settingsResolutionHintLabel != null)
            {
                if (canChangeResolution)
                {
                    settingsResolutionHintLabel.text = selectedResolutionIndex < 0
                        ? $"Current Resolution: {Screen.width} x {Screen.height} (Auto)"
                        : $"Current Resolution: {Screen.width} x {Screen.height}";
                }
                else if (Application.isMobilePlatform)
                {
                    settingsResolutionHintLabel.text = "Mobile uses system resolution; no manual switch needed.";
                }
                else
                {
                    settingsResolutionHintLabel.text = "No available resolution options detected.";
                }
            }

            suppressSettingsEvents = false;
        }

        private void PopulateResolutionOptions()
        {
            availableResolutions.Clear();
            availableResolutionLabels.Clear();
            availableResolutionLabels.Add(AutoResolutionLabel);

            HashSet<string> uniqueSizes = new HashSet<string>();
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions != null)
            {
                for (int i = 0; i < resolutions.Length; i++)
                {
                    Resolution resolution = resolutions[i];
                    string key = $"{resolution.width}x{resolution.height}";
                    if (!uniqueSizes.Add(key))
                    {
                        continue;
                    }

                    availableResolutions.Add(new ResolutionOption
                    {
                        width = resolution.width,
                        height = resolution.height
                    });
                }
            }

            if (availableResolutions.Count == 0)
            {
                availableResolutions.Add(new ResolutionOption
                {
                    width = Screen.width,
                    height = Screen.height
                });
            }

            availableResolutions.Sort((a, b) =>
            {
                int areaCompare = (a.width * a.height).CompareTo(b.width * b.height);
                if (areaCompare != 0)
                {
                    return areaCompare;
                }

                return a.width.CompareTo(b.width);
            });

            for (int i = 0; i < availableResolutions.Count; i++)
            {
                ResolutionOption option = availableResolutions[i];
                availableResolutionLabels.Add($"{option.width} x {option.height}");
            }
        }

        private ResolutionOption GetAutoResolution()
        {
            if (availableResolutions.Count > 0)
            {
                return availableResolutions[availableResolutions.Count - 1];
            }

            Resolution screenResolution = Screen.currentResolution;
            if (screenResolution.width > 0 && screenResolution.height > 0)
            {
                return new ResolutionOption
                {
                    width = screenResolution.width,
                    height = screenResolution.height
                };
            }

            return new ResolutionOption
            {
                width = Mathf.Max(Screen.width, 1),
                height = Mathf.Max(Screen.height, 1)
            };
        }

        private bool SupportsRuntimeResolutionSelection()
        {
            return !Application.isMobilePlatform;
        }

        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsMainMenuSceneActive()
        {
            Scene active = SceneManager.GetActiveScene();
            return active.IsValid() && string.Equals(active.name, "MainMenuScene", StringComparison.Ordinal);
        }

        private void SetSettingsPanelVisible(bool visible)
        {
            SetDisplay(settingsPanelElement, visible);
        }

        private void ApplySafeAreaIfNeeded()
        {
            if (safeAreaElement == null)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            float panelWidth = uiDocument != null && uiDocument.rootVisualElement != null
                ? uiDocument.rootVisualElement.resolvedStyle.width
                : screenSize.x;
            float panelHeight = uiDocument != null && uiDocument.rootVisualElement != null
                ? uiDocument.rootVisualElement.resolvedStyle.height
                : screenSize.y;
            if (panelWidth <= 0f)
            {
                panelWidth = screenSize.x;
            }

            if (panelHeight <= 0f)
            {
                panelHeight = screenSize.y;
            }

            Vector2 panelSize = new Vector2(panelWidth, panelHeight);
            if (safe == lastSafeArea && screenSize == lastScreenSize && (panelSize - lastPanelSize).sqrMagnitude < 0.01f)
            {
                return;
            }

            float xScale = panelWidth / Mathf.Max(1f, screenSize.x);
            float yScale = panelHeight / Mathf.Max(1f, screenSize.y);

            float left = safe.xMin;
            float right = screenSize.x - safe.xMax;
            float bottom = safe.yMin;
            float top = screenSize.y - safe.yMax;

            safeAreaElement.style.paddingLeft = left * xScale;
            safeAreaElement.style.paddingRight = right * xScale;
            safeAreaElement.style.paddingBottom = bottom * yScale;
            safeAreaElement.style.paddingTop = top * yScale;

            lastSafeArea = safe;
            lastScreenSize = screenSize;
            lastPanelSize = panelSize;
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element == null)
            {
                return;
            }
            element.EnableInClassList(VisibleClass, visible);
        }
    }
}
