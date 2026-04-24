using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using EndlessRunner;
using Object = UnityEngine.Object;

public class PauseManualFlowTests
{
    private const string SampleSceneName = "SampleScene";
    private const string MenuPanelName = "menu-panel";
    private const string PausePanelName = "pause-panel";
    private const string CodexPanelName = "codex-panel";
    private const string AbilityPanelName = "ability-acquired-panel";
    private const string AbilityTitleName = "ability-acquired-title";
    private const string PauseButtonName = "pause-button";
    private const string PauseCollectionButtonName = "pause-collection-button";

    [UnitySetUp]
    public IEnumerator SetUpScene()
    {
        Time.timeScale = 1f;

        AsyncOperation load = SceneManager.LoadSceneAsync(SampleSceneName);
        while (!load.isDone)
        {
            yield return null;
        }

        yield return null;
    }

    [TearDown]
    public void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator PauseMenu_CanOpenManualCloseAndResume()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        HUDController hudController = Object.FindAnyObjectByType<HUDController>();
        UIDocument uiDocument = UIDocumentLocator.FindGameplayDocument();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(hudController, "SampleScene should contain HUDController.");
        Assert.IsNotNull(uiDocument, "SampleScene should contain UIDocument.");

        gameManager.BeginRun();
        yield return null;

        VisualElement root = uiDocument.rootVisualElement;
        VisualElement menuPanel = root.Q<VisualElement>(MenuPanelName);
        VisualElement pausePanel = root.Q<VisualElement>(PausePanelName);
        VisualElement codexPanel = root.Q<VisualElement>(CodexPanelName);

        AssertDisplay(menuPanel, DisplayStyle.None, "Pre-run menu should start hidden during gameplay.");
        AssertDisplay(pausePanel, DisplayStyle.None, "Pause menu should start hidden during gameplay.");
        AssertDisplay(codexPanel, DisplayStyle.None, "Manual overlay should start hidden during gameplay.");

        InvokePrivate(hudController, "OnPauseClicked");
        yield return null;

        Assert.AreEqual(GameState.Paused, gameManager.State, "Pause button should pause gameplay.");
        Assert.AreEqual(0f, Time.timeScale, "Gameplay should stop while pause menu is open.");
        AssertDisplay(menuPanel, DisplayStyle.None, "Pre-run menu should stay hidden while gameplay is paused.");
        AssertDisplay(pausePanel, DisplayStyle.Flex, "Pause menu should be visible after pausing.");
        AssertDisplay(codexPanel, DisplayStyle.None, "Manual overlay should remain hidden until opened.");

        InvokePrivate(hudController, "OnCollectionClicked");
        yield return null;

        Assert.AreEqual(GameState.Paused, gameManager.State, "Opening the manual should keep the game paused.");
        Assert.AreEqual(0f, Time.timeScale, "Manual overlay should not resume gameplay.");
        AssertDisplay(menuPanel, DisplayStyle.None, "Pre-run menu should remain hidden while gameplay manual is open.");
        AssertDisplay(pausePanel, DisplayStyle.None, "Manual overlay should replace the pause menu while it is open.");
        AssertDisplay(codexPanel, DisplayStyle.Flex, "Manual overlay should be visible after opening it.");

        InvokePrivate(hudController, "OnCodexCloseClicked");
        yield return null;

        Assert.AreEqual(GameState.Paused, gameManager.State, "Closing the manual should return to paused state.");
        Assert.AreEqual(0f, Time.timeScale, "Closing the manual should not resume gameplay by itself.");
        AssertDisplay(menuPanel, DisplayStyle.None, "Pre-run menu should remain hidden after closing the gameplay manual.");
        AssertDisplay(pausePanel, DisplayStyle.Flex, "Pause menu should still be visible after closing the manual.");
        AssertDisplay(codexPanel, DisplayStyle.None, "Manual overlay should be hidden after closing it.");

        InvokePrivate(hudController, "OnContinueClicked");
        yield return null;

        Assert.AreEqual(GameState.Running, gameManager.State, "Continue should resume gameplay.");
        Assert.AreEqual(1f, Time.timeScale, "Continue should restore normal timescale.");
        AssertDisplay(menuPanel, DisplayStyle.None, "Pre-run menu should be hidden after resuming.");
        AssertDisplay(pausePanel, DisplayStyle.None, "Pause menu should be hidden after resuming.");
        AssertDisplay(codexPanel, DisplayStyle.None, "Manual overlay should stay hidden after resuming.");
    }

    [UnityTest]
    public IEnumerator AbilityPopup_QueuedEntriesKeepPauseUntilLastDismissed()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        AbilityAcquiredUI popup = Object.FindAnyObjectByType<AbilityAcquiredUI>();
        UIDocument uiDocument = UIDocumentLocator.FindGameplayDocument();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(popup, "SampleScene should contain AbilityAcquiredUI.");
        Assert.IsNotNull(uiDocument, "SampleScene should contain UIDocument.");

        gameManager.BeginRun();
        yield return null;

        popup.ShowCodexEntry(CodexCategory.Creature, new CodexEntry
        {
            id = "test-creature-a",
            title = "Test Creature A",
            description = "First popup entry.",
            abilityId = "bite"
        });
        popup.ShowCodexEntry(CodexCategory.Collection, new CodexEntry
        {
            id = "test-collection-b",
            title = "Test Collection B",
            description = "Second popup entry.",
            unlockHint = "Found deeper in the run."
        });
        yield return null;

        VisualElement root = uiDocument.rootVisualElement;
        VisualElement abilityPanel = root.Q<VisualElement>(AbilityPanelName);
        VisualElement menuPanel = root.Q<VisualElement>(MenuPanelName);
        Label titleLabel = root.Q<Label>(AbilityTitleName);

        Assert.IsNotNull(abilityPanel, "Ability acquired panel should exist in GameUI.");
        Assert.IsNotNull(titleLabel, "Ability acquired title label should exist in GameUI.");
        Assert.IsTrue(abilityPanel.ClassListContains("is-visible"), "Popup should become visible after first discovery.");
        Assert.AreEqual("Test Creature A", titleLabel.text, "First queued popup should display first.");
        Assert.AreEqual(GameState.Paused, gameManager.State, "First discovery popup should pause gameplay.");
        Assert.AreEqual(0f, Time.timeScale, "First discovery popup should stop timescale.");
        AssertDisplay(menuPanel, DisplayStyle.None, "Pause menu should not open when discovery popup pauses the game.");

        InvokePrivate(popup, "Hide");
        yield return WaitUntil(
            () =>
            {
                VisualElement currentPanel = uiDocument.rootVisualElement.Q<VisualElement>(AbilityPanelName);
                Label currentTitleLabel = uiDocument.rootVisualElement.Q<Label>(AbilityTitleName);
                return currentPanel != null &&
                       currentTitleLabel != null &&
                       string.Equals(currentTitleLabel.text, "Test Collection B");
            },
            2f,
            "Second queued popup should replace the first after dismissal.");

        abilityPanel = uiDocument.rootVisualElement.Q<VisualElement>(AbilityPanelName);
        titleLabel = uiDocument.rootVisualElement.Q<Label>(AbilityTitleName);

        Assert.IsTrue(abilityPanel.ClassListContains("is-visible"), "Second queued popup should appear immediately.");
        Assert.AreEqual("Test Collection B", titleLabel.text, "Second queued popup should replace the first after dismissal.");
        Assert.AreEqual(GameState.Paused, gameManager.State, "Queued popup handoff should keep gameplay paused.");
        Assert.AreEqual(0f, Time.timeScale, "Queued popup handoff should keep timescale at zero.");

        InvokePrivate(popup, "Hide");
        yield return WaitUntil(
            () =>
            {
                VisualElement currentPanel = uiDocument.rootVisualElement.Q<VisualElement>(AbilityPanelName);
                bool isVisible = currentPanel != null && currentPanel.ClassListContains("is-visible");
                return !isVisible &&
                       gameManager.State == GameState.Running &&
                       Mathf.Approximately(Time.timeScale, 1f);
            },
            2f,
            "Popup should fully dismiss and resume gameplay after the last queued entry closes.");

        abilityPanel = uiDocument.rootVisualElement.Q<VisualElement>(AbilityPanelName);

        Assert.IsFalse(abilityPanel.ClassListContains("is-visible"), "Popup should be hidden after the last dismissal.");
        Assert.AreEqual(GameState.Running, gameManager.State, "Gameplay should resume only after the last popup closes.");
        Assert.AreEqual(1f, Time.timeScale, "Gameplay should restore normal timescale after the last popup closes.");
    }

    [UnityTest]
    public IEnumerator PauseButtonCenter_IsRecognizedAsUiByInputRouterAndHudFallback()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        HUDController hudController = Object.FindAnyObjectByType<HUDController>();
        InputRouter inputRouter = Object.FindAnyObjectByType<InputRouter>();
        UIDocument uiDocument = UIDocumentLocator.FindGameplayDocument();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(hudController, "SampleScene should contain HUDController.");
        Assert.IsNotNull(inputRouter, "SampleScene should contain InputRouter.");
        Assert.IsNotNull(uiDocument, "SampleScene should contain UIDocument.");

        gameManager.BeginRun();
        yield return null;
        yield return null;

        VisualElement root = uiDocument.rootVisualElement;
        Button pauseButton = root.Q<Button>(PauseButtonName);

        Assert.IsNotNull(pauseButton, "Pause button should exist in GameUI.");
        Assert.Greater(pauseButton.worldBound.width, 0f, "Pause button should have a valid layout width.");
        Assert.Greater(pauseButton.worldBound.height, 0f, "Pause button should have a valid layout height.");

        Vector2 pauseScreenPosition = ToScreenPosition(root, pauseButton.worldBound.center);
        bool overUi = InvokePrivate<bool>(inputRouter, "IsScreenPositionOverUi", pauseScreenPosition);

        Assert.IsTrue(overUi, "Pause button center should be treated as UI by InputRouter.");

        object[] fallbackArgs = { pauseScreenPosition, null };
        bool fallbackHit = InvokePrivate<bool>(hudController, "TryGetFallbackButtonAtScreenPosition", fallbackArgs);
        Button hitButton = fallbackArgs[1] as Button;

        Assert.IsTrue(fallbackHit, "HUD fallback hit-test should resolve the pause button.");
        Assert.AreSame(pauseButton, hitButton, "HUD fallback should resolve the actual pause button.");
    }

    [UnityTest]
    public IEnumerator PauseManualButtonCenter_MapsToGameplayManualOverlay()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        HUDController hudController = Object.FindAnyObjectByType<HUDController>();
        InputRouter inputRouter = Object.FindAnyObjectByType<InputRouter>();
        UIDocument uiDocument = UIDocumentLocator.FindGameplayDocument();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(hudController, "SampleScene should contain HUDController.");
        Assert.IsNotNull(inputRouter, "SampleScene should contain InputRouter.");
        Assert.IsNotNull(uiDocument, "SampleScene should contain UIDocument.");

        gameManager.BeginRun();
        yield return null;
        yield return null;

        InvokePrivate(hudController, "OnPauseClicked");
        yield return null;

        VisualElement root = uiDocument.rootVisualElement;
        Button pauseCollectionButton = root.Q<Button>(PauseCollectionButtonName);

        Assert.IsNotNull(pauseCollectionButton, "Pause-menu manual button should exist in GameUI.");
        Assert.Greater(pauseCollectionButton.worldBound.width, 0f, "Pause-menu manual button should have a valid layout width.");
        Assert.Greater(pauseCollectionButton.worldBound.height, 0f, "Pause-menu manual button should have a valid layout height.");

        Vector2 manualScreenPosition = ToScreenPosition(root, pauseCollectionButton.worldBound.center);
        bool overUi = InvokePrivate<bool>(inputRouter, "IsScreenPositionOverUi", manualScreenPosition);

        Assert.IsTrue(overUi, "Pause-menu manual button center should be treated as UI by InputRouter.");

        object[] fallbackArgs = { manualScreenPosition, null };
        bool fallbackHit = InvokePrivate<bool>(hudController, "TryGetFallbackButtonAtScreenPosition", fallbackArgs);
        Button hitButton = fallbackArgs[1] as Button;

        Assert.IsTrue(fallbackHit, "HUD fallback hit-test should resolve the pause-menu manual button.");
        Assert.AreSame(pauseCollectionButton, hitButton, "Pause-menu manual button should not be mistaken for another menu action.");

        InvokePrivate(hudController, "OnCollectionClicked");
        yield return null;

        VisualElement menuPanel = root.Q<VisualElement>(MenuPanelName);
        VisualElement pausePanel = root.Q<VisualElement>(PausePanelName);
        VisualElement codexPanel = root.Q<VisualElement>(CodexPanelName);

        AssertDisplay(menuPanel, DisplayStyle.None, "Gameplay manual should stay in-scene instead of switching to another scene menu.");
        AssertDisplay(pausePanel, DisplayStyle.None, "Pause menu should be hidden while the gameplay manual overlay is open.");
        AssertDisplay(codexPanel, DisplayStyle.Flex, "Gameplay manual should open its overlay in the current gameplay scene.");
        Assert.AreEqual(GameState.Paused, gameManager.State, "Opening the gameplay manual should preserve paused state.");
    }

    private static void AssertDisplay(VisualElement element, DisplayStyle expected, string message)
    {
        Assert.IsNotNull(element, "Expected UI element to exist.");
        Assert.AreEqual(expected, element.resolvedStyle.display, message);
    }

    private static Vector2 ToScreenPosition(VisualElement root, Vector2 panelPosition)
    {
        Rect rootBounds = root.worldBound;
        Assert.Greater(rootBounds.width, 0f, "UI root should have a valid layout width.");
        Assert.Greater(rootBounds.height, 0f, "UI root should have a valid layout height.");

        float normalizedX = (panelPosition.x - rootBounds.xMin) / rootBounds.width;
        float normalizedY = (panelPosition.y - rootBounds.yMin) / rootBounds.height;
        return new Vector2(normalizedX * Screen.width, Screen.height - normalizedY * Screen.height);
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Expected private method {methodName} on {target.GetType().Name}.");
        method.Invoke(target, arguments);
    }

    private static T InvokePrivate<T>(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Expected private method {methodName} on {target.GetType().Name}.");
        object result = method.Invoke(target, arguments);
        return result is T typedResult ? typedResult : default;
    }

    private static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds, string failureMessage)
    {
        Assert.IsNotNull(predicate, "WaitUntil requires a predicate.");
        float deadline = Time.realtimeSinceStartup + Mathf.Max(0.01f, timeoutSeconds);

        while (Time.realtimeSinceStartup < deadline)
        {
            if (predicate())
            {
                yield break;
            }

            yield return null;
        }

        Assert.IsTrue(predicate(), failureMessage);
    }
}
