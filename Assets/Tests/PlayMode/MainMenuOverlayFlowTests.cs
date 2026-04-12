using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using EndlessRunner;
using Object = UnityEngine.Object;

public class MainMenuOverlayFlowTests
{
    private const string MainMenuSceneName = "MainMenuScene";
    private const string ManualOverlayName = "mainmenu-collection-overlay";
    private const string AchievementOverlayName = "mainmenu-achievement-overlay";
    private const string LeaderboardOverlayName = "mainmenu-leaderboard-overlay";
    private const string SettingsOverlayName = "mainmenu-settings-overlay";
    private const string ManualCreaturesTabName = "mainmenu-manual-tab-creatures";
    private const string ManualObstaclesTabName = "mainmenu-manual-tab-obstacles";
    private const string ManualCollectionsTabName = "mainmenu-manual-tab-collections";
    private const string ManualProgressLabelName = "mainmenu-collection-progress-label";
    private const string VisibleClass = "is-visible";
    private const string ActiveClass = "is-active";

    [UnitySetUp]
    public IEnumerator SetUpScene()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(MainMenuSceneName);
        while (!load.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Overlays_AreMutuallyExclusive_AndBackActionClosesCurrentOverlay()
    {
        MainMenuSceneController controller = Object.FindAnyObjectByType<MainMenuSceneController>();
        UIDocument uiDocument = Object.FindAnyObjectByType<UIDocument>();

        Assert.IsNotNull(controller, "MainMenuScene should contain MainMenuSceneController.");
        Assert.IsNotNull(uiDocument, "MainMenuScene should contain UIDocument.");

        VisualElement root = uiDocument.rootVisualElement;
        VisualElement manualOverlay = root.Q<VisualElement>(ManualOverlayName);
        VisualElement achievementOverlay = root.Q<VisualElement>(AchievementOverlayName);
        VisualElement leaderboardOverlay = root.Q<VisualElement>(LeaderboardOverlayName);
        VisualElement settingsOverlay = root.Q<VisualElement>(SettingsOverlayName);

        AssertHidden(manualOverlay, "Manual overlay should start hidden.");
        AssertHidden(achievementOverlay, "Achievement overlay should start hidden.");
        AssertHidden(leaderboardOverlay, "Leaderboard overlay should start hidden.");
        AssertHidden(settingsOverlay, "Settings overlay should start hidden.");

        InvokePrivate(controller, "OnCollectionClicked");
        yield return null;

        AssertVisible(manualOverlay, "Manual overlay should open from the main menu.");
        AssertHidden(achievementOverlay, "Opening manual should keep achievements hidden.");
        AssertHidden(leaderboardOverlay, "Opening manual should keep leaderboard hidden.");
        AssertHidden(settingsOverlay, "Opening manual should keep settings hidden.");

        InvokePrivate(controller, "OnSettingsClicked");
        yield return null;

        AssertHidden(manualOverlay, "Opening settings should close the manual overlay.");
        AssertHidden(achievementOverlay, "Opening settings should keep achievements hidden.");
        AssertHidden(leaderboardOverlay, "Opening settings should keep leaderboard hidden.");
        AssertVisible(settingsOverlay, "Settings overlay should be the only active overlay.");

        bool handled = InvokePrivate<bool>(controller, "TryHandleOverlayBackAction");
        yield return null;

        Assert.IsTrue(handled, "Back action should close the currently active overlay.");
        AssertHidden(manualOverlay, "Manual overlay should remain hidden after back action.");
        AssertHidden(achievementOverlay, "Achievement overlay should remain hidden after back action.");
        AssertHidden(leaderboardOverlay, "Leaderboard overlay should remain hidden after back action.");
        AssertHidden(settingsOverlay, "Settings overlay should be closed by back action.");

        InvokePrivate(controller, "OnAchievementClicked");
        yield return null;

        AssertHidden(manualOverlay, "Opening achievements should keep manual hidden.");
        AssertVisible(achievementOverlay, "Achievement overlay should open from the main menu.");
        AssertHidden(leaderboardOverlay, "Opening achievements should keep leaderboard hidden.");
        AssertHidden(settingsOverlay, "Opening achievements should keep settings hidden.");

        InvokePrivate(controller, "OnLeaderboardClicked");
        yield return null;

        AssertHidden(manualOverlay, "Opening leaderboard should keep manual hidden.");
        AssertHidden(achievementOverlay, "Opening leaderboard should close achievements.");
        AssertVisible(leaderboardOverlay, "Leaderboard overlay should become active.");
        AssertHidden(settingsOverlay, "Opening leaderboard should keep settings hidden.");

        InvokePrivate(controller, "OnBackToMainInterfaceClicked");
        yield return null;

        AssertHidden(manualOverlay, "Back to main interface should hide manual.");
        AssertHidden(achievementOverlay, "Back to main interface should hide achievements.");
        AssertHidden(leaderboardOverlay, "Back to main interface should hide leaderboard.");
        AssertHidden(settingsOverlay, "Back to main interface should hide settings.");
    }

    [UnityTest]
    public IEnumerator ManualTabs_SwitchContent_WithoutClosingOverlay()
    {
        MainMenuSceneController controller = Object.FindAnyObjectByType<MainMenuSceneController>();
        UIDocument uiDocument = Object.FindAnyObjectByType<UIDocument>();

        Assert.IsNotNull(controller, "MainMenuScene should contain MainMenuSceneController.");
        Assert.IsNotNull(uiDocument, "MainMenuScene should contain UIDocument.");

        VisualElement root = uiDocument.rootVisualElement;
        VisualElement manualOverlay = root.Q<VisualElement>(ManualOverlayName);
        Button creaturesTab = root.Q<Button>(ManualCreaturesTabName);
        Button obstaclesTab = root.Q<Button>(ManualObstaclesTabName);
        Button collectionsTab = root.Q<Button>(ManualCollectionsTabName);
        Label progressLabel = root.Q<Label>(ManualProgressLabelName);

        Assert.IsNotNull(manualOverlay, "Manual overlay should exist in MainMenuUI.");
        Assert.IsNotNull(creaturesTab, "Creatures tab should exist in MainMenuUI.");
        Assert.IsNotNull(obstaclesTab, "Obstacles tab should exist in MainMenuUI.");
        Assert.IsNotNull(collectionsTab, "Collections tab should exist in MainMenuUI.");
        Assert.IsNotNull(progressLabel, "Manual progress label should exist in MainMenuUI.");

        InvokePrivate(controller, "OnCollectionClicked");
        yield return null;

        AssertVisible(manualOverlay, "Manual overlay should stay open while switching tabs.");
        Assert.IsTrue(creaturesTab.ClassListContains(ActiveClass), "Creatures tab should be active when manual opens.");
        Assert.IsFalse(obstaclesTab.ClassListContains(ActiveClass), "Obstacles tab should start inactive.");
        Assert.IsFalse(collectionsTab.ClassListContains(ActiveClass), "Collections tab should start inactive.");

        InvokePrivate(controller, "OnManualObstaclesClicked");
        yield return null;

        AssertVisible(manualOverlay, "Switching to obstacles should not close the manual overlay.");
        Assert.IsFalse(creaturesTab.ClassListContains(ActiveClass), "Creatures tab should deactivate after switching.");
        Assert.IsTrue(obstaclesTab.ClassListContains(ActiveClass), "Obstacles tab should become active after switching.");
        Assert.IsFalse(collectionsTab.ClassListContains(ActiveClass), "Collections tab should remain inactive after switching to obstacles.");

        InvokePrivate(controller, "OnManualCollectionsClicked");
        yield return null;

        AssertVisible(manualOverlay, "Switching to collections should not close the manual overlay.");
        Assert.IsFalse(creaturesTab.ClassListContains(ActiveClass), "Creatures tab should remain inactive after switching to collections.");
        Assert.IsFalse(obstaclesTab.ClassListContains(ActiveClass), "Obstacles tab should deactivate after switching to collections.");
        Assert.IsTrue(collectionsTab.ClassListContains(ActiveClass), "Collections tab should become active after switching.");
        StringAssert.Contains("Unlocked", progressLabel.text, "Manual progress label should refresh after tab switching.");
    }

    private static void AssertVisible(VisualElement overlay, string message)
    {
        Assert.IsNotNull(overlay, "Expected overlay to exist.");
        Assert.IsTrue(overlay.ClassListContains(VisibleClass), message);
        Assert.AreEqual(DisplayStyle.Flex, overlay.resolvedStyle.display, message);
    }

    private static void AssertHidden(VisualElement overlay, string message)
    {
        Assert.IsNotNull(overlay, "Expected overlay to exist.");
        Assert.IsFalse(overlay.ClassListContains(VisibleClass), message);
        Assert.AreEqual(DisplayStyle.None, overlay.resolvedStyle.display, message);
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
}
