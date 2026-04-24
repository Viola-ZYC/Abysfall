using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EndlessRunner
{
    public class AchievementToastUI : MonoBehaviour
    {
        private const string VisibleClass = "is-visible";
        private const float DisplayDuration = 3f;
        private const float GapBetweenToasts = 0.5f;

        private VisualElement toastPanel;
        private Label titleLabel;
        private Label descLabel;
        private Coroutine showRoutine;
        private readonly Queue<AchievementManager.AchievementDefinition> pendingToasts = new();

        private void OnEnable()
        {
            AchievementManager.AchievementUnlocked += OnAchievementUnlocked;
        }

        private void OnDisable()
        {
            AchievementManager.AchievementUnlocked -= OnAchievementUnlocked;
        }

        private void OnAchievementUnlocked(AchievementManager.AchievementDefinition def)
        {
            pendingToasts.Enqueue(def);
            if (showRoutine == null)
            {
                CacheElements();
                showRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (pendingToasts.Count > 0)
            {
                AchievementManager.AchievementDefinition def = pendingToasts.Dequeue();
                ShowToast(def.Title, def.Description);
                yield return new WaitForSecondsRealtime(DisplayDuration);
                HideToast();

                if (pendingToasts.Count > 0)
                {
                    yield return new WaitForSecondsRealtime(GapBetweenToasts);
                }
            }

            showRoutine = null;
        }

        private void ShowToast(string title, string description)
        {
            if (toastPanel == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = title;
            }

            if (descLabel != null)
            {
                descLabel.text = description;
            }

            toastPanel.EnableInClassList(VisibleClass, true);
        }

        private void HideToast()
        {
            if (toastPanel != null)
            {
                toastPanel.EnableInClassList(VisibleClass, false);
            }
        }

        private void CacheElements()
        {
            if (toastPanel != null)
            {
                return;
            }

            UIDocument doc = UIDocumentLocator.FindGameplayDocument();
            if (doc == null)
            {
                return;
            }

            VisualElement root = doc.rootVisualElement;
            if (root == null)
            {
                return;
            }

            toastPanel = root.Q<VisualElement>("achievement-toast");
            if (toastPanel == null)
            {
                return;
            }

            titleLabel = toastPanel.Q<Label>("achievement-toast-title");
            descLabel = toastPanel.Q<Label>("achievement-toast-desc");
        }
    }
}
