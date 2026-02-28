using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRunner.EditorTools
{
    [InitializeOnLoad]
    public static class PlayModeSceneGuard
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        static PlayModeSceneGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.path.StartsWith("Temp/__Backupscenes/"))
            {
                return;
            }

            // Prevent entering Play Mode from Unity auto-backup scenes, which often look empty.
            EditorApplication.isPlaying = false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) != null)
            {
                EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
                Debug.LogWarning("Detected backup scene. Switched to Assets/Scenes/SampleScene.unity. Press Play again.");
            }
            else
            {
                Debug.LogError("Detected backup scene, but Assets/Scenes/SampleScene.unity was not found.");
            }
        }
    }
}

