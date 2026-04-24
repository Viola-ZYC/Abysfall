using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

        private struct AchievementEntry
        {
            public string title;
            public string description;
            public int current;
            public int target;
        }

        private enum MainMenuOverlay
        {
            None,
            Manual,
            Achievements,
            Leaderboard,
            Settings
        }

        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private string gameplaySceneName = "SampleScene";
        [SerializeField] private string panelSettingsResource = "UI/GamePanelSettings";
        [SerializeField] private string visualTreeResource = "UI/MainMenuUI";
        [SerializeField] private string styleSheetResource = "UI/MainMenuUI";
        [SerializeField] private string mainMenuCardName = "mainmenu-card";
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
        [SerializeField] private string manualTabCreaturesButtonName = "mainmenu-manual-tab-creatures";
        [SerializeField] private string manualTabCollectionsButtonName = "mainmenu-manual-tab-collections";
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
        [SerializeField] private string gameModeDropdownName = "mainmenu-game-mode-dropdown";
        [SerializeField] private string gameModeHintLabelName = "mainmenu-game-mode-hint-label";
        [SerializeField] private string settingsApplyButtonName = "mainmenu-settings-apply-button";
        [SerializeField] private string settingsBackButtonName = "mainmenu-settings-back-button";
        [SerializeField] private string settingsCloseButtonName = "mainmenu-settings-close-button";
        [SerializeField] private string tutorialButtonName = "mainmenu-tutorial-button";
        [SerializeField] private CollectionEntry[] collectionEntries;
        [SerializeField] private CodexDatabase codexDatabase;
        [SerializeField] private bool applySafeArea = true;
        [SerializeField] private float glowMinInterval = 1.6f;
        [SerializeField] private float glowMaxInterval = 3.2f;
        [SerializeField] private float glowDuration = 0.12f;

        private UIDocument uiDocument;
        private UITKButton playButton;
        private UITKButton leaderboardButton;
        private UITKButton collectionButton;
        private UITKButton achievementButton;
        private UITKButton settingsButton;
        private UITKButton exitButton;
        private UITKButton collectionBackButton;
        private UITKButton collectionCloseButton;
        private UITKButton manualTabCreaturesButton;
        private UITKButton manualTabCollectionsButton;
        private UITKButton achievementBackButton;
        private UITKButton achievementCloseButton;
        private UITKButton leaderboardBackButton;
        private UITKButton leaderboardCloseButton;
        private UITKButton settingsApplyButton;
        private UITKButton settingsBackButton;
        private UITKButton settingsCloseButton;
        private UITKButton tutorialButton;
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
        private Label gameModeHintLabel;
        private VisualElement safeAreaElement;
        private VisualElement mainMenuCard;
        private VisualElement collectionOverlay;
        private VisualElement achievementOverlay;
        private VisualElement leaderboardOverlay;
        private VisualElement settingsOverlay;
        private VisualElement collectionList;
        private VisualElement achievementList;
        private UITKSlider settingsVolumeSlider;
        private UITKDropdownField gameModeDropdown;
        private bool isBound;
        private bool settingsInitialized;
        private bool suppressSettingsEvents;
        private CodexCategory currentManualCategory = CodexCategory.Creature;
        private MainMenuOverlay activeOverlay = MainMenuOverlay.None;
        private string pendingSelectedModeId = RunProgressStore.ModeClassic;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private Vector2 lastPanelSize = Vector2.zero;
        private readonly List<string> availableModeIds = new List<string>();
        private readonly List<string> availableModeLabels = new List<string>();
        private const string VisibleClass = "is-visible";
        private const string LockedClass = "is-locked";
        private const string GlowClass = "is-glow";
        private const string TabActiveClass = "is-active";
        private Coroutine glowRoutine;
        private const string MasterVolumePrefKey = "settings.master_volume";

        private void Awake()
        {
            if (!IsMainMenuScene())
            {
                enabled = false;
                return;
            }

            EnsureEventSystem();
            EnsureUiDocument();
        }

        private void OnEnable()
        {
            EnsureEventSystem();
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

            HandleOverlayBackInput();

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
            if (uiDocument != null &&
                (uiDocument.gameObject == gameObject || string.Equals(uiDocument.gameObject.name, "MainMenuUI", StringComparison.Ordinal)))
            {
                return;
            }

            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = UIDocumentLocator.FindMainMenuDocument();
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
            manualTabCreaturesButton = root.Q<UITKButton>(manualTabCreaturesButtonName);
            manualTabCollectionsButton = root.Q<UITKButton>(manualTabCollectionsButtonName);
            achievementBackButton = root.Q<UITKButton>(achievementBackButtonName);
            achievementCloseButton = root.Q<UITKButton>(achievementCloseButtonName);
            leaderboardBackButton = root.Q<UITKButton>(leaderboardBackButtonName);
            leaderboardCloseButton = root.Q<UITKButton>(leaderboardCloseButtonName);
            settingsApplyButton = root.Q<UITKButton>(settingsApplyButtonName);
            settingsBackButton = root.Q<UITKButton>(settingsBackButtonName);
            settingsCloseButton = root.Q<UITKButton>(settingsCloseButtonName);
            tutorialButton = root.Q<UITKButton>(tutorialButtonName);
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
            gameModeHintLabel = root.Q<Label>(gameModeHintLabelName);
            safeAreaElement = root.Q<VisualElement>(safeAreaElementName);
            mainMenuCard = root.Q<VisualElement>(mainMenuCardName);
            collectionOverlay = root.Q<VisualElement>(collectionOverlayName);
            achievementOverlay = root.Q<VisualElement>(achievementOverlayName);
            leaderboardOverlay = root.Q<VisualElement>(leaderboardOverlayName);
            settingsOverlay = root.Q<VisualElement>(settingsOverlayName);
            collectionList = root.Q<VisualElement>(collectionListName);
            achievementList = root.Q<VisualElement>(achievementListName);
            settingsVolumeSlider = root.Q<UITKSlider>(settingsVolumeSliderName);
            gameModeDropdown = root.Q<UITKDropdownField>(gameModeDropdownName);

            List<string> missingElements = new List<string>();
            if (playButton == null) missingElements.Add(playButtonName);
            if (leaderboardButton == null) missingElements.Add(leaderboardButtonName);
            if (collectionButton == null) missingElements.Add(collectionButtonName);
            if (achievementButton == null) missingElements.Add(achievementButtonName);
            if (settingsButton == null) missingElements.Add(settingsButtonName);
            if (exitButton == null) missingElements.Add(exitButtonName);
            if (collectionBackButton == null) missingElements.Add(collectionBackButtonName);
            if (collectionCloseButton == null) missingElements.Add(collectionCloseButtonName);
            if (manualTabCreaturesButton == null) missingElements.Add(manualTabCreaturesButtonName);
            if (manualTabCollectionsButton == null) missingElements.Add(manualTabCollectionsButtonName);
            if (achievementBackButton == null) missingElements.Add(achievementBackButtonName);
            if (achievementCloseButton == null) missingElements.Add(achievementCloseButtonName);
            if (leaderboardBackButton == null) missingElements.Add(leaderboardBackButtonName);
            if (leaderboardCloseButton == null) missingElements.Add(leaderboardCloseButtonName);
            if (settingsApplyButton == null) missingElements.Add(settingsApplyButtonName);
            if (settingsBackButton == null) missingElements.Add(settingsBackButtonName);
            if (settingsCloseButton == null) missingElements.Add(settingsCloseButtonName);
            if (hintLabel == null) missingElements.Add(hintLabelName);
            if (collectionOverlay == null) missingElements.Add(collectionOverlayName);
            if (achievementOverlay == null) missingElements.Add(achievementOverlayName);
            if (leaderboardOverlay == null) missingElements.Add(leaderboardOverlayName);
            if (settingsOverlay == null) missingElements.Add(settingsOverlayName);
            if (collectionList == null) missingElements.Add(collectionListName);
            if (achievementList == null) missingElements.Add(achievementListName);
            if (collectionProgressLabel == null) missingElements.Add(collectionProgressLabelName);
            if (achievementProgressLabel == null) missingElements.Add(achievementProgressLabelName);
            if (leaderboardBestScoreLabel == null) missingElements.Add(leaderboardBestScoreLabelName);
            if (leaderboardLastScoreLabel == null) missingElements.Add(leaderboardLastScoreLabelName);
            if (leaderboardTotalRunsLabel == null) missingElements.Add(leaderboardTotalRunsLabelName);
            if (leaderboardAverageScoreLabel == null) missingElements.Add(leaderboardAverageScoreLabelName);
            if (leaderboardModeProgressLabel == null) missingElements.Add(leaderboardModeProgressLabelName);
            if (leaderboardTopScoresLabel == null) missingElements.Add(leaderboardTopScoresLabelName);
            if (settingsVolumeSlider == null) missingElements.Add(settingsVolumeSliderName);
            if (settingsVolumeValueLabel == null) missingElements.Add(settingsVolumeValueLabelName);
            if (gameModeDropdown == null) missingElements.Add(gameModeDropdownName);
            if (gameModeHintLabel == null) missingElements.Add(gameModeHintLabelName);

            if (missingElements.Count > 0)
            {
                Debug.LogError(
                    $"MainMenuSceneController could not bind required UI Toolkit elements. Missing: {string.Join(", ", missingElements)}",
                    this);
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
            if (manualTabCreaturesButton != null)
            {
                manualTabCreaturesButton.clicked -= OnManualCreaturesClicked;
                manualTabCreaturesButton.clicked += OnManualCreaturesClicked;
            }
            if (manualTabCollectionsButton != null)
            {
                manualTabCollectionsButton.clicked -= OnManualCollectionsClicked;
                manualTabCollectionsButton.clicked += OnManualCollectionsClicked;
            }
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
            if (gameModeDropdown != null)
            {
                gameModeDropdown.UnregisterValueChangedCallback(OnGameModeChanged);
                gameModeDropdown.RegisterValueChangedCallback(OnGameModeChanged);
            }

            exitButton.clicked -= OnExitClicked;
            exitButton.clicked += OnExitClicked;
            if (tutorialButton != null)
            {
                tutorialButton.clicked -= OnTutorialClicked;
                tutorialButton.clicked += OnTutorialClicked;
            }

            SetManualCategory(CodexCategory.Creature);
            BuildAchievementPage();
            RefreshLeaderboardPanel();
            EnsureSettingsInitialized();
            CloseAllOverlays();
            SetHint("Select an option.");
            ApplySafeAreaIfNeeded();
            isBound = true;
            StartGlowLoopIfReady();
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

            if (manualTabCreaturesButton != null)
            {
                manualTabCreaturesButton.clicked -= OnManualCreaturesClicked;
            }
            if (manualTabCollectionsButton != null)
            {
                manualTabCollectionsButton.clicked -= OnManualCollectionsClicked;
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

            if (tutorialButton != null)
            {
                tutorialButton.clicked -= OnTutorialClicked;
            }

            if (settingsVolumeSlider != null)
            {
                settingsVolumeSlider.UnregisterValueChangedCallback(OnSettingsVolumeChanged);
            }

            if (gameModeDropdown != null)
            {
                gameModeDropdown.UnregisterValueChangedCallback(OnGameModeChanged);
            }

            CloseAllOverlays();
            isBound = false;
            StopGlowLoop();
        }

        private void StartGlowLoopIfReady()
        {
            if (glowRoutine != null || mainMenuCard == null)
            {
                return;
            }

            glowRoutine = StartCoroutine(GlowLoop());
        }

        private void StopGlowLoop()
        {
            if (glowRoutine != null)
            {
                StopCoroutine(glowRoutine);
                glowRoutine = null;
            }

            if (mainMenuCard != null)
            {
                mainMenuCard.RemoveFromClassList(GlowClass);
            }
        }

        private IEnumerator GlowLoop()
        {
            while (mainMenuCard != null)
            {
                float waitTime = UnityEngine.Random.Range(glowMinInterval, glowMaxInterval);
                yield return new WaitForSecondsRealtime(waitTime);

                mainMenuCard.AddToClassList(GlowClass);
                yield return new WaitForSecondsRealtime(glowDuration);
                mainMenuCard.RemoveFromClassList(GlowClass);
            }

            glowRoutine = null;
        }

        private void OnPlayClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseAllOverlays();
            GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.LoadGameplayScene();
                return;
            }

            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                if (!SceneTransitionOverlay.TryLoadScene(gameplaySceneName))
                {
                    SceneManager.LoadScene(gameplaySceneName);
                }
            }
        }

        private void OnTutorialClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseAllOverlays();
            TutorialOverlayController.ForceNextRun = true;
            SetHint("Loading tutorial...");
            GameManager gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.LoadGameplayScene();
                return;
            }

            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                if (!SceneTransitionOverlay.TryLoadScene(gameplaySceneName))
                {
                    SceneManager.LoadScene(gameplaySceneName);
                }
            }
        }

        private void OnExitClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseAllOverlays();
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
            AudioManager.Instance?.PlayButtonClick();
            RefreshLeaderboardPanel();
            OpenOverlay(MainMenuOverlay.Leaderboard);
            SetHint("Leaderboard opened.");
        }

        private void OnCollectionClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            SetManualCategory(CodexCategory.Creature);
            OpenOverlay(MainMenuOverlay.Manual);
            SetHint("Manual opened.");
        }

        private void OnManualCreaturesClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            SetManualCategory(CodexCategory.Creature);
        }

        private void OnManualCollectionsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            SetManualCategory(CodexCategory.Collection);
        }

        private void OnAchievementClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            BuildAchievementPage();
            OpenOverlay(MainMenuOverlay.Achievements);
            SetHint("Achievements opened.");
        }

        private void OnCollectionCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseActiveOverlay();
            SetHint("Manual closed.");
        }

        private void OnAchievementCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseActiveOverlay();
            SetHint("Achievements closed.");
        }

        private void OnBackToMainInterfaceClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseAllOverlays();
            SetHint("Returned to main interface.");
        }

        private void OnLeaderboardCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseActiveOverlay();
            SetHint("Leaderboard closed.");
        }

        private void OnSettingsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            EnsureSettingsInitialized();
            RefreshSettingsPanel();
            OpenOverlay(MainMenuOverlay.Settings);
            SetHint("Settings opened.");
        }

        private void OnSettingsApplyClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (settingsVolumeSlider != null)
            {
                ApplyMasterVolume(settingsVolumeSlider.value, true);
            }

            if (!RunProgressStore.TrySetSelectedMode(pendingSelectedModeId, out string reason))
            {
                PlayerPrefs.Save();
                SetHint(reason);
                RefreshSettingsPanel();
                return;
            }

            PlayerPrefs.Save();
            CloseActiveOverlay();
            SetHint("Settings saved.");
        }

        private void OnSettingsCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            CloseActiveOverlay();
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

        private void HandleOverlayBackInput()
        {
            if (!IsEscapePressed())
            {
                return;
            }

            TryHandleOverlayBackAction();
        }

        private bool TryHandleOverlayBackAction()
        {
            switch (activeOverlay)
            {
                case MainMenuOverlay.Settings:
                    OnSettingsCloseClicked();
                    return true;
                case MainMenuOverlay.Achievements:
                    OnAchievementCloseClicked();
                    return true;
                case MainMenuOverlay.Manual:
                    OnCollectionCloseClicked();
                    return true;
                case MainMenuOverlay.Leaderboard:
                    OnLeaderboardCloseClicked();
                    return true;
                default:
                    return false;
            }
        }

        private void OpenOverlay(MainMenuOverlay overlay)
        {
            activeOverlay = overlay;
            RefreshOverlayVisibility();
        }

        private void CloseActiveOverlay()
        {
            activeOverlay = MainMenuOverlay.None;
            RefreshOverlayVisibility();
        }

        private void CloseAllOverlays()
        {
            activeOverlay = MainMenuOverlay.None;
            RefreshOverlayVisibility();
        }

        private void RefreshOverlayVisibility()
        {
            SetOverlayVisible(collectionOverlay, activeOverlay == MainMenuOverlay.Manual);
            SetOverlayVisible(achievementOverlay, activeOverlay == MainMenuOverlay.Achievements);
            SetOverlayVisible(leaderboardOverlay, activeOverlay == MainMenuOverlay.Leaderboard);
            SetOverlayVisible(settingsOverlay, activeOverlay == MainMenuOverlay.Settings);
        }

        private void SetOverlayVisible(VisualElement overlay, bool visible)
        {
            if (overlay == null)
            {
                return;
            }

            overlay.EnableInClassList(VisibleClass, visible);
        }

        private void BuildCollectionPage()
        {
            if (collectionList == null || collectionProgressLabel == null)
            {
                return;
            }

            collectionList.Clear();
            CodexDatabase database = GetCodexDatabase();
            IReadOnlyList<CodexEntry> entries = database != null ? database.GetEntries(currentManualCategory) : null;
            int unlockedCount = 0;
            int totalCollectedCount = 0;
            int totalCount = entries != null ? entries.Count : 0;

            if (entries != null && totalCount > 0)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    CodexEntry entry = entries[i];
                    bool unlocked = RunProgressStore.IsCodexEntryUnlocked(currentManualCategory, entry.id);
                    int ownedCount = RunProgressStore.GetCodexEntryCount(currentManualCategory, entry.id);
                    if (unlocked)
                    {
                        unlockedCount++;
                    }

                    totalCollectedCount += ownedCount;
                    collectionList.Add(CreateManualEntryElement(entry, currentManualCategory, unlocked, ownedCount));
                }
            }
            else if (currentManualCategory == CodexCategory.Collection)
            {
                CollectionEntry[] legacyEntries = GetCollectionEntries();
                totalCount = legacyEntries.Length;
                for (int i = 0; i < legacyEntries.Length; i++)
                {
                    CollectionEntry entry = legacyEntries[i];
                    bool unlocked = RunProgressStore.IsCollectionEntryUnlocked(i);
                    int ownedCount = RunProgressStore.GetCollectionEntryCount(i);
                    if (unlocked)
                    {
                        unlockedCount++;
                    }

                    totalCollectedCount += ownedCount;
                    collectionList.Add(CreateLegacyCollectionEntryElement(entry, unlocked, ownedCount));
                }
            }

            if (currentManualCategory == CodexCategory.Collection)
            {
                collectionProgressLabel.text = $"Unlocked {unlockedCount}/{Mathf.Max(1, totalCount)} | Total Collected: {totalCollectedCount}";
            }
            else
            {
                collectionProgressLabel.text = $"Unlocked {unlockedCount}/{Mathf.Max(1, totalCount)}";
            }
        }

        private CodexDatabase GetCodexDatabase()
        {
            if (codexDatabase == null)
            {
                codexDatabase = CodexDatabase.Load();
            }

            return codexDatabase;
        }

        private void SetManualCategory(CodexCategory category)
        {
            currentManualCategory = category;
            UpdateManualTabVisuals();
            BuildCollectionPage();
        }

        private void UpdateManualTabVisuals()
        {
            SetTabActive(manualTabCreaturesButton, currentManualCategory == CodexCategory.Creature);
            SetTabActive(manualTabCollectionsButton, currentManualCategory == CodexCategory.Collection);
        }

        private static void SetTabActive(VisualElement button, bool active)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList(TabActiveClass, active);
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
            AchievementManager.AchievementDefinition[] defs = AchievementManager.GetDefinitions();
            AchievementEntry[] entries = BuildAchievementEntries();
            int completedCount = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                AchievementEntry entry = entries[i];
                bool completed = RunProgressStore.IsAchievementCompleted(defs[i].Id)
                              || entry.current >= entry.target;
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
            AchievementManager.AchievementDefinition[] defs = AchievementManager.GetDefinitions();
            AchievementEntry[] entries = new AchievementEntry[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                AchievementManager.AchievementDefinition def = defs[i];
                entries[i] = new AchievementEntry
                {
                    title = def.Title,
                    description = def.Description,
                    current = def.GetCurrentValue != null ? def.GetCurrentValue() : 0,
                    target = AchievementManager.GetTarget(def)
                };
            }

            return entries;
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

        private VisualElement CreateLegacyCollectionEntryElement(CollectionEntry entry, bool unlocked, int ownedCount)
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

        private VisualElement CreateManualEntryElement(CodexEntry entry, CodexCategory category, bool unlocked, int ownedCount)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("collection-entry");
            if (!unlocked)
            {
                row.AddToClassList(LockedClass);
            }

            string title = unlocked ? entry.title : "???";
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
                statusText = $"Locked - {entry.unlockHint}";
            }

            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("collection-entry-status");
            row.Add(statusLabel);

            string description = unlocked ? entry.description : "Keep exploring to unlock this entry.";
            Label descriptionLabel = new Label(description);
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
            if (settingsVolumeSlider == null || gameModeDropdown == null)
            {
                return;
            }

            if (settingsInitialized)
            {
                RefreshSettingsPanel();
                return;
            }

            PopulateGameModeOptions();
            pendingSelectedModeId = RunProgressStore.GetSelectedModeId();
            float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f));
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(savedVolume);
            }
            else
            {
                AudioListener.volume = savedVolume;
            }
            settingsInitialized = true;
            RefreshSettingsPanel();
        }

        private void RefreshSettingsPanel()
        {
            if (settingsVolumeSlider == null || gameModeDropdown == null)
            {
                return;
            }

            suppressSettingsEvents = true;
            float currentVolume = AudioManager.Instance != null
                ? AudioManager.Instance.GetMasterVolume()
                : Mathf.Clamp01(AudioListener.volume);
            settingsVolumeSlider.SetValueWithoutNotify(currentVolume);
            RefreshVolumeValueText(currentVolume);

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
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(clamped);
            }
            else
            {
                AudioListener.volume = clamped;
            }

            if (!persist)
            {
                return;
            }

            PlayerPrefs.SetFloat(MasterVolumePrefKey, clamped);
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
