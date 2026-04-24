using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class AchievementManager : MonoBehaviour
    {
        public struct AchievementDefinition
        {
            public string Id;
            public string Title;
            public string Description;
            public int Target;
            public Func<int> GetCurrentValue;
        }

        public static event Action<AchievementDefinition> AchievementUnlocked;

        private static AchievementDefinition[] cachedDefinitions;

        public static AchievementDefinition[] GetDefinitions()
        {
            if (cachedDefinitions == null)
            {
                cachedDefinitions = BuildDefinitions();
            }

            return cachedDefinitions;
        }

        public static int GetTarget(AchievementDefinition def)
        {
            if (def.Target > 0)
            {
                return def.Target;
            }

            if (string.Equals(def.Id, "collector", StringComparison.Ordinal))
            {
                CodexDatabase db = CodexDatabase.Load();
                return db != null ? Mathf.Max(1, db.GetEntryCount(CodexCategory.Collection)) : 1;
            }

            if (string.Equals(def.Id, "mode_pioneer", StringComparison.Ordinal))
            {
                List<RunProgressStore.GameModeProgress> modes = RunProgressStore.GetGameModeProgress();
                return Mathf.Max(1, modes.Count);
            }

            return 1;
        }

        public void CheckAllAchievements()
        {
            AchievementDefinition[] defs = GetDefinitions();
            for (int i = 0; i < defs.Length; i++)
            {
                AchievementDefinition def = defs[i];
                if (RunProgressStore.IsAchievementCompleted(def.Id))
                {
                    continue;
                }

                int current = def.GetCurrentValue != null ? def.GetCurrentValue() : 0;
                int target = GetTarget(def);
                if (current >= target)
                {
                    if (RunProgressStore.CompleteAchievement(def.Id))
                    {
                        AchievementUnlocked?.Invoke(def);
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                CheckAllAchievements();
            }
        }

        private static int CountUnlockedModes()
        {
            List<RunProgressStore.GameModeProgress> modes = RunProgressStore.GetGameModeProgress();
            int count = 0;
            for (int i = 0; i < modes.Count; i++)
            {
                if (modes[i].Unlocked)
                {
                    count++;
                }
            }

            return count;
        }

        private static AchievementDefinition[] BuildDefinitions()
        {
            return new[]
            {
                new AchievementDefinition
                {
                    Id = "first_dive",
                    Title = "First Dive",
                    Description = "Complete your first run.",
                    Target = 1,
                    GetCurrentValue = () => RunProgressStore.GetTotalRuns()
                },
                new AchievementDefinition
                {
                    Id = "seasoned_runner",
                    Title = "Seasoned Runner",
                    Description = "Complete 10 runs.",
                    Target = 10,
                    GetCurrentValue = () => RunProgressStore.GetTotalRuns()
                },
                new AchievementDefinition
                {
                    Id = "deep_scout",
                    Title = "Deep Scout",
                    Description = "Reach a best score of 600.",
                    Target = 600,
                    GetCurrentValue = () => RunProgressStore.GetBestScore()
                },
                new AchievementDefinition
                {
                    Id = "abyss_challenger",
                    Title = "Abyss Challenger",
                    Description = "Reach a best score of 1200.",
                    Target = 1200,
                    GetCurrentValue = () => RunProgressStore.GetBestScore()
                },
                new AchievementDefinition
                {
                    Id = "collector",
                    Title = "Collector",
                    Description = "Unlock all collection entries.",
                    Target = -1,
                    GetCurrentValue = () => RunProgressStore.GetUnlockedCollectionCount()
                },
                new AchievementDefinition
                {
                    Id = "relic_hoarder",
                    Title = "Relic Hoarder",
                    Description = "Collect 15 relics in total.",
                    Target = 15,
                    GetCurrentValue = () => RunProgressStore.GetTotalCollectionPickups()
                },
                new AchievementDefinition
                {
                    Id = "mode_pioneer",
                    Title = "Mode Pioneer",
                    Description = "Unlock all game modes.",
                    Target = -1,
                    GetCurrentValue = () => CountUnlockedModes()
                }
            };
        }
    }
}
