using UnityEngine;
using UnityEngine.UIElements;

namespace EndlessRunner
{
    public class AbilityAcquiredUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private GameManager gameManager;

        [Header("UI Toolkit")]
        [SerializeField] private string panelName = "ability-acquired-panel";
        [SerializeField] private string titleLabelName = "ability-acquired-title";
        [SerializeField] private string descLabelName = "ability-acquired-desc";
        [SerializeField] private string hintLabelName = "ability-acquired-hint";

        private VisualElement panel;
        private Label titleLabel;
        private Label descLabel;
        private Label hintLabel;
        private bool isOpen;
        private const string VisibleClass = "is-visible";

        private void Awake()
        {
            ResolveReferences();
            CacheElements();
            SetVisible(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheElements();
            if (abilityManager != null)
            {
                abilityManager.AbilityReplaced += OnAbilityReplaced;
            }
        }

        private void OnDisable()
        {
            if (abilityManager != null)
            {
                abilityManager.AbilityReplaced -= OnAbilityReplaced;
            }
        }

        private void OnAbilityReplaced(AbilityDefinition ability)
        {
            if (ability == null)
            {
                return;
            }

            Show(ability);
        }

        private void Show(AbilityDefinition ability)
        {
            if (panel == null)
            {
                CacheElements();
            }

            if (panel == null)
            {
                return;
            }

            titleLabel.text = ability.displayName;
            descLabel.text = string.IsNullOrWhiteSpace(ability.description)
                ? "A new power surges within you."
                : ability.description;

            if (hintLabel != null)
            {
                hintLabel.text = "Tap anywhere to continue.";
            }

            SetVisible(true);
            PauseGame();
        }

        private void Hide()
        {
            if (!isOpen)
            {
                return;
            }

            SetVisible(false);
            ResumeGame();
        }

        private void SetVisible(bool visible)
        {
            if (panel == null)
            {
                return;
            }

            isOpen = visible;
            if (visible)
            {
                panel.AddToClassList(VisibleClass);
            }
            else
            {
                panel.RemoveFromClassList(VisibleClass);
            }
        }

        private void HandlePanelClick(ClickEvent evt)
        {
            Hide();
        }

        private void CacheElements()
        {
            if (uiDocument == null)
            {
                uiDocument = FindAnyObjectByType<UIDocument>();
            }

            if (uiDocument == null)
            {
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            panel = root.Q<VisualElement>(panelName);
            titleLabel = root.Q<Label>(titleLabelName);
            descLabel = root.Q<Label>(descLabelName);
            hintLabel = root.Q<Label>(hintLabelName);

            if (panel != null)
            {
                panel.UnregisterCallback<ClickEvent>(HandlePanelClick);
                panel.RegisterCallback<ClickEvent>(HandlePanelClick);
            }
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
    }
}
