using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UITKButton = UnityEngine.UIElements.Button;
using UITKDropdownField = UnityEngine.UIElements.DropdownField;
using UITKSlider = UnityEngine.UIElements.Slider;

namespace EndlessRunner
{
    public class MainMenuSceneController : MonoBehaviour
    {
        [Serializable]
        private struct CollectionEntry
        {
            public string title;
            [TextArea] public string description;
            public string unlockHint;
        }

        private struct ResolutionOption
        {
            public int width;
            public int height;
        }

        private struct AchievementEntry
        {
            public string title;
            public string description;
            public int current;
            public int target;
        }

        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private string gameplaySceneName = "SampleScene";
        [SerializeField] private string panelSettingsResource = "UI/GamePanelSettings";
        [SerializeField] private string visualTreeResource = "UI/MainMenuUI";
        [SerializeField] private string styleSheetResource = "UI/MainMenuUI";
        [SerializeField] private string playButtonName = "mainmenu-play-button";
        [SerializeField] private string leaderboardButtonName = "mainmenu-leaderboard-button";
        [SerializeField] private string collectionButtonName = "mainmenu-collection-button";
        [SerializeField] private string achievementButtonName = "mainmenu-achievement-button";
        [SerializeField] private string settingsButtonName = "mainmenu-settings-button";
        [SerializeField] private string exitButtonName = "mainmenu-exit-button";
        [SerializeField] private string hintLabelName = "mainmenu-hint-label";
        [SerializeField] private string safeAreaElementName = "mainmenu-safe-area";
        [SerializeField] private string collectionOverlayName = "mainmenu-collection-overlay";
        [SerializeField] private string collectionProgressLabelName = "mainmenu-collection-progress-label";
        [SerializeField] private string collectionListName = "mainmenu-collection-list";
        [SerializeField] private string collectionBackButtonName = "mainmenu-collection-back-button";
        [SerializeField] private string collectionCloseButtonName = "mainmenu-collection-close-button";
        [SerializeField] private string achievementOverlayName = "mainmenu-achievement-overlay";
        [SerializeField] private string achievementProgressLabelName = "mainmenu-achievement-progress-label";
        [SerializeField] private string achievementListName = "mainmenu-achievement-list";
        [SerializeField] private string achievementBackButtonName = "mainmenu-achievement-back-button";
        [SerializeField] private string achievementCloseButtonName = "mainmenu-achievement-close-button";
        [SerializeField] private string leaderboardOverlayName = "mainmenu-leaderboard-overlay";
        [SerializeField] private string leaderboardBestScoreLabelName = "mainmenu-best-score-label";
        [SerializeField] private string leaderboardLastScoreLabelName = "mainmenu-last-score-label";
        [SerializeField] private string leaderboardTotalRunsLabelName = "mainmenu-total-runs-label";
        [SerializeField] private string leaderboardAverageScoreLabelName = "mainmenu-average-score-label";
        [SerializeField] private string leaderboardModeProgressLabelName = "mainmenu-mode-progress-label";
        [SerializeField] private string leaderboardTopScoresLabelName = "mainmenu-top-scores-label";
        [SerializeField] private string leaderboardBackButtonName = "mainmenu-leaderboard-back-button";
        [SerializeField] private string leaderboardCloseButtonName = "mainmenu-leaderboard-close-button";
        [SerializeField] private string settingsOverlayName = "mainmenu-settings-overlay";
        [SerializeField] private string settingsVolumeSliderName = "mainmenu-settings-volume-slider";
        [SerializeField] private string settingsVolumeValueLabelName = "mainmenu-settings-volume-value-label";
        [SerializeField] private string settingsResolutionDropdownName = "mainmenu-settings-resolution-dropdown";
        [SerializeField] private string settingsResolutionHintLabelName = "mainmenu-settings-resolution-hint-label";
        [SerializeField] private string gameModeDropdownName = "mainmenu-game-mode-dropdown";
        [SerializeField] private string gameModeHintLabelName = "mainmenu-game-mode-hint-label";
        [SerializeField] private string settingsApplyButtonName = "mainmenu-settings-apply-button";
        [SerializeField] private string settingsBackButtonName = "mainmenu-settings-back-button";
        [SerializeField] private string settingsCloseButtonName = "mainmenu-settings-close-button";
        [SerializeField] private CollectionEntry[] collectionEntries;
        [SerializeField] private bool applySafeArea = true;

        private UIDocument uiDocument;
        private UITKButton playButton;
        private UITKButton leaderboardButton;
        private UITKButton collectionButton;
        private UITKButton achievementButton;
        private UITKButton settingsButton;
        private UITKButton exitButton;
        private UITKButton collectionBackButton;
        private UITKButton collectionCloseButton;
        private UITKButton achievementBackButton;
        private UITKButton achievementCloseButton;
        private UITKButton leaderboardBackButton;
        private UITKButton leaderboardCloseButton;
        private UITKButton settingsApplyButton;
        private UITKButton settingsBackButton;
        private UITKButton settingsCloseButton;
        private Label hintLabel;
        private Label collectionProgressLabel;
        private Label achievementProgressLabel;
        private Label leaderboardBestScoreLabel;
        private Label leaderboardLastScoreLabel;
        private Label leaderboardTotalRunsLabel;
        private Label leaderboardAverageScoreLabel;
        private Label leaderboardModeProgressLabel;
        private Label leaderboardTopScoresLabel;
        private Label settingsVolumeValueLabel;
        private Label settingsResolutionHintLabel;
        private Label gameModeHintLabel;
        private VisualElement safeAreaElement;
        private VisualElement collectionOverlay;
        private VisualElement achievementOverlay;
        private VisualElement leaderboardOverlay;
        private VisualElement settingsOverlay;
        private VisualElement collectionList;
        private VisualElement achievementList;
        private UITKSlider settingsVolumeSlider;
        private UITKDropdownField settingsResolutionDropdown;
        private UITKDropdownField gameModeDropdown;
        private bool isBound;
        private bool isCollectionVisible;
        private bool isAchievementVisible;
        private bool isLeaderboardVisible;
        private bool isSettingsVisible;
        private bool settingsInitialized;
        private bool suppressSettingsEvents;
        private int selectedResolutionIndex = -1;
        private string pendingSelectedModeId = RunProgressStore.ModeClassic;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private Vector2 lastPanelSize = Vector2.zero;
        private readonly List<ResolutionOption> availableResolutions = new List<ResolutionOption>();
        private readonly List<string> availableResolutionLabels = new List<string>();
        private readonly List<string> availableModeIds = new List<string>();
        private readonly List<string> availableModeLabels = new List<string>();
        private const string VisibleClass = "is-visible";
        private const string LockedClass = "is-locked";
        private const string MasterVolumePrefKey = "settings.master_volume";
        private const string ResolutionPrefKey = "settings.resolution_index";
        private const string AutoResolutionLabel = "Auto (Recommended)";

        private void Awake()
        {
            if (!IsMainMenuScene())
            {
                enabled = false;
                return;
            }

            EnsureUiDocument();
        }

        private void OnEnable()
        {
            TryBindButtons();
        }

        private void Start()
        {
            TryBindButtons();
        }

        private void Update()
        {
            if (!isBound)
            {
                TryBindButtons();
                return;
            }

            if (IsEscapePressed())
            {
                if (isSettingsVisible)
                {
                    SetSettingsOverlayVisible(false);
                    SetHint("Settings closed.");
                }
                else if (isAchievementVisible)
                {
                    SetAchievementOverlayVisible(false);
                    SetHint("Achievements closed.");
                }
                else if (isCollectionVisible)
                {
                    SetCollectionOverlayVisible(false);
                    SetHint("Collection closed.");
                }
                else if (isLeaderboardVisible)
                {
                    SetLeaderboardOverlayVisible(false);
                    SetHint("Leaderboard closed.");
                }
            }

            if (applySafeArea)
            {
                ApplySafeAreaIfNeeded();
            }
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private bool IsMainMenuScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && string.Equals(activeScene.name, mainMenuSceneName, StringComparison.Ordinal);
        }

        private void EnsureUiDocument()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            if (uiDocument.panelSettings == null && !string.IsNullOrEmpty(panelSettingsResource))
            {
                uiDocument.panelSettings = Resources.Load<PanelSettings>(panelSettingsResource);
            }

            if (uiDocument.visualTreeAsset == null && !string.IsNullOrEmpty(visualTreeResource))
            {
                uiDocument.visualTreeAsset = Resources.Load<VisualTreeAsset>(visualTreeResource);
            }
        }

        private void TryBindButtons()
        {
            if (isBound)
            {
                return;
            }

            EnsureUiDocument();
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (!string.IsNullOrEmpty(styleSheetResource))
            {
                StyleSheet sheet = Resources.Load<StyleSheet>(styleSheetResource);
                if (sheet != null && !root.styleSheets.Contains(sheet))
                {
                    root.styleSheets.Add(sheet);
                }
            }

            playButton = root.Q<UITKButton>(playButtonName);
            leaderboardButton = root.Q<UITKButton>(leaderboardButtonName);
            collectionButton = root.Q<UITKButton>(collectionButtonName);
            achievementButton = root.Q<UITKButton>(achievementButtonName);
            settingsButton = root.Q<UITKButton>(settingsButtonName);
            exitButton = root.Q<UITKButton>(exitButtonName);
            collectionBackButton = root.Q<UITKButton>(collectionBackButtonName);
            collectionCloseButton = root.Q<UITKButton>(collectionCloseButtonName);
            achievementBackButton = root.Q<UITKButton>(achievementBackButtonName);
            achievementCloseButton = root.Q<UITKButton>(achievementCloseButtonName);
            leaderboardBackButton = root.Q<UITKButton>(leaderboardBackButtonName);
            leaderboardCloseButton = root.Q<UITKButton>(leaderboardCloseButtonName);
            settingsApplyButton = root.Q<UITKButton>(settingsApplyButtonName);
            settingsBackButton = root.Q<UITKButton>(settingsBackButtonName);
            settingsCloseButton = root.Q<UITKButton>(settingsCloseButtonName);
            hintLabel = root.Q<Label>(hintLabelName);
            collectionProgressLabel = root.Q<Label>(collectionProgressLabelName);
            achievementProgressLabel = root.Q<Label>(achievementProgressLabelName);
            leaderboardBestScoreLabel = root.Q<Label>(leaderboardBestScoreLabelName);
            leaderboardLastScoreLabel = root.Q<Label>(leaderboardLastScoreLabelName);
            leaderboardTotalRunsLabel = root.Q<Label>(leaderboardTotalRunsLabelName);
            leaderboardAverageScoreLabel = root.Q<Label>(leaderboardAverageScoreLabelName);
            leaderboardModeProgressLabel = root.Q<Label>(leaderboardModeProgressLabelName);
            leaderboardTopScoresLabel = root.Q<Label>(leaderboardTopScoresLabelName);
            settingsVolumeValueLabel = root.Q<Label>(settingsVolumeValueLabelName);
            settingsResolutionHintLabel = root.Q<Label>(settingsResolutionHintLabelName);
            gameModeHintLabel = root.Q<Label>(gameModeHintLabelName);
            safeAreaElement = root.Q<VisualElement>(safeAreaElementName);
            collectionOverlay = root.Q<VisualElement>(collectionOverlayName);
            achievementOverlay = root.Q<VisualElement>(achievementOverlayName);
            leaderboardOverlay = root.Q<VisualElement>(leaderboardOverlayName);
            settingsOverlay = root.Q<VisualElement>(settingsOverlayName);
            collectionList = root.Q<VisualElement>(collectionListName);
            achievementList = root.Q<VisualElement>(achievementListName);
            settingsVolumeSlider = root.Q<UITKSlider>(settingsVolumeSliderName);
            settingsResolutionDropdown = root.Q<UITKDropdownField>(settingsResolutionDropdownName);
            gameModeDropdown = root.Q<UITKDropdownField>(gameModeDropdownName);
            if (playButton == null || leaderboardButton == null || collectionButton == null || achievementButton == null || settingsButton == null || exitButton == null)
            {
                return;
            }

            playButton.clicked -= OnPlayClicked;
            playButton.clicked += OnPlayClicked;
            leaderboardButton.clicked -= OnLeaderboardClicked;
            leaderboardButton.clicked += OnLeaderboardClicked;
            collectionButton.clicked -= OnCollectionClicked;
            collectionButton.clicked += OnCollectionClicked;
            achievementButton.clicked -= OnAchievementClicked;
            achievementButton.clicked += OnAchievementClicked;
            if (collectionBackButton != null)
            {
                collectionBackButton.clicked -= OnBackToMainInterfaceClicked;
                collectionBackButton.clicked += OnBackToMainInterfaceClicked;
            }
            if (collectionCloseButton != null)
            {
                collectionCloseButton.clicked -= OnCollectionCloseClicked;
                collectionCloseButton.clicked += OnCollectionCloseClicked;
            }
            if (achievementBackButton != null)
            {
                achievementBackButton.clicked -= OnBackToMainInterfaceClicked;
                achievementBackButton.clicked += OnBackToMainInterfaceClicked;
            }
            if (achievementCloseButton != null)
            {
                achievementCloseButton.clicked -= OnAchievementCloseClicked;
                achievementCloseButton.clicked += OnAchievementCloseClicked;
            }
            if (leaderboardBackButton != null)
            {
                leaderboardBackButton.clicked -= OnBackToMainInterfaceClicked;
                leaderboardBackButton.clicked += OnBackToMainInterfaceClicked;
            }
            if (leaderboardCloseButton != null)
            {
                leaderboardCloseButton.clicked -= OnLeaderboardCloseClicked;
                leaderboardCloseButton.clicked += OnLeaderboardCloseClicked;
            }
            settingsButton.clicked -= OnSettingsClicked;
            settingsButton.clicked += OnSettingsClicked;
            if (settingsApplyButton != null)
            {
                settingsApplyButton.clicked -= OnSettingsApplyClicked;
                settingsApplyButton.clicked += OnSettingsApplyClicked;
            }
            if (settingsBackButton != null)
            {
                settingsBackButton.clicked -= OnBackToMainInterfaceClicked;
                settingsBackButton.clicked += OnBackToMainInterfaceClicked;
            }
            if (settingsCloseButton != null)
            {
                settingsCloseButton.clicked -= OnSettingsCloseClicked;
                settingsCloseButton.clicked += OnSettingsCloseClicked;
            }
            if (settingsVolumeSlider != null)
            {
                settingsVolumeSlider.UnregisterValueChangedCallback(OnSettingsVolumeChanged);
                settingsVolumeSlider.RegisterValueChangedCallback(OnSettingsVolumeChanged);
            }
            if (settingsResolutionDropdown != null)
            {
                settingsResolutionDropdown.UnregisterValueChangedCallback(OnSettingsResolutionChanged);
                settingsResolutionDropdown.RegisterValueChangedCallback(OnSettingsResolutionChanged);
            }
            if (gameModeDropdown != null)
            {
                gameModeDropdown.UnregisterValueChangedCallback(OnGameModeChanged);
                gameModeDropdown.RegisterValueChangedCallback(OnGameModeChanged);
            }

            exitButton.clicked -= OnExitClicked;
            exitButton.clicked += OnExitClicked;
            BuildCollectionPage();
            BuildAchievementPage();
            RefreshLeaderboardPanel();
            EnsureSettingsInitialized();
            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            SetHint("Select an option.");
            ApplySafeAreaIfNeeded();
            isBound = true;
        }

        private void UnbindButtons()
        {
            if (playButton != null)
            {
                playButton.clicked -= OnPlayClicked;
            }

            if (exitButton != null)
            {
                exitButton.clicked -= OnExitClicked;
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.clicked -= OnLeaderboardClicked;
            }

            if (collectionButton != null)
            {
                collectionButton.clicked -= OnCollectionClicked;
            }

            if (achievementButton != null)
            {
                achievementButton.clicked -= OnAchievementClicked;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked -= OnSettingsClicked;
            }

            if (collectionCloseButton != null)
            {
                collectionCloseButton.clicked -= OnCollectionCloseClicked;
            }
            if (collectionBackButton != null)
            {
                collectionBackButton.clicked -= OnBackToMainInterfaceClicked;
            }

            if (achievementCloseButton != null)
            {
                achievementCloseButton.clicked -= OnAchievementCloseClicked;
            }
            if (achievementBackButton != null)
            {
                achievementBackButton.clicked -= OnBackToMainInterfaceClicked;
            }

            if (leaderboardCloseButton != null)
            {
                leaderboardCloseButton.clicked -= OnLeaderboardCloseClicked;
            }
            if (leaderboardBackButton != null)
            {
                leaderboardBackButton.clicked -= OnBackToMainInterfaceClicked;
            }

            if (settingsApplyButton != null)
            {
                settingsApplyButton.clicked -= OnSettingsApplyClicked;
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.clicked -= OnSettingsCloseClicked;
            }
            if (settingsBackButton != null)
            {
                settingsBackButton.clicked -= OnBackToMainInterfaceClicked;
            }

            if (settingsVolumeSlider != null)
            {
                settingsVolumeSlider.UnregisterValueChangedCallback(OnSettingsVolumeChanged);
            }

            if (settingsResolutionDropdown != null)
            {
                settingsResolutionDropdown.UnregisterValueChangedCallback(OnSettingsResolutionChanged);
            }
            if (gameModeDropdown != null)
            {
                gameModeDropdown.UnregisterValueChangedCallback(OnGameModeChanged);
            }

            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            isBound = false;
        }

        private void OnPlayClicked()
        {
            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.LoadGameplayScene();
                return;
            }

            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
        }

        private void OnExitClicked()
        {
            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
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

        private void OnLeaderboardClicked()
        {
            RefreshLeaderboardPanel();
            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            SetLeaderboardOverlayVisible(true);
            SetHint("Leaderboard opened.");
        }

        private void OnCollectionClicked()
        {
            BuildCollectionPage();
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            SetCollectionOverlayVisible(true);
            SetHint("Collection opened.");
        }

        private void OnAchievementClicked()
        {
            BuildAchievementPage();
            SetCollectionOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            SetAchievementOverlayVisible(true);
            SetHint("Achievements opened.");
        }

        private void OnCollectionCloseClicked()
        {
            SetCollectionOverlayVisible(false);
            SetHint("Collection closed.");
        }

        private void OnAchievementCloseClicked()
        {
            SetAchievementOverlayVisible(false);
            SetHint("Achievements closed.");
        }

        private void OnBackToMainInterfaceClicked()
        {
            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(false);
            SetHint("Returned to main interface.");
        }

        private void OnLeaderboardCloseClicked()
        {
            SetLeaderboardOverlayVisible(false);
            SetHint("Leaderboard closed.");
        }

        private void OnSettingsClicked()
        {
            EnsureSettingsInitialized();
            RefreshSettingsPanel();
            SetCollectionOverlayVisible(false);
            SetAchievementOverlayVisible(false);
            SetLeaderboardOverlayVisible(false);
            SetSettingsOverlayVisible(true);
            SetHint("Settings opened.");
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
            }
            else
            {
                PlayerPrefs.SetInt(ResolutionPrefKey, -1);
            }

            if (!RunProgressStore.TrySetSelectedMode(pendingSelectedModeId, out string reason))
            {
                PlayerPrefs.Save();
                SetHint(reason);
                RefreshSettingsPanel();
                return;
            }

            PlayerPrefs.Save();
            SetSettingsOverlayVisible(false);
            SetHint("Settings saved.");
        }

        private void OnSettingsCloseClicked()
        {
            SetSettingsOverlayVisible(false);
            SetHint("Settings closed.");
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

        private void OnGameModeChanged(ChangeEvent<string> evt)
        {
            if (suppressSettingsEvents)
            {
                return;
            }

            int index = availableModeLabels.IndexOf(evt.newValue);
            if (index < 0 || index >= availableModeIds.Count)
            {
                return;
            }

            pendingSelectedModeId = availableModeIds[index];
            RefreshGameModeHint();
        }

        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void SetCollectionOverlayVisible(bool visible)
        {
            isCollectionVisible = visible;
            if (collectionOverlay == null)
            {
                return;
            }

            collectionOverlay.EnableInClassList(VisibleClass, visible);
        }

        private void SetAchievementOverlayVisible(bool visible)
        {
            isAchievementVisible = visible;
            if (achievementOverlay == null)
            {
                return;
            }

            achievementOverlay.EnableInClassList(VisibleClass, visible);
        }

        private void SetLeaderboardOverlayVisible(bool visible)
        {
            isLeaderboardVisible = visible;
            if (leaderboardOverlay == null)
            {
                return;
            }

            leaderboardOverlay.EnableInClassList(VisibleClass, visible);
        }

        private void SetSettingsOverlayVisible(bool visible)
        {
            isSettingsVisible = visible;
            if (settingsOverlay == null)
            {
                return;
            }

            settingsOverlay.EnableInClassList(VisibleClass, visible);
        }

        private void BuildCollectionPage()
        {
            if (collectionList == null || collectionProgressLabel == null)
            {
                return;
            }

            collectionList.Clear();
            CollectionEntry[] entries = GetCollectionEntries();
            int unlockedCount = 0;
            int totalCollectedCount = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                CollectionEntry entry = entries[i];
                bool unlocked = RunProgressStore.IsCollectionEntryUnlocked(i);
                int ownedCount = RunProgressStore.GetCollectionEntryCount(i);
                if (unlocked)
                {
                    unlockedCount++;
                }

                totalCollectedCount += ownedCount;
                collectionList.Add(CreateCollectionEntryElement(entry, unlocked, ownedCount));
            }

            collectionProgressLabel.text = $"Unlocked {unlockedCount}/{entries.Length} | Total Collected: {totalCollectedCount}";
        }

        private CollectionEntry[] GetCollectionEntries()
        {
            if (collectionEntries != null && collectionEntries.Length > 0)
            {
                return collectionEntries;
            }

            return new[]
            {
                new CollectionEntry
                {
                    title = "Ancient Coin",
                    description = "A bronze coin from the old abyss expedition.",
                    unlockHint = "Find at Depth 120"
                },
                new CollectionEntry
                {
                    title = "Crystal Core",
                    description = "A condensed crystal shard with unstable energy.",
                    unlockHint = "Find at Depth 280"
                },
                new CollectionEntry
                {
                    title = "Worn Compass",
                    description = "Still points down, no matter where you stand.",
                    unlockHint = "Find at Depth 450"
                },
                new CollectionEntry
                {
                    title = "Abyss Emblem",
                    description = "Badge from the first successful deep run.",
                    unlockHint = "Find at Depth 650"
                },
                new CollectionEntry
                {
                    title = "Void Fragment",
                    description = "A fragment that bends light around its edges.",
                    unlockHint = "Find at Depth 900"
                },
                new CollectionEntry
                {
                    title = "Crown of Depths",
                    description = "A symbolic trophy for elite runners.",
                    unlockHint = "Find at Depth 1200"
                }
            };
        }

        private void BuildAchievementPage()
        {
            if (achievementList == null || achievementProgressLabel == null)
            {
                return;
            }

            achievementList.Clear();
            AchievementEntry[] entries = BuildAchievementEntries();
            int completedCount = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                AchievementEntry entry = entries[i];
                bool completed = entry.current >= entry.target;
                if (completed)
                {
                    completedCount++;
                }

                achievementList.Add(CreateAchievementEntryElement(entry, completed));
            }

            achievementProgressLabel.text = $"Completed {completedCount}/{entries.Length}";
        }

        private AchievementEntry[] BuildAchievementEntries()
        {
            int totalRuns = RunProgressStore.GetTotalRuns();
            int bestScore = RunProgressStore.GetBestScore();
            int unlockedCollections = RunProgressStore.GetUnlockedCollectionCount();
            int totalCollectedRelics = RunProgressStore.GetTotalCollectionPickups();
            int totalCollectionEntries = Mathf.Max(1, GetCollectionEntries().Length);

            List<RunProgressStore.GameModeProgress> modeProgress = RunProgressStore.GetGameModeProgress();
            int unlockedModes = 0;
            for (int i = 0; i < modeProgress.Count; i++)
            {
                if (modeProgress[i].Unlocked)
                {
                    unlockedModes++;
                }
            }

            int totalModes = Mathf.Max(1, modeProgress.Count);
            return new[]
            {
                new AchievementEntry
                {
                    title = "First Dive",
                    description = "Complete your first run.",
                    current = totalRuns,
                    target = 1
                },
                new AchievementEntry
                {
                    title = "Seasoned Runner",
                    description = "Complete 10 runs.",
                    current = totalRuns,
                    target = 10
                },
                new AchievementEntry
                {
                    title = "Deep Scout",
                    description = "Reach a best score of 600.",
                    current = bestScore,
                    target = 600
                },
                new AchievementEntry
                {
                    title = "Abyss Challenger",
                    description = "Reach a best score of 1200.",
                    current = bestScore,
                    target = 1200
                },
                new AchievementEntry
                {
                    title = "Collector",
                    description = "Unlock all collection entries.",
                    current = unlockedCollections,
                    target = totalCollectionEntries
                },
                new AchievementEntry
                {
                    title = "Relic Hoarder",
                    description = "Collect 15 relics in total.",
                    current = totalCollectedRelics,
                    target = 15
                },
                new AchievementEntry
                {
                    title = "Mode Pioneer",
                    description = "Unlock all game modes.",
                    current = unlockedModes,
                    target = totalModes
                }
            };
        }

        private VisualElement CreateAchievementEntryElement(AchievementEntry entry, bool completed)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("collection-entry");
            row.AddToClassList("achievement-entry");
            if (!completed)
            {
                row.AddToClassList(LockedClass);
            }

            Label titleLabel = new Label(entry.title);
            titleLabel.AddToClassList("collection-entry-title");
            row.Add(titleLabel);

            string statusText = completed
                ? "Completed"
                : $"Progress: {Mathf.Clamp(entry.current, 0, entry.target)}/{entry.target}";
            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("collection-entry-status");
            row.Add(statusLabel);

            Label descriptionLabel = new Label(entry.description);
            descriptionLabel.AddToClassList("collection-entry-desc");
            row.Add(descriptionLabel);

            return row;
        }

        private VisualElement CreateCollectionEntryElement(CollectionEntry entry, bool unlocked, int ownedCount)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("collection-entry");
            if (!unlocked)
            {
                row.AddToClassList(LockedClass);
            }

            Label titleLabel = new Label(unlocked ? entry.title : "???");
            titleLabel.AddToClassList("collection-entry-title");
            row.Add(titleLabel);

            string statusText = unlocked
                ? $"Collected x{Mathf.Max(1, ownedCount)}"
                : $"Locked - {entry.unlockHint}";
            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("collection-entry-status");
            row.Add(statusLabel);

            Label descriptionLabel = new Label(unlocked ? entry.description : "Keep exploring to unlock this collectible.");
            descriptionLabel.AddToClassList("collection-entry-desc");
            row.Add(descriptionLabel);

            return row;
        }

        private void RefreshLeaderboardPanel()
        {
            int bestScore = RunProgressStore.GetBestScore();
            int lastScore = RunProgressStore.GetLastScore();
            int totalRuns = RunProgressStore.GetTotalRuns();
            float averageScore = RunProgressStore.GetAverageScore();
            List<RunProgressStore.GameModeProgress> modeProgress = RunProgressStore.GetGameModeProgress();
            List<RunProgressStore.LeaderboardEntry> topEntries = RunProgressStore.GetLeaderboardEntries(5);

            if (leaderboardBestScoreLabel != null)
            {
                leaderboardBestScoreLabel.text = $"Best Score: {bestScore}";
            }

            if (leaderboardLastScoreLabel != null)
            {
                leaderboardLastScoreLabel.text = $"Last Score: {lastScore}";
            }

            if (leaderboardTotalRunsLabel != null)
            {
                leaderboardTotalRunsLabel.text = $"Total Runs: {totalRuns}";
            }

            if (leaderboardAverageScoreLabel != null)
            {
                leaderboardAverageScoreLabel.text = $"Average Score: {Mathf.RoundToInt(averageScore)}";
            }

            if (leaderboardModeProgressLabel != null)
            {
                List<string> status = new List<string>(modeProgress.Count);
                for (int i = 0; i < modeProgress.Count; i++)
                {
                    RunProgressStore.GameModeProgress item = modeProgress[i];
                    string suffix = item.Unlocked ? "Unlocked" : $"{Mathf.RoundToInt(item.Progress01 * 100f)}%";
                    status.Add($"{item.DisplayName}: {suffix}");
                }

                leaderboardModeProgressLabel.text = $"Modes: {string.Join(" | ", status)}";
            }

            if (leaderboardTopScoresLabel != null)
            {
                if (topEntries.Count == 0)
                {
                    leaderboardTopScoresLabel.text = "Top Scores: none yet";
                }
                else
                {
                    List<string> topTexts = new List<string>(topEntries.Count);
                    for (int i = 0; i < topEntries.Count; i++)
                    {
                        RunProgressStore.LeaderboardEntry entry = topEntries[i];
                        string modeName = RunProgressStore.GetModeDisplayName(entry.modeId);
                        topTexts.Add($"#{i + 1} {entry.score} ({modeName})");
                    }

                    leaderboardTopScoresLabel.text = $"Top Scores: {string.Join(", ", topTexts)}";
                }
            }
        }

        private void EnsureSettingsInitialized()
        {
            if (settingsVolumeSlider == null || settingsResolutionDropdown == null || gameModeDropdown == null)
            {
                return;
            }

            if (settingsInitialized)
            {
                RefreshSettingsPanel();
                return;
            }

            PopulateResolutionOptions();
            PopulateGameModeOptions();
            if (PlayerPrefs.HasKey(ResolutionPrefKey))
            {
                int savedIndex = PlayerPrefs.GetInt(ResolutionPrefKey, -1);
                if (savedIndex >= 0 && savedIndex < availableResolutions.Count)
                {
                    selectedResolutionIndex = savedIndex;
                }
                else
                {
                    selectedResolutionIndex = -1;
                }
            }
            else
            {
                selectedResolutionIndex = -1;
            }

            pendingSelectedModeId = RunProgressStore.GetSelectedModeId();
            float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f));
            AudioListener.volume = savedVolume;
            settingsInitialized = true;
            RefreshSettingsPanel();
        }

        private void RefreshSettingsPanel()
        {
            if (settingsVolumeSlider == null || settingsResolutionDropdown == null || gameModeDropdown == null)
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
                    settingsResolutionHintLabel.text = "Mobile uses system resolution.";
                }
                else
                {
                    settingsResolutionHintLabel.text = "No available resolution options detected.";
                }
            }

            PopulateGameModeOptions();
            gameModeDropdown.choices = availableModeLabels;
            if (availableModeIds.Count > 0)
            {
                int selectedModeIndex = availableModeIds.IndexOf(pendingSelectedModeId);
                if (selectedModeIndex < 0)
                {
                    selectedModeIndex = 0;
                    pendingSelectedModeId = availableModeIds[selectedModeIndex];
                }

                string selectedModeLabel = availableModeLabels[selectedModeIndex];
                gameModeDropdown.SetValueWithoutNotify(selectedModeLabel);
            }

            RefreshGameModeHint();
            suppressSettingsEvents = false;
        }

        private void RefreshVolumeValueText(float volume)
        {
            if (settingsVolumeValueLabel == null)
            {
                return;
            }

            settingsVolumeValueLabel.text = $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
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

        private void PopulateGameModeOptions()
        {
            availableModeIds.Clear();
            availableModeLabels.Clear();

            List<RunProgressStore.GameModeProgress> modes = RunProgressStore.GetGameModeProgress();
            for (int i = 0; i < modes.Count; i++)
            {
                RunProgressStore.GameModeProgress mode = modes[i];
                availableModeIds.Add(mode.ModeId);
                string label = mode.Unlocked
                    ? $"{mode.DisplayName} (Unlocked)"
                    : $"{mode.DisplayName} (Locked)";
                availableModeLabels.Add(label);
            }
        }

        private void RefreshGameModeHint()
        {
            if (gameModeHintLabel == null)
            {
                return;
            }

            List<RunProgressStore.GameModeProgress> modes = RunProgressStore.GetGameModeProgress();
            for (int i = 0; i < modes.Count; i++)
            {
                RunProgressStore.GameModeProgress mode = modes[i];
                if (!string.Equals(mode.ModeId, pendingSelectedModeId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (mode.Unlocked)
                {
                    gameModeHintLabel.text = $"Current mode: {mode.DisplayName}.";
                }
                else
                {
                    gameModeHintLabel.text =
                        $"Locked: need Best {mode.RequiredBestScore}, Runs {mode.RequiredRuns}. Current {mode.CurrentBestScore}/{mode.CurrentRuns}.";
                }
                return;
            }

            gameModeHintLabel.text = "Select a mode.";
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

        private void SetHint(string message)
        {
            if (hintLabel == null)
            {
                return;
            }

            hintLabel.text = message;
        }

        private void ApplySafeAreaIfNeeded()
        {
            if (safeAreaElement == null || uiDocument == null || uiDocument.rootVisualElement == null)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            float panelWidth = uiDocument.rootVisualElement.resolvedStyle.width;
            float panelHeight = uiDocument.rootVisualElement.resolvedStyle.height;
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

            float left = safe.xMin * xScale;
            float right = (screenSize.x - safe.xMax) * xScale;
            float bottom = safe.yMin * yScale;
            float top = (screenSize.y - safe.yMax) * yScale;

            safeAreaElement.style.paddingLeft = left;
            safeAreaElement.style.paddingRight = right;
            safeAreaElement.style.paddingBottom = bottom;
            safeAreaElement.style.paddingTop = top;

            lastSafeArea = safe;
            lastScreenSize = screenSize;
            lastPanelSize = panelSize;
        }
    }
}
