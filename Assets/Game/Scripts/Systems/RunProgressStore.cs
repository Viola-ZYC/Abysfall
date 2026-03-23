using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EndlessRunner
{
    public static class RunProgressStore
    {
        [Serializable]
        private class SaveData
        {
            public int version = 3;
            public int bestScore;
            public int lastScore;
            public int totalRuns;
            public int totalScore;
            public int unlockedCollectionCount;
            public int collectionUnlockMask;
            public List<int> collectionEntryCounts = new List<int>();
            public List<string> unlockedCreatureIds = new List<string>();
            public List<string> unlockedObstacleIds = new List<string>();
            public List<string> unlockedAbilityIds = new List<string>();
            public List<CodexCountEntry> collectionCounts = new List<CodexCountEntry>();
            public string selectedModeId = ModeClassic;
            public List<ModeState> modeStates = new List<ModeState>();
            public List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
        }

        [Serializable]
        private class CodexCountEntry
        {
            public string id;
            public int count;
        }

        [Serializable]
        private class ModeState
        {
            public string modeId;
            public bool unlocked;
        }

        [Serializable]
        public class LeaderboardEntry
        {
            public int score;
            public string modeId;
            public string utcTime;
        }

        private class ModeDefinition
        {
            public string modeId;
            public string displayName;
            public int requiredBestScore;
            public int requiredRuns;

            public ModeDefinition(string modeId, string displayName, int requiredBestScore, int requiredRuns)
            {
                this.modeId = modeId;
                this.displayName = displayName;
                this.requiredBestScore = requiredBestScore;
                this.requiredRuns = requiredRuns;
            }
        }

        public struct GameModeProgress
        {
            public string ModeId;
            public string DisplayName;
            public bool Unlocked;
            public int RequiredBestScore;
            public int RequiredRuns;
            public int CurrentBestScore;
            public int CurrentRuns;
            public float Progress01;
        }

        public const string ModeClassic = "classic";
        public const string ModeHard = "hard";
        public const string ModeAbyss = "abyss";

        private const string SaveFileName = "run_progress.json";
        private const int MaxLeaderboardEntries = 20;
        private const string BestScoreKey = "progress.best_score";
        private const string LastScoreKey = "progress.last_score";
        private const string TotalRunsKey = "progress.total_runs";
        private const string TotalScoreKey = "progress.total_score";
        private const string CollectionUnlockedCountKey = "collection.unlocked_count";
        private const int MaxCollectionEntries = 32;

        private static readonly ModeDefinition[] ModeDefinitions =
        {
            new ModeDefinition(ModeClassic, "Classic", 0, 0),
            new ModeDefinition(ModeHard, "Hard", 400, 3),
            new ModeDefinition(ModeAbyss, "Abyss", 900, 8)
        };

        private static SaveData cachedData;
        private static bool loaded;

        public static int GetBestScore()
        {
            SaveData data = GetData();
            return Mathf.Max(0, data.bestScore);
        }

        public static int GetLastScore()
        {
            SaveData data = GetData();
            return Mathf.Max(0, data.lastScore);
        }

        public static int GetTotalRuns()
        {
            SaveData data = GetData();
            return Mathf.Max(0, data.totalRuns);
        }

        public static int GetTotalScore()
        {
            SaveData data = GetData();
            return Mathf.Max(0, data.totalScore);
        }

        public static float GetAverageScore()
        {
            SaveData data = GetData();
            if (data.totalRuns <= 0)
            {
                return 0f;
            }

            return (float)data.totalScore / data.totalRuns;
        }

        public static int GetUnlockedCollectionCount()
        {
            SaveData data = GetData();
            return CountCollectionUnlocked(data);
        }

        public static bool IsCollectionEntryUnlocked(int entryIndex)
        {
            string entryId = ResolveCollectionEntryId(entryIndex);
            if (string.IsNullOrWhiteSpace(entryId))
            {
                if (entryIndex < 0 || entryIndex >= MaxCollectionEntries)
                {
                    return false;
                }

                SaveData legacy = GetData();
                int bit = 1 << entryIndex;
                return (legacy.collectionUnlockMask & bit) != 0;
            }

            return IsCodexEntryUnlocked(CodexCategory.Collection, entryId);
        }

        public static bool UnlockCollectionEntry(int entryIndex)
        {
            string entryId = ResolveCollectionEntryId(entryIndex);
            if (!string.IsNullOrWhiteSpace(entryId))
            {
                return UnlockCodexEntry(CodexCategory.Collection, entryId, 1);
            }

            if (entryIndex < 0 || entryIndex >= MaxCollectionEntries)
            {
                return false;
            }

            SaveData data = GetData();
            EnsureCollectionEntryCountSize(data);
            int bit = 1 << entryIndex;
            bool newlyUnlocked = (data.collectionUnlockMask & bit) == 0;
            if (newlyUnlocked)
            {
                data.collectionUnlockMask |= bit;
            }

            int currentCount = Mathf.Max(0, data.collectionEntryCounts[entryIndex]);
            data.collectionEntryCounts[entryIndex] = currentCount + 1;
            data.unlockedCollectionCount = CountUnlockedCollections(data.collectionUnlockMask);
            SaveDataToDisk(data);
            return newlyUnlocked;
        }

        public static int GetCollectionEntryCount(int entryIndex)
        {
            string entryId = ResolveCollectionEntryId(entryIndex);
            if (!string.IsNullOrWhiteSpace(entryId))
            {
                return GetCodexEntryCount(CodexCategory.Collection, entryId);
            }

            if (entryIndex < 0 || entryIndex >= MaxCollectionEntries)
            {
                return 0;
            }

            SaveData data = GetData();
            EnsureCollectionEntryCountSize(data);
            return Mathf.Max(0, data.collectionEntryCounts[entryIndex]);
        }

        public static int GetTotalCollectionPickups()
        {
            SaveData data = GetData();
            if (data.collectionCounts != null && data.collectionCounts.Count > 0)
            {
                int total = 0;
                for (int i = 0; i < data.collectionCounts.Count; i++)
                {
                    CodexCountEntry entry = data.collectionCounts[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    total += Mathf.Max(0, entry.count);
                }

                return total;
            }

            EnsureCollectionEntryCountSize(data);
            int legacyTotal = 0;
            for (int i = 0; i < data.collectionEntryCounts.Count; i++)
            {
                legacyTotal += Mathf.Max(0, data.collectionEntryCounts[i]);
            }

            return legacyTotal;
        }

        public static bool IsCodexEntryUnlocked(CodexCategory category, string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return false;
            }

            SaveData data = GetData();
            switch (category)
            {
                case CodexCategory.Creature:
                    return data.unlockedCreatureIds != null && data.unlockedCreatureIds.Contains(entryId);
                case CodexCategory.Obstacle:
                    return data.unlockedObstacleIds != null && data.unlockedObstacleIds.Contains(entryId);
                case CodexCategory.Collection:
                    return GetCollectionCount(data, entryId) > 0;
                default:
                    return false;
            }
        }

        public static bool UnlockCodexEntry(CodexCategory category, string entryId, int addCount = 1)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return false;
            }

            SaveData data = GetData();
            bool newlyUnlocked = false;
            bool changed = false;
            switch (category)
            {
                case CodexCategory.Creature:
                    newlyUnlocked = AddUnique(data.unlockedCreatureIds, entryId);
                    changed = newlyUnlocked;
                    break;
                case CodexCategory.Obstacle:
                    newlyUnlocked = AddUnique(data.unlockedObstacleIds, entryId);
                    changed = newlyUnlocked;
                    break;
                case CodexCategory.Collection:
                    newlyUnlocked = AddOrIncrementCollection(data, entryId, addCount);
                    data.unlockedCollectionCount = CountCollectionUnlocked(data);
                    changed = true;
                    break;
            }

            if (changed)
            {
                SaveDataToDisk(data);
            }
            return newlyUnlocked;
        }

        public static bool IsAbilityUnlocked(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            SaveData data = GetData();
            return data.unlockedAbilityIds != null && data.unlockedAbilityIds.Contains(abilityId);
        }

        public static bool UnlockAbility(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            SaveData data = GetData();
            bool newlyUnlocked = AddUnique(data.unlockedAbilityIds, abilityId);
            if (newlyUnlocked)
            {
                SaveDataToDisk(data);
            }
            return newlyUnlocked;
        }

        public static int GetCodexUnlockedCount(CodexCategory category)
        {
            SaveData data = GetData();
            switch (category)
            {
                case CodexCategory.Creature:
                    return CountUnique(data.unlockedCreatureIds);
                case CodexCategory.Obstacle:
                    return CountUnique(data.unlockedObstacleIds);
                case CodexCategory.Collection:
                    return CountCollectionUnlocked(data);
                default:
                    return 0;
            }
        }

        public static int GetCodexEntryCount(CodexCategory category, string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return 0;
            }

            SaveData data = GetData();
            if (category != CodexCategory.Collection)
            {
                return IsCodexEntryUnlocked(category, entryId) ? 1 : 0;
            }

            return GetCollectionCount(data, entryId);
        }

        public static List<LeaderboardEntry> GetLeaderboardEntries(int maxCount = 10)
        {
            SaveData data = GetData();
            int count = Mathf.Clamp(maxCount, 0, data.leaderboardEntries.Count);
            List<LeaderboardEntry> result = new List<LeaderboardEntry>(count);
            for (int i = 0; i < count; i++)
            {
                LeaderboardEntry source = data.leaderboardEntries[i];
                result.Add(new LeaderboardEntry
                {
                    score = source.score,
                    modeId = source.modeId,
                    utcTime = source.utcTime
                });
            }

            return result;
        }

        public static List<GameModeProgress> GetGameModeProgress()
        {
            SaveData data = GetData();
            int bestScore = Mathf.Max(0, data.bestScore);
            int totalRuns = Mathf.Max(0, data.totalRuns);
            List<GameModeProgress> result = new List<GameModeProgress>(ModeDefinitions.Length);
            for (int i = 0; i < ModeDefinitions.Length; i++)
            {
                ModeDefinition definition = ModeDefinitions[i];
                bool unlocked = IsModeUnlockedInternal(data, definition.modeId);

                float scoreProgress = definition.requiredBestScore <= 0
                    ? 1f
                    : Mathf.Clamp01((float)bestScore / definition.requiredBestScore);
                float runProgress = definition.requiredRuns <= 0
                    ? 1f
                    : Mathf.Clamp01((float)totalRuns / definition.requiredRuns);

                result.Add(new GameModeProgress
                {
                    ModeId = definition.modeId,
                    DisplayName = definition.displayName,
                    Unlocked = unlocked,
                    RequiredBestScore = definition.requiredBestScore,
                    RequiredRuns = definition.requiredRuns,
                    CurrentBestScore = bestScore,
                    CurrentRuns = totalRuns,
                    Progress01 = Mathf.Min(scoreProgress, runProgress)
                });
            }

            return result;
        }

        public static string GetModeDisplayName(string modeId)
        {
            ModeDefinition definition = FindModeDefinition(modeId);
            return definition != null ? definition.displayName : modeId;
        }

        public static string GetSelectedModeId()
        {
            SaveData data = GetData();
            string selected = string.IsNullOrWhiteSpace(data.selectedModeId) ? ModeClassic : data.selectedModeId;
            if (!IsModeUnlockedInternal(data, selected))
            {
                selected = ModeClassic;
                data.selectedModeId = selected;
                SaveDataToDisk(data);
            }

            return selected;
        }

        public static bool TrySetSelectedMode(string modeId, out string reason)
        {
            SaveData data = GetData();
            ModeDefinition definition = FindModeDefinition(modeId);
            if (definition == null)
            {
                reason = "Unknown mode.";
                return false;
            }

            if (!IsModeUnlockedInternal(data, definition.modeId))
            {
                reason = BuildModeRequirementText(definition, data.bestScore, data.totalRuns);
                return false;
            }

            data.selectedModeId = definition.modeId;
            SaveDataToDisk(data);
            reason = string.Empty;
            return true;
        }

        public static void RecordRun(int finalScore)
        {
            RecordRun(finalScore, GetSelectedModeId());
        }

        public static void RecordRun(int finalScore, string modeId)
        {
            SaveData data = GetData();
            int clampedScore = Mathf.Max(0, finalScore);
            string safeModeId = FindModeDefinition(modeId) != null ? modeId : ModeClassic;

            data.lastScore = clampedScore;
            data.totalRuns = Mathf.Max(0, data.totalRuns) + 1;

            long mergedTotal = (long)Mathf.Max(0, data.totalScore) + clampedScore;
            data.totalScore = mergedTotal > int.MaxValue ? int.MaxValue : (int)mergedTotal;

            if (clampedScore > data.bestScore)
            {
                data.bestScore = clampedScore;
            }

            data.leaderboardEntries.Add(new LeaderboardEntry
            {
                score = clampedScore,
                modeId = safeModeId,
                utcTime = DateTime.UtcNow.ToString("o")
            });

            data.leaderboardEntries.Sort((a, b) =>
            {
                int scoreCompare = b.score.CompareTo(a.score);
                if (scoreCompare != 0)
                {
                    return scoreCompare;
                }

                return string.CompareOrdinal(b.utcTime, a.utcTime);
            });

            if (data.leaderboardEntries.Count > MaxLeaderboardEntries)
            {
                data.leaderboardEntries.RemoveRange(MaxLeaderboardEntries, data.leaderboardEntries.Count - MaxLeaderboardEntries);
            }

            UpdateCollectionUnlockByBestScore(data.bestScore);
            UpdateModeUnlockStates(data);
            SaveDataToDisk(data);
        }

        public static void UpdateCollectionUnlockByBestScore(int bestScore)
        {
            // Reserved for legacy compatibility.
            // Collection unlock now comes from picking up fixed lore items at milestone depths.
        }

        private static SaveData GetData()
        {
            if (loaded && cachedData != null)
            {
                return cachedData;
            }

            cachedData = LoadFromDiskOrMigrate();
            EnsureDataIntegrity(cachedData);
            loaded = true;
            return cachedData;
        }

        private static SaveData LoadFromDiskOrMigrate()
        {
            string path = GetSavePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        SaveData data = JsonUtility.FromJson<SaveData>(json);
                        if (data != null)
                        {
                            return data;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"RunProgressStore failed to load save file: {exception.Message}");
                }
            }

            SaveData migrated = new SaveData
            {
                bestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0)),
                lastScore = Mathf.Max(0, PlayerPrefs.GetInt(LastScoreKey, 0)),
                totalRuns = Mathf.Max(0, PlayerPrefs.GetInt(TotalRunsKey, 0)),
                totalScore = Mathf.Max(0, PlayerPrefs.GetInt(TotalScoreKey, 0)),
                unlockedCollectionCount = Mathf.Max(0, PlayerPrefs.GetInt(CollectionUnlockedCountKey, 0)),
                selectedModeId = ModeClassic
            };

            return migrated;
        }

        private static void EnsureDataIntegrity(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.bestScore = Mathf.Max(0, data.bestScore);
            data.lastScore = Mathf.Max(0, data.lastScore);
            data.totalRuns = Mathf.Max(0, data.totalRuns);
            data.totalScore = Mathf.Max(0, data.totalScore);
            if (data.collectionUnlockMask == 0 && data.unlockedCollectionCount > 0)
            {
                int migratedCount = Mathf.Min(data.unlockedCollectionCount, MaxCollectionEntries);
                for (int i = 0; i < migratedCount; i++)
                {
                    data.collectionUnlockMask |= 1 << i;
                }
            }

            EnsureCollectionEntryCountSize(data);
            if (data.unlockedCreatureIds == null)
            {
                data.unlockedCreatureIds = new List<string>();
            }

            if (data.unlockedObstacleIds == null)
            {
                data.unlockedObstacleIds = new List<string>();
            }

            if (data.unlockedAbilityIds == null)
            {
                data.unlockedAbilityIds = new List<string>();
            }

            if (data.collectionCounts == null)
            {
                data.collectionCounts = new List<CodexCountEntry>();
            }

            NormalizeUnlockedList(data.unlockedCreatureIds);
            NormalizeUnlockedList(data.unlockedObstacleIds);
            NormalizeUnlockedList(data.unlockedAbilityIds);
            NormalizeCollectionCounts(data);
            MigrateLegacyCollectionsIfNeeded(data);
            data.unlockedCollectionCount = CountCollectionUnlocked(data);
            data.selectedModeId = string.IsNullOrWhiteSpace(data.selectedModeId) ? ModeClassic : data.selectedModeId;

            if (data.modeStates == null)
            {
                data.modeStates = new List<ModeState>();
            }

            if (data.leaderboardEntries == null)
            {
                data.leaderboardEntries = new List<LeaderboardEntry>();
            }

            UpdateModeUnlockStates(data);
            if (!IsModeUnlockedInternal(data, data.selectedModeId))
            {
                data.selectedModeId = ModeClassic;
            }

            SaveDataToDisk(data);
        }

        private static void UpdateModeUnlockStates(SaveData data)
        {
            for (int i = 0; i < ModeDefinitions.Length; i++)
            {
                ModeDefinition definition = ModeDefinitions[i];
                ModeState state = FindOrCreateModeState(data, definition.modeId);
                bool unlocked = data.bestScore >= definition.requiredBestScore && data.totalRuns >= definition.requiredRuns;
                state.unlocked = unlocked;
            }
        }

        private static bool IsModeUnlockedInternal(SaveData data, string modeId)
        {
            ModeState state = FindOrCreateModeState(data, modeId);
            return state != null && state.unlocked;
        }

        private static ModeState FindOrCreateModeState(SaveData data, string modeId)
        {
            if (data == null || data.modeStates == null)
            {
                return null;
            }

            for (int i = 0; i < data.modeStates.Count; i++)
            {
                ModeState modeState = data.modeStates[i];
                if (modeState != null && string.Equals(modeState.modeId, modeId, StringComparison.Ordinal))
                {
                    return modeState;
                }
            }

            ModeState created = new ModeState
            {
                modeId = modeId,
                unlocked = string.Equals(modeId, ModeClassic, StringComparison.Ordinal)
            };
            data.modeStates.Add(created);
            return created;
        }

        private static ModeDefinition FindModeDefinition(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                return null;
            }

            for (int i = 0; i < ModeDefinitions.Length; i++)
            {
                if (string.Equals(ModeDefinitions[i].modeId, modeId, StringComparison.Ordinal))
                {
                    return ModeDefinitions[i];
                }
            }

            return null;
        }

        private static string BuildModeRequirementText(ModeDefinition definition, int bestScore, int runs)
        {
            if (definition == null)
            {
                return "Unlock requirements not found.";
            }

            return $"{definition.displayName} unlock: Best {bestScore}/{definition.requiredBestScore}, Runs {runs}/{definition.requiredRuns}.";
        }

        private static void SaveDataToDisk(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                string path = GetSavePath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"RunProgressStore failed to save progress: {exception.Message}");
            }
        }

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        private static string ResolveCollectionEntryId(int entryIndex)
        {
            CodexDatabase database = CodexDatabase.Load();
            if (database == null)
            {
                return string.Empty;
            }

            return database.GetEntryId(CodexCategory.Collection, entryIndex);
        }

        private static int CountUnlockedCollections(int mask)
        {
            int count = 0;
            uint bits = (uint)mask;
            while (bits != 0)
            {
                bits &= bits - 1;
                count++;
            }

            return count;
        }

        private static int CountUnique(List<string> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                if (seen.Add(entry))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool AddUnique(List<string> entries, string entryId)
        {
            if (entries == null || string.IsNullOrWhiteSpace(entryId))
            {
                return false;
            }

            if (entries.Contains(entryId))
            {
                return false;
            }

            entries.Add(entryId);
            return true;
        }

        private static int GetCollectionCount(SaveData data, string entryId)
        {
            if (data == null || data.collectionCounts == null || string.IsNullOrWhiteSpace(entryId))
            {
                return 0;
            }

            for (int i = 0; i < data.collectionCounts.Count; i++)
            {
                CodexCountEntry entry = data.collectionCounts[i];
                if (entry != null && string.Equals(entry.id, entryId, StringComparison.Ordinal))
                {
                    return Mathf.Max(0, entry.count);
                }
            }

            return 0;
        }

        private static bool AddOrIncrementCollection(SaveData data, string entryId, int addCount)
        {
            if (data == null || string.IsNullOrWhiteSpace(entryId))
            {
                return false;
            }

            if (data.collectionCounts == null)
            {
                data.collectionCounts = new List<CodexCountEntry>();
            }

            CodexCountEntry found = null;
            for (int i = 0; i < data.collectionCounts.Count; i++)
            {
                CodexCountEntry entry = data.collectionCounts[i];
                if (entry != null && string.Equals(entry.id, entryId, StringComparison.Ordinal))
                {
                    found = entry;
                    break;
                }
            }

            if (found == null)
            {
                found = new CodexCountEntry { id = entryId, count = 0 };
                data.collectionCounts.Add(found);
            }

            bool newlyUnlocked = found.count <= 0;
            int increment = Mathf.Max(1, addCount);
            found.count = Mathf.Max(0, found.count) + increment;
            return newlyUnlocked;
        }

        private static int CountCollectionUnlocked(SaveData data)
        {
            if (data == null || data.collectionCounts == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < data.collectionCounts.Count; i++)
            {
                CodexCountEntry entry = data.collectionCounts[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id) && entry.count > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static void NormalizeUnlockedList(List<string> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            List<string> normalized = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                if (seen.Add(entry))
                {
                    normalized.Add(entry);
                }
            }

            entries.Clear();
            entries.AddRange(normalized);
        }

        private static void NormalizeCollectionCounts(SaveData data)
        {
            if (data == null || data.collectionCounts == null || data.collectionCounts.Count == 0)
            {
                return;
            }

            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < data.collectionCounts.Count; i++)
            {
                CodexCountEntry entry = data.collectionCounts[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                {
                    continue;
                }

                int count = Mathf.Max(0, entry.count);
                if (merged.TryGetValue(entry.id, out int existing))
                {
                    merged[entry.id] = existing + count;
                }
                else
                {
                    merged[entry.id] = count;
                }
            }

            data.collectionCounts.Clear();
            foreach (KeyValuePair<string, int> pair in merged)
            {
                data.collectionCounts.Add(new CodexCountEntry
                {
                    id = pair.Key,
                    count = Mathf.Max(0, pair.Value)
                });
            }
        }

        private static void MigrateLegacyCollectionsIfNeeded(SaveData data)
        {
            if (data == null || data.collectionCounts == null)
            {
                return;
            }

            if (data.collectionCounts.Count > 0)
            {
                return;
            }

            bool hasLegacy = data.collectionUnlockMask != 0;
            for (int i = 0; i < data.collectionEntryCounts.Count; i++)
            {
                if (data.collectionEntryCounts[i] > 0)
                {
                    hasLegacy = true;
                    break;
                }
            }

            if (!hasLegacy)
            {
                return;
            }

            for (int i = 0; i < data.collectionEntryCounts.Count; i++)
            {
                int count = Mathf.Max(0, data.collectionEntryCounts[i]);
                bool unlockedByMask = (data.collectionUnlockMask & (1 << i)) != 0;
                if (count == 0 && !unlockedByMask)
                {
                    continue;
                }

                if (count == 0 && unlockedByMask)
                {
                    count = 1;
                }

                string entryId = ResolveCollectionEntryId(i);
                if (string.IsNullOrWhiteSpace(entryId))
                {
                    entryId = $"legacy_{i}";
                }

                data.collectionCounts.Add(new CodexCountEntry
                {
                    id = entryId,
                    count = count
                });
            }
        }

        private static void EnsureCollectionEntryCountSize(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.collectionEntryCounts == null)
            {
                data.collectionEntryCounts = new List<int>(MaxCollectionEntries);
            }

            if (data.collectionEntryCounts.Count > MaxCollectionEntries)
            {
                data.collectionEntryCounts.RemoveRange(MaxCollectionEntries, data.collectionEntryCounts.Count - MaxCollectionEntries);
            }

            while (data.collectionEntryCounts.Count < MaxCollectionEntries)
            {
                data.collectionEntryCounts.Add(0);
            }

            for (int i = 0; i < MaxCollectionEntries; i++)
            {
                int count = Mathf.Max(0, data.collectionEntryCounts[i]);
                bool unlockedByMask = (data.collectionUnlockMask & (1 << i)) != 0;
                if (count > 0)
                {
                    data.collectionUnlockMask |= 1 << i;
                }
                else if (unlockedByMask)
                {
                    count = 1;
                }

                data.collectionEntryCounts[i] = count;
            }
        }
    }
}
