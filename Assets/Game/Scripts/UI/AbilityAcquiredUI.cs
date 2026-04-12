using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EndlessRunner
{
    public class AbilityAcquiredUI : MonoBehaviour
    {
        private const string VisibleClass = "is-visible";
        private const string DefaultSubtitleLabelName = "ability-acquired-subtitle";
        private const string DefaultMetaLabelName = "ability-acquired-meta";

        private struct PopupRequest
        {
            public string subtitle;
            public string title;
            public string description;
            public string meta;
            public string hint;
        }

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private GameManager gameManager;

        [Header("UI Toolkit")]
        [SerializeField] private string panelName = "ability-acquired-panel";
        [SerializeField] private string titleLabelName = "ability-acquired-title";
        [SerializeField] private string descLabelName = "ability-acquired-desc";
        [SerializeField] private string hintLabelName = "ability-acquired-hint";

        private readonly Queue<PopupRequest> popupQueue = new();
        private VisualElement panel;
        private Label subtitleLabel;
        private Label titleLabel;
        private Label descLabel;
        private Label metaLabel;
        private Label hintLabel;
        private bool isOpen;
        private bool pauseRequested;

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
                abilityManager.AbilityAcquired += OnAbilityAcquired;
            }
        }

        private void OnDisable()
        {
            if (abilityManager != null)
            {
                abilityManager.AbilityAcquired -= OnAbilityAcquired;
            }

            DismissAll(releasePause: true);
        }

        private void OnAbilityAcquired(AbilityDefinition ability, int stacks)
        {
            if (ability == null || stacks != 1)
            {
                return;
            }

            if (!RunProgressStore.UnlockAbility(ability.abilityId))
            {
                return;
            }

            EnqueuePopup(BuildAbilityPopup(ability));
        }

        public void ShowCollectible(CodexEntry entry)
        {
            ShowCodexEntry(CodexCategory.Collection, entry);
        }

        public void ShowCreature(CodexEntry entry)
        {
            ShowCodexEntry(CodexCategory.Creature, entry);
        }

        public void ShowCodexEntry(CodexCategory category, CodexEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            EnqueuePopup(BuildCodexPopup(category, entry));
        }

        private PopupRequest BuildAbilityPopup(AbilityDefinition ability)
        {
            bool isPassive = ability.isPassive || ability.activeEffect == null;
            return new PopupRequest
            {
                subtitle = "Ability Manual Updated",
                title = string.IsNullOrWhiteSpace(ability.displayName) ? "Ability Acquired" : ability.displayName,
                description = string.IsNullOrWhiteSpace(ability.description)
                    ? "A new power surges within you."
                    : ability.description,
                meta = isPassive ? "Passive ability recorded." : "Active ability recorded.",
                hint = "Tap to resume the run."
            };
        }

        private PopupRequest BuildCodexPopup(CodexCategory category, CodexEntry entry)
        {
            return new PopupRequest
            {
                subtitle = category switch
                {
                    CodexCategory.Creature => "New Creature Entry",
                    CodexCategory.Collection => "New Collectible Entry",
                    CodexCategory.Obstacle => "New Obstacle Entry",
                    _ => "New Codex Entry"
                },
                title = string.IsNullOrWhiteSpace(entry.title) ? "Unknown Entry" : entry.title,
                description = string.IsNullOrWhiteSpace(entry.description)
                    ? "A new record has been added to the codex."
                    : entry.description,
                meta = BuildCodexMeta(category, entry),
                hint = "Tap to resume the run."
            };
        }

        private string BuildCodexMeta(CodexCategory category, CodexEntry entry)
        {
            string detail = string.Empty;
            if (category == CodexCategory.Creature && !string.IsNullOrWhiteSpace(entry.abilityId))
            {
                detail = entry.isPassive
                    ? $"Trait: {entry.abilityId}"
                    : $"Skill Source: {entry.abilityId}";
            }
            else if (!string.IsNullOrWhiteSpace(entry.note) &&
                     !string.Equals(entry.note, "Example", System.StringComparison.OrdinalIgnoreCase))
            {
                detail = entry.note;
            }
            else if (!string.IsNullOrWhiteSpace(entry.unlockHint))
            {
                detail = entry.unlockHint;
            }

            string categoryLabel = category switch
            {
                CodexCategory.Creature => "Creatures",
                CodexCategory.Collection => "Collections",
                CodexCategory.Obstacle => "Obstacles",
                _ => "Codex"
            };

            return string.IsNullOrWhiteSpace(detail)
                ? $"{categoryLabel} Archive"
                : $"{categoryLabel} Archive • {detail}";
        }

        private void EnqueuePopup(PopupRequest request)
        {
            popupQueue.Enqueue(request);
            TryShowNextPopup();
        }

        private void TryShowNextPopup()
        {
            if (isOpen || popupQueue.Count == 0)
            {
                return;
            }

            if (panel == null)
            {
                CacheElements();
            }

            if (panel == null)
            {
                return;
            }

            PopupRequest request = popupQueue.Dequeue();
            if (subtitleLabel != null)
            {
                subtitleLabel.text = request.subtitle;
            }

            if (titleLabel != null)
            {
                titleLabel.text = request.title;
            }

            if (descLabel != null)
            {
                descLabel.text = request.description;
            }

            if (metaLabel != null)
            {
                metaLabel.text = request.meta;
            }

            if (hintLabel != null)
            {
                hintLabel.text = request.hint;
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

            if (popupQueue.Count > 0)
            {
                TryShowNextPopup();
                return;
            }

            ResumeGame();
        }

        private void DismissAll(bool releasePause)
        {
            popupQueue.Clear();
            SetVisible(false);
            if (releasePause)
            {
                ResumeGame();
            }
        }

        private void SetVisible(bool visible)
        {
            isOpen = visible;

            if (panel == null)
            {
                return;
            }

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
            evt.StopPropagation();
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
            subtitleLabel = root.Q<Label>(DefaultSubtitleLabelName);
            titleLabel = root.Q<Label>(titleLabelName);
            descLabel = root.Q<Label>(descLabelName);
            metaLabel = root.Q<Label>(DefaultMetaLabelName);
            hintLabel = root.Q<Label>(hintLabelName);

            if (panel != null)
            {
                panel.UnregisterCallback<ClickEvent>(HandlePanelClick);
                panel.RegisterCallback<ClickEvent>(HandlePanelClick);
            }

            if (!isOpen && panel != null)
            {
                panel.RemoveFromClassList(VisibleClass);
            }

            if (panel != null)
            {
                TryShowNextPopup();
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
            if (pauseRequested)
            {
                return;
            }

            pauseRequested = true;
            if (gameManager != null)
            {
                gameManager.RequestPause(this);
                return;
            }

            Time.timeScale = 0f;
        }

        private void ResumeGame()
        {
            if (!pauseRequested)
            {
                return;
            }

            pauseRequested = false;
            if (gameManager != null)
            {
                gameManager.ReleasePause(this);
                return;
            }

            Time.timeScale = 1f;
        }
    }
}
