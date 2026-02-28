using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessRunner
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState initialState = GameState.Menu;
        [SerializeField] private bool forcePortrait = true;
        [SerializeField] private bool useSceneBasedMainMenu = true;
        [SerializeField] private bool allowGameOverQuickRestart = false;
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private string gameplaySceneName = "SampleScene";
        [SerializeField] private RunnerController runner;
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private AbilityManager abilityManager;

        public GameState State { get; private set; }
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            ApplyPortraitOnlyOrientation();

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            GameState startState = initialState;
            if (startState == GameState.Boot)
            {
                startState = GameState.Menu;
                Debug.LogWarning("GameManager initialState is Boot. Falling back to Menu.");
            }

            SetState(startState);
            if (startState == GameState.Running)
            {
                BeginRun();
            }
        }

        private void Update()
        {
            if (useSceneBasedMainMenu && State == GameState.Menu && IsCurrentScene(mainMenuSceneName) && IsQuitPressed())
            {
                QuitGame();
                return;
            }

            if (allowGameOverQuickRestart && State == GameState.GameOver && IsStartPressed())
            {
                BeginRun();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BeginRun()
        {
            if (useSceneBasedMainMenu && IsCurrentScene(mainMenuSceneName))
            {
                LoadGameplayScene();
                return;
            }

            Time.timeScale = 1f;
            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }

            if (abilityManager != null)
            {
                abilityManager.ResetRun();
            }
            if (runner != null)
            {
                runner.ResetRunner();
            }

            if (trackManager != null)
            {
                trackManager.ResetTrack();
            }

            if (scoreManager != null)
            {
                scoreManager.ResetScore();
            }

            SetState(GameState.Running);
        }

        public void Pause()
        {
            if (State != GameState.Running)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Running);
        }

        public void GameOver()
        {
            if (State == GameState.GameOver)
            {
                return;
            }

            if (scoreManager == null)
            {
                scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
            }

            int finalScore = scoreManager != null ? scoreManager.Score : 0;
            RunProgressStore.RecordRun(finalScore, RunProgressStore.GetSelectedModeId());
            SetState(GameState.GameOver);
        }

        public void LoadMainMenuScene()
        {
            LoadSceneByName(mainMenuSceneName);
        }

        public void LoadGameplayScene()
        {
            LoadSceneByName(gameplaySceneName);
        }

        /// <summary>
        /// Runtime hook used by character switch systems.
        /// Updates the active runner reference so BeginRun/Reset logic targets the new character.
        /// </summary>
        public void SetRunner(RunnerController newRunner)
        {
            runner = newRunner;
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetState(GameState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }

        private bool IsStartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
            return false;
#else
            if (Input.anyKeyDown)
            {
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }

            return Input.touchCount > 0;
#endif
        }

        private bool IsQuitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private bool IsCurrentScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && string.Equals(activeScene.name, sceneName, StringComparison.Ordinal);
        }

        private void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("Scene name is empty. Cannot load scene.", this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void ApplyPortraitOnlyOrientation()
        {
            if (!forcePortrait)
            {
                return;
            }

            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }
    }
}
