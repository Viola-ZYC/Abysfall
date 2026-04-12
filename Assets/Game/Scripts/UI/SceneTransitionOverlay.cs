using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EndlessRunner
{
    public class SceneTransitionOverlay : MonoBehaviour
    {
        private const string DefaultObjectName = "SceneTransitionUI";
        private const string DefaultPanelSettingsResource = "UI/GamePanelSettings";
        private const string DefaultVisualTreeResource = "UI/SceneTransitionUI";
        private const string DefaultOverlayRootName = "scene-transition-root";

        private static SceneTransitionOverlay instance;
        private static bool startupPlayed;

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string panelSettingsResource = DefaultPanelSettingsResource;
        [SerializeField] private string visualTreeResource = DefaultVisualTreeResource;
        [SerializeField] private string overlayRootName = DefaultOverlayRootName;
        [SerializeField] private int sortingOrder = 200;
        [SerializeField] private float startupHoldDuration = 0.12f;
        [SerializeField] private float startupRevealDuration = 0.55f;
        [SerializeField] private float fadeToBlackDuration = 0.2f;
        [SerializeField] private float blackHoldDuration = 0.05f;
        [SerializeField] private float revealDuration = 0.24f;

        private VisualElement overlayRoot;
        private Coroutine transitionRoutine;
        private bool isTransitioning;

        public static bool IsTransitioning => instance != null && instance.isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance = null;
            startupPlayed = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapOverlay()
        {
            EnsureExists();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PlayInitialStartupOverlay()
        {
            PlayStartupIfNeeded(SceneManager.GetActiveScene().name);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureUiDocument();
            CacheElements();
            SetOverlayVisible(!startupPlayed);
            SetOverlayOpacity(startupPlayed ? 0f : 1f);
        }

        public static void EnsureExists()
        {
            if (instance != null)
            {
                return;
            }

            instance = FindAnyObjectByType<SceneTransitionOverlay>();
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject(DefaultObjectName);
            instance = root.AddComponent<SceneTransitionOverlay>();
        }

        public static void PlayStartupIfNeeded(string sceneName)
        {
            EnsureExists();
            if (instance == null || startupPlayed || instance.isTransitioning)
            {
                return;
            }

            instance.BeginStartupSequence(sceneName);
        }

        public static bool TryLoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            EnsureExists();
            return instance != null && instance.BeginSceneTransition(sceneName);
        }

        private void BeginStartupSequence(string sceneName)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            startupPlayed = true;
            transitionRoutine = StartCoroutine(StartupSequence(sceneName));
        }

        private bool BeginSceneTransition(string sceneName)
        {
            if (isTransitioning)
            {
                return false;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(SceneTransitionSequence(sceneName));
            return true;
        }

        private IEnumerator StartupSequence(string sceneName)
        {
            isTransitioning = true;
            SetOverlayVisible(true);
            SetOverlayOpacity(1f);

            yield return WaitForSecondsRealtime(startupHoldDuration);
            yield return FadeOverlay(1f, 0f, startupRevealDuration);

            SetOverlayVisible(false);
            transitionRoutine = null;
            isTransitioning = false;
        }

        private IEnumerator SceneTransitionSequence(string sceneName)
        {
            isTransitioning = true;
            SetOverlayVisible(true);
            SetOverlayOpacity(0f);

            yield return FadeOverlay(0f, 1f, fadeToBlackDuration);
            yield return WaitForSecondsRealtime(blackHoldDuration);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (loadOperation != null && !loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;

            yield return WaitForSecondsRealtime(blackHoldDuration);
            yield return FadeOverlay(1f, 0f, revealDuration);

            SetOverlayVisible(false);
            transitionRoutine = null;
            isTransitioning = false;
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            if (overlayRoot == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                SetOverlayOpacity(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetOverlayOpacity(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetOverlayOpacity(to);
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

            uiDocument.sortingOrder = sortingOrder;
        }

        private void CacheElements()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;
            overlayRoot = root.Q<VisualElement>(overlayRootName);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (overlayRoot == null)
            {
                CacheElements();
                if (overlayRoot == null)
                {
                    return;
                }
            }

            overlayRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            overlayRoot.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        private void SetOverlayOpacity(float opacity)
        {
            if (overlayRoot == null)
            {
                CacheElements();
                if (overlayRoot == null)
                {
                    return;
                }
            }

            overlayRoot.style.opacity = Mathf.Clamp01(opacity);
        }

        private static IEnumerator WaitForSecondsRealtime(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
