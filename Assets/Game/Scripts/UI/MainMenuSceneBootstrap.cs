using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRunner
{
    public static class MainMenuSceneBootstrap
    {
        private const string MainMenuSceneName = "MainMenuScene";
        private static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureMainMenuController(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureMainMenuController(scene);
        }

        private static void EnsureMainMenuController(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.name, MainMenuSceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (UnityEngine.Object.FindAnyObjectByType<MainMenuSceneController>() != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject("MainMenuSceneController");
            bootstrap.AddComponent<MainMenuSceneController>();
        }
    }
}
