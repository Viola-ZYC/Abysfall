using System.IO;
using UnityEditor;
using UnityEngine;

namespace EndlessRunner.EditorTools
{
    public static class RunProgressTools
    {
        [MenuItem("Tools/Progress/Clear Codex Progress")]
        public static void ClearCodexProgress()
        {
            string savePath = RunProgressStore.GetSavePathForDebug();
            if (!EditorUtility.DisplayDialog(
                    "Clear Codex Progress",
                    $"Clear creature and collectible unlock history?\n\nSave file:\n{savePath}\n\nScore, run count, mode unlocks, and leaderboard will be kept.",
                    "Clear Codex",
                    "Cancel"))
            {
                return;
            }

            RunProgressStore.ResetCodexProgress();
            Debug.Log($"Codex progress cleared. Save file: {savePath}");
        }

        [MenuItem("Tools/Progress/Clear All Run Progress")]
        public static void ClearAllRunProgress()
        {
            string savePath = RunProgressStore.GetSavePathForDebug();
            if (!EditorUtility.DisplayDialog(
                    "Clear All Run Progress",
                    $"Reset all run progress and discovery history?\n\nSave file:\n{savePath}\n\nThis will also clear scores, runs, mode progress, and leaderboard.",
                    "Clear All Progress",
                    "Cancel"))
            {
                return;
            }

            RunProgressStore.ResetAllProgress();
            Debug.Log($"All run progress cleared. Save file: {savePath}");
        }

        [MenuItem("Tools/Progress/Reveal Save File")]
        public static void RevealSaveFile()
        {
            string savePath = RunProgressStore.GetSavePathForDebug();
            string target = File.Exists(savePath) ? savePath : Path.GetDirectoryName(savePath);
            if (string.IsNullOrWhiteSpace(target))
            {
                Debug.LogWarning("Run progress save path could not be resolved.");
                return;
            }

            EditorUtility.RevealInFinder(target);
            Debug.Log($"Revealed run progress path: {target}");
        }
    }
}
