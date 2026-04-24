using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace EndlessRunner
{
    public class TutorialOverlayController : MonoBehaviour
    {
        private const string PanelName = "tutorial-panel";
        private const string VisibleClass = "is-visible";
        private const string WaitingClass = "is-waiting";
        private const float StartDelay = 1.5f;
        private const float MoveThreshold = 0.8f;

        private UIDocument uiDocument;
        private VisualElement panel;
        private Label stepLabel;
        private Label titleLabel;
        private Label bodyLabel;
        private Label hintLabel;

        private GameManager gameManager;
        private RunnerController runner;
        private AbilityManager abilityManager;
        private Coroutine tutorialRoutine;
        private bool pauseRequested;
        private bool waitingForTap;
        private bool tapped;

        private bool step1Done;
        private bool step2Done;
        private bool step3Done;
        private bool tutorialActive;

        private struct TutorialStep
        {
            public string StepText;
            public string Title;
            public string Body;
            public string Hint;
        }

        private static readonly TutorialStep StepMove = new TutorialStep
        {
            StepText = "Step 1/3",
            Title = "Horizontal Movement",
            Body = "Drag or swipe left and right to dodge obstacles and collect items.",
            Hint = "Try moving now!"
        };

        private static readonly TutorialStep StepStomp = new TutorialStep
        {
            StepText = "Step 2/3",
            Title = "Brake & Stomp",
            Body = "Your character dives automatically. Hitting creatures from above deals damage. Avoid side collisions!",
            Hint = "Tap to continue"
        };

        private static readonly TutorialStep StepAbility = new TutorialStep
        {
            StepText = "Step 3/3",
            Title = "Abilities",
            Body = "Defeat creatures to unlock abilities. Active abilities can be triggered with the button at the bottom of the screen.",
            Hint = "Tap to continue"
        };

        public static bool ForceNextRun { get; set; }

        public static bool ShouldShowTutorial()
        {
            if (ForceNextRun)
            {
                return true;
            }

            return RunProgressStore.GetTotalRuns() == 0 && !RunProgressStore.IsTutorialCompleted();
        }

        public void StartTutorial()
        {
            ForceNextRun = false;

            if (tutorialActive)
            {
                return;
            }

            CacheElements();
            if (panel == null)
            {
                return;
            }

            gameManager = GameManager.Instance;
            runner = FindAnyObjectByType<RunnerController>();
            abilityManager = FindAnyObjectByType<AbilityManager>();
            tutorialActive = true;
            step1Done = false;
            step2Done = false;
            step3Done = false;

            SubscribeEvents();
            tutorialRoutine = StartCoroutine(RunStep1());
        }

        private void OnDestroy()
        {
            CancelTutorial();
        }

        private void CancelTutorial()
        {
            if (tutorialRoutine != null)
            {
                StopCoroutine(tutorialRoutine);
                tutorialRoutine = null;
            }

            UnsubscribeEvents();
            HidePanel();
            ReleasePause();
            tutorialActive = false;

            if (panel != null)
            {
                panel.UnregisterCallback<ClickEvent>(HandleClick);
            }
        }

        private void SubscribeEvents()
        {
            if (runner != null)
            {
                runner.CreatureStomped += OnCreatureStomped;
            }

            if (abilityManager != null)
            {
                abilityManager.AbilityAcquired += OnAbilityAcquired;
            }
        }

        private void UnsubscribeEvents()
        {
            if (runner != null)
            {
                runner.CreatureStomped -= OnCreatureStomped;
            }

            if (abilityManager != null)
            {
                abilityManager.AbilityAcquired -= OnAbilityAcquired;
            }
        }

        private void OnCreatureStomped(CreatureBase creature)
        {
            if (!tutorialActive || step2Done)
            {
                return;
            }

            step2Done = true;
            if (tutorialRoutine != null)
            {
                StopCoroutine(tutorialRoutine);
            }

            tutorialRoutine = StartCoroutine(RunReactiveStep(StepStomp, CheckAllDone));
        }

        private void OnAbilityAcquired(AbilityDefinition def, int level)
        {
            if (!tutorialActive || step3Done)
            {
                return;
            }

            step3Done = true;
            if (tutorialRoutine != null)
            {
                StopCoroutine(tutorialRoutine);
            }

            tutorialRoutine = StartCoroutine(RunReactiveStep(StepAbility, CheckAllDone));
        }

        private void CacheElements()
        {
            if (panel != null)
            {
                return;
            }

            uiDocument = UIDocumentLocator.FindGameplayDocument();
            if (uiDocument == null)
            {
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            panel = root.Q<VisualElement>(PanelName);
            if (panel == null)
            {
                return;
            }

            stepLabel = panel.Q<Label>("tutorial-step-label");
            titleLabel = panel.Q<Label>("tutorial-title");
            bodyLabel = panel.Q<Label>("tutorial-body");
            hintLabel = panel.Q<Label>("tutorial-hint");
            panel.RegisterCallback<ClickEvent>(HandleClick);
        }

        private IEnumerator RunStep1()
        {
            yield return new WaitForSecondsRealtime(StartDelay);

            if (IsGameOver())
            {
                FinishTutorial();
                yield break;
            }

            SetStepContent(StepMove);
            ShowPanel();
            RequestPause();
            yield return WaitForTap();
            HidePanel();
            ReleasePause();

            if (hintLabel != null)
            {
                hintLabel.EnableInClassList(WaitingClass, true);
            }

            yield return WaitForPlayerMove();

            if (hintLabel != null)
            {
                hintLabel.EnableInClassList(WaitingClass, false);
            }

            step1Done = true;
            tutorialRoutine = null;
            CheckAllDone();
        }

        private IEnumerator RunReactiveStep(TutorialStep step, System.Action onDone)
        {
            if (IsGameOver())
            {
                FinishTutorial();
                yield break;
            }

            SetStepContent(step);
            ShowPanel();
            RequestPause();
            yield return WaitForTap();
            HidePanel();
            ReleasePause();
            tutorialRoutine = null;
            onDone?.Invoke();
        }

        private void CheckAllDone()
        {
            if (step1Done && step2Done && step3Done)
            {
                FinishTutorial();
            }
        }

        private void FinishTutorial()
        {
            UnsubscribeEvents();
            HidePanel();
            ReleasePause();
            RunProgressStore.SetTutorialCompleted();
            tutorialActive = false;
            tutorialRoutine = null;

            if (panel != null)
            {
                panel.UnregisterCallback<ClickEvent>(HandleClick);
            }
        }

        private void SetStepContent(TutorialStep step)
        {
            if (stepLabel != null) stepLabel.text = step.StepText;
            if (titleLabel != null) titleLabel.text = step.Title;
            if (bodyLabel != null) bodyLabel.text = step.Body;
            if (hintLabel != null) hintLabel.text = step.Hint;
        }

        private void ShowPanel()
        {
            if (panel != null)
            {
                panel.EnableInClassList(VisibleClass, true);
            }
        }

        private void HidePanel()
        {
            if (panel != null)
            {
                panel.EnableInClassList(VisibleClass, false);
            }
        }

        private void RequestPause()
        {
            if (pauseRequested || gameManager == null)
            {
                return;
            }

            gameManager.RequestPause(this);
            pauseRequested = true;
        }

        private void ReleasePause()
        {
            if (!pauseRequested || gameManager == null)
            {
                return;
            }

            gameManager.ReleasePause(this);
            pauseRequested = false;
        }

        private IEnumerator WaitForTap()
        {
            waitingForTap = true;
            tapped = false;
            while (!tapped)
            {
                if (IsGameOver())
                {
                    break;
                }

                yield return null;
            }

            waitingForTap = false;
        }

        private IEnumerator WaitForPlayerMove()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }

            if (runner == null)
            {
                yield return new WaitForSeconds(2f);
                yield break;
            }

            float startX = runner.transform.position.x;
            while (Mathf.Abs(runner.transform.position.x - startX) < MoveThreshold)
            {
                if (IsGameOver())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private bool IsGameOver()
        {
            return gameManager != null && gameManager.State == GameState.GameOver;
        }

        private void HandleClick(ClickEvent evt)
        {
            if (!waitingForTap)
            {
                return;
            }

            evt.StopPropagation();
            tapped = true;
        }

        private void Update()
        {
            if (!tutorialActive)
            {
                return;
            }

            if (IsGameOver())
            {
                CancelTutorial();
                RunProgressStore.SetTutorialCompleted();
            }
        }
    }
}
