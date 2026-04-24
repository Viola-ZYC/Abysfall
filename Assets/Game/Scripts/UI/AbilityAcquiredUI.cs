using System;
using System.Collections.Generic;
using System.Collections;
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
        [SerializeField] private string cardName = "ability-acquired-card";
        [SerializeField] private string titleLabelName = "ability-acquired-title";
        [SerializeField] private string descLabelName = "ability-acquired-desc";
        [SerializeField] private string hintLabelName = "ability-acquired-hint";

        [Header("Popup Animation")]
        [SerializeField, Min(0.01f)] private float popupOpenDuration = 0.26f;
        [SerializeField, Min(0.01f)] private float popupCloseDuration = 0.22f;
        [SerializeField, Range(0.5f, 0.98f)] private float popupHiddenScale = 0.82f;
        [SerializeField, Range(0f, 2f)] private float popupOpenBounce = 0.9f;
        [SerializeField, Range(0f, 2f)] private float popupCloseBounce = 0.65f;

        private readonly Queue<PopupRequest> popupQueue = new();
        private readonly HashSet<string> shownAbilityPopupIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> shownCodexPopupKeys = new(StringComparer.Ordinal);
        private VisualElement panel;
        private VisualElement card;
        private Label subtitleLabel;
        private Label titleLabel;
        private Label descLabel;
        private Label metaLabel;
        private Label hintLabel;
        private bool isOpen;
        private bool isClosing;
        private bool pauseRequested;
        private bool hasObservedGameState;
        private GameState lastObservedGameState = GameState.Boot;
        private Coroutine animationRoutine;

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
            if (gameManager != null)
            {
                gameManager.StateChanged += OnGameStateChanged;
                OnGameStateChanged(gameManager.State);
            }

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

            if (gameManager != null)
            {
                gameManager.StateChanged -= OnGameStateChanged;
            }

            DismissAll(releasePause: true);
        }

        private void OnAbilityAcquired(AbilityDefinition ability, int stacks)
        {
            if (ability == null || stacks != 1)
            {
                return;
            }

            RunProgressStore.UnlockAbility(ability.abilityId);
            if (!TryRegisterAbilityPopup(ability))
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

        public void ShowCodexEntryOncePerRun(CodexCategory category, CodexEntry entry)
        {
            if (entry == null || !TryRegisterCodexPopup(category, entry.id))
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
                hint = "Tap anywhere to continue."
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
                    _ => "New Codex Entry"
                },
                title = string.IsNullOrWhiteSpace(entry.title) ? "Unknown Entry" : entry.title,
                description = string.IsNullOrWhiteSpace(entry.description)
                    ? "A new record has been added to the codex."
                    : entry.description,
                meta = BuildCodexMeta(category, entry),
                hint = "Tap anywhere to continue."
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

        private bool TryRegisterAbilityPopup(AbilityDefinition ability)
        {
            if (ability == null)
            {
                return false;
            }

            string key = !string.IsNullOrWhiteSpace(ability.abilityId)
                ? ability.abilityId
                : ability.name;
            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            return shownAbilityPopupIds.Add(key);
        }

        private bool TryRegisterCodexPopup(CodexCategory category, string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return false;
            }

            string key = $"{category}:{entryId}";
            return shownCodexPopupKeys.Add(key);
        }

        private void TryShowNextPopup()
        {
            if (isOpen || animationRoutine != null || popupQueue.Count == 0)
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

            StartShowAnimation();
            PauseGame();
        }

        private void Hide()
        {
            if (!isOpen || isClosing)
            {
                return;
            }

            StartHideAnimation();
        }

        private void DismissAll(bool releasePause)
        {
            popupQueue.Clear();
            HideImmediately();
            if (releasePause)
            {
                ResumeGame();
            }
        }

        private void ResetRunPopupState()
        {
            popupQueue.Clear();
            HideImmediately();
            pauseRequested = false;
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
            if (uiDocument == null || !UIDocumentLocator.DocumentContainsElement(uiDocument, panelName))
            {
                uiDocument = UIDocumentLocator.FindGameplayDocument();
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
            card = root.Q<VisualElement>(cardName);
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
                ApplyAnimationState(isOpen ? 1f : 0f, isOpen ? 1f : popupHiddenScale);
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

        private void OnGameStateChanged(GameState state)
        {
            bool shouldReset =
                state == GameState.Menu ||
                (state == GameState.Running &&
                 (!hasObservedGameState ||
                  lastObservedGameState == GameState.Boot ||
                  lastObservedGameState == GameState.Menu ||
                  lastObservedGameState == GameState.GameOver));

            lastObservedGameState = state;
            hasObservedGameState = true;

            if (!shouldReset)
            {
                return;
            }

            shownAbilityPopupIds.Clear();
            shownCodexPopupKeys.Clear();
            ResetRunPopupState();
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

        private void StartShowAnimation()
        {
            StopCurrentAnimation();
            isOpen = true;
            isClosing = false;
            SetVisible(true);
            ApplyAnimationState(0f, popupHiddenScale);
            animationRoutine = StartCoroutine(AnimateShow());
        }

        private void StartHideAnimation()
        {
            StopCurrentAnimation();
            isClosing = true;
            animationRoutine = StartCoroutine(AnimateHide());
        }

        private void HideImmediately()
        {
            StopCurrentAnimation();
            isOpen = false;
            isClosing = false;
            SetVisible(false);
            ApplyAnimationState(0f, popupHiddenScale);
        }

        private void StopCurrentAnimation()
        {
            if (animationRoutine == null)
            {
                return;
            }

            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        private IEnumerator AnimateShow()
        {
            float duration = Mathf.Max(0.01f, popupOpenDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float opacity = EaseOutQuad(normalized);
                float scale = Mathf.LerpUnclamped(
                    popupHiddenScale,
                    1f,
                    EaseOutBack(normalized, popupOpenBounce));
                ApplyAnimationState(opacity, scale);
                yield return null;
            }

            ApplyAnimationState(1f, 1f);
            animationRoutine = null;
        }

        private IEnumerator AnimateHide()
        {
            float duration = Mathf.Max(0.01f, popupCloseDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float opacity = Mathf.Lerp(1f, 0f, EaseInQuad(normalized));
                float scale = Mathf.LerpUnclamped(
                    1f,
                    popupHiddenScale,
                    EaseInBack(normalized, popupCloseBounce));
                ApplyAnimationState(opacity, scale);
                yield return null;
            }

            ApplyAnimationState(0f, popupHiddenScale);
            SetVisible(false);
            isOpen = false;
            isClosing = false;
            animationRoutine = null;

            if (popupQueue.Count > 0)
            {
                TryShowNextPopup();
                yield break;
            }

            ResumeGame();
        }

        private void ApplyAnimationState(float opacity, float scale)
        {
            if (panel != null)
            {
                panel.style.opacity = Mathf.Clamp01(opacity);
            }

            if (card != null)
            {
                card.transform.scale = new Vector3(scale, scale, 1f);
            }
        }

        private static float EaseOutQuad(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse;
        }

        private static float EaseInQuad(float value)
        {
            float clamped = Mathf.Clamp01(value);
            return clamped * clamped;
        }

        private static float EaseOutBack(float value, float strength)
        {
            float clamped = Mathf.Clamp01(value) - 1f;
            float overshoot = Mathf.Max(0f, strength);
            return 1f + (overshoot + 1f) * clamped * clamped * clamped + overshoot * clamped * clamped;
        }

        private static float EaseInBack(float value, float strength)
        {
            float clamped = Mathf.Clamp01(value);
            float overshoot = Mathf.Max(0f, strength);
            return (overshoot + 1f) * clamped * clamped * clamped - overshoot * clamped * clamped;
        }
    }
}
