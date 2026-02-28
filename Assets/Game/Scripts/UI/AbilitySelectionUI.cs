using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UITKButton = UnityEngine.UIElements.Button;
using UGUIButton = UnityEngine.UI.Button;
using UGUIText = UnityEngine.UI.Text;
using UGUIImage = UnityEngine.UI.Image;

namespace EndlessRunner
{
    public class AbilitySelectionUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private UGUIButton[] choiceButtons;
        [SerializeField] private UGUIText[] choiceLabels;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RunnerController runner;

        [Header("UI Toolkit")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private bool createUiDocumentIfMissing = false;
        [SerializeField] private string visualTreeResource = "UI/GameUI";
        [SerializeField] private string panelSettingsResource = "UI/GamePanelSettings";
        [SerializeField] private string panelName = "ability-panel";
        [SerializeField] private string choiceButtonPrefix = "ability-choice-";
        [SerializeField] private string mainMenuButtonName = "ability-mainmenu-button";
        [SerializeField] private int toolkitChoiceCount = 3;

        private readonly List<AbilityDefinition> currentChoices = new();
        private readonly List<UITKButton> toolkitButtons = new();
        private readonly List<Action> toolkitHandlers = new();
        private UITKButton toolkitMainMenuButton;
        private VisualElement toolkitPanel;
        private bool isOpen;
        private bool useToolkit;
        private const string VisibleClass = "is-visible";

        private void Awake()
        {
            ResolveReferences();
            EnsureUiDocument();
            HideLegacyUI();
        }

        private void Start()
        {
            // UIDocument can finish attaching its visual tree after Awake.
            CacheToolkitElements(false);
            SetToolkitVisible(false);
        }

        public bool Open()
        {
            ResolveReferences();
            CacheToolkitElements(true);

            if (isOpen)
            {
                return false;
            }

            if (!useToolkit)
            {
                Debug.LogError("AbilitySelectionUI is configured for UI Toolkit only, but toolkit UI is unavailable.");
                return false;
            }

            runner?.ResetVelocity();
            PauseGame();
            isOpen = true;
            HideLegacyUI();
            SetToolkitVisible(true);
            RefreshToolkitChoices();
            return true;
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            SetToolkitVisible(false);

            ResumeGame();
        }

        /// <summary>
        /// Runtime hook: updates the controlled runner reference after character switch.
        /// </summary>
        public void SetRunner(RunnerController newRunner)
        {
            runner = newRunner;
        }

        private void RefreshLegacyChoices()
        {
            currentChoices.Clear();
            if (abilityManager != null)
            {
                currentChoices.AddRange(abilityManager.RollChoices());
            }

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                UGUIButton button = choiceButtons[i];
                UGUIText label = i < choiceLabels.Length ? choiceLabels[i] : null;
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();

                if (i < currentChoices.Count)
                {
                    AbilityDefinition ability = currentChoices[i];
                    button.gameObject.SetActive(true);
                    if (label != null && ability != null)
                    {
                        label.text = ability.displayName;
                    }

                    int index = i;
                    button.onClick.AddListener(() => Choose(index));
                }
                else
                {
                    button.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshToolkitChoices()
        {
            currentChoices.Clear();
            if (abilityManager != null)
            {
                currentChoices.AddRange(abilityManager.RollChoices());
            }

            for (int i = 0; i < toolkitButtons.Count; i++)
            {
                UITKButton button = toolkitButtons[i];
                ClearToolkitHandler(i);

                if (i < currentChoices.Count)
                {
                    AbilityDefinition ability = currentChoices[i];
                    button.style.display = DisplayStyle.Flex;
                    button.SetEnabled(true);
                    button.text = ability != null ? ability.displayName : "Ability";

                    int index = i;
                    Action handler = () => Choose(index);
                    toolkitHandlers[i] = handler;
                    button.clicked += handler;
                }
                else
                {
                    button.style.display = DisplayStyle.None;
                }
            }
        }

        private void Choose(int index)
        {
            if (index < 0 || index >= currentChoices.Count)
            {
                return;
            }

            AbilityDefinition ability = currentChoices[index];
            if (abilityManager != null)
            {
                abilityManager.ChooseAbility(ability);
            }

            Close();
        }

        private void PauseGame()
        {
            if (gameManager != null)
            {
                gameManager.Pause();
                return;
            }

            Time.timeScale = 0f;
        }

        private void ResumeGame()
        {
            if (gameManager != null)
            {
                gameManager.Resume();
                return;
            }

            Time.timeScale = 1f;
        }

        private void ResolveReferences()
        {
            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            }

            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }
        }

        private bool EnsureUI()
        {
            if (useToolkit)
            {
                return false;
            }

            if (panel != null && choiceButtons != null && choiceButtons.Length >= 1)
            {
                EnsureEventSystem();
                return true;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("AbilityCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            panel = CreatePanel(canvas.transform);
            choiceButtons = new UGUIButton[3];
            choiceLabels = new UGUIText[3];

            for (int i = 0; i < 3; i++)
            {
                CreateButton(panel.transform, i, out choiceButtons[i], out choiceLabels[i]);
            }

            EnsureEventSystem();
            return true;
        }

        private void CacheToolkitElements(bool logErrors)
        {
            useToolkit = false;
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

            toolkitPanel = root.Q<VisualElement>(panelName);
            toolkitMainMenuButton = root.Q<UITKButton>(mainMenuButtonName);
            toolkitButtons.Clear();
            toolkitHandlers.Clear();

            for (int i = 0; i < toolkitChoiceCount; i++)
            {
                UITKButton button = root.Q<UITKButton>($"{choiceButtonPrefix}{i}");
                if (button == null)
                {
                    continue;
                }

                toolkitButtons.Add(button);
                toolkitHandlers.Add(null);
            }

            useToolkit = toolkitPanel != null && toolkitButtons.Count >= toolkitChoiceCount;
            if (toolkitMainMenuButton != null)
            {
                toolkitMainMenuButton.clicked -= OnMainMenuClicked;
                toolkitMainMenuButton.clicked += OnMainMenuClicked;
            }

            if (!useToolkit && logErrors)
            {
                Debug.LogError(
                    $"AbilitySelectionUI could not bind required toolkit elements. Panel: {panelName}, Buttons Found: {toolkitButtons.Count}/{toolkitChoiceCount}");
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

        private void HideLegacyUI()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void SetToolkitVisible(bool visible)
        {
            if (toolkitPanel == null)
            {
                return;
            }

            toolkitPanel.EnableInClassList(VisibleClass, visible);
        }

        private void ClearToolkitHandler(int index)
        {
            if (index < 0 || index >= toolkitHandlers.Count)
            {
                return;
            }

            Action handler = toolkitHandlers[index];
            if (handler == null || index >= toolkitButtons.Count)
            {
                return;
            }

            toolkitButtons[index].clicked -= handler;
            toolkitHandlers[index] = null;
        }

        private void OnMainMenuClicked()
        {
            isOpen = false;
            SetToolkitVisible(false);

            if (gameManager != null)
            {
                gameManager.LoadMainMenuScene();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenuScene");
        }

        private GameObject CreatePanel(Transform parent)
        {
            GameObject panelObject = new GameObject("AbilityPanel");
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UGUIImage image = panelObject.AddComponent<UGUIImage>();
            image.color = new Color(0f, 0f, 0f, 0.7f);

            return panelObject;
        }

        private void CreateButton(Transform parent, int index, out UGUIButton button, out UGUIText label)
        {
            GameObject buttonObject = new GameObject($"AbilityButton_{index + 1}");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560, 120);
            rect.anchoredPosition = new Vector2(0, 140 - index * 150);

            UGUIImage image = buttonObject.AddComponent<UGUIImage>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            button = buttonObject.AddComponent<UGUIButton>();

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            label = textObject.AddComponent<UGUIText>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.black;
            label.text = "Ability";
        }

        private void EnsureEventSystem()
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
    }
}
