using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EndlessRunner.Tests
{
    public class ResourceSmokeTests
    {
        [UnityTest]
        public IEnumerator UiResourcesAreAvailable()
        {
            VisualTreeAsset gameUi = Resources.Load<VisualTreeAsset>("UI/GameUI");
            VisualTreeAsset mainMenuUi = Resources.Load<VisualTreeAsset>("UI/MainMenuUI");
            PanelSettings panelSettings = Resources.Load<PanelSettings>("UI/GamePanelSettings");
            Font pixelFont = Resources.Load<Font>("UI/PressStart2P-Regular");

            Assert.IsNotNull(gameUi, "UI/GameUI should exist in Resources.");
            Assert.IsNotNull(mainMenuUi, "UI/MainMenuUI should exist in Resources.");
            Assert.IsNotNull(panelSettings, "UI/GamePanelSettings should exist in Resources.");
            Assert.IsNotNull(pixelFont, "UI/PressStart2P-Regular should exist in Resources.");

            yield return null;
        }
    }
}
