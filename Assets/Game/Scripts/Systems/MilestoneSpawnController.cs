using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class MilestoneSpawnController : MonoBehaviour
    {
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private ObjectPool pool;
        [SerializeField] private GameObject[] specialCreaturePrefabs;
        [SerializeField] private GameObject loreCollectiblePrefab;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RunnerController runner;
        [SerializeField] private CodexDatabase codexDatabase;
        [SerializeField, Min(1)] private int scoreInterval = 100;
        [SerializeField] private bool enableCreatureMilestones = false;
        [Header("Lore Collectible Milestones")]
        [SerializeField] private bool enableLoreCollectibleMilestones = true;
        [SerializeField] private int[] loreCollectibleMilestones = { 120, 280, 450, 650, 900, 1200 };
        [SerializeField, Min(0)] private int loreCollectibleScoreValue = 0;
        [SerializeField, Min(0f)] private float spawnOffsetBelowScreen = 0.2f;
        [SerializeField, Min(0f)] private float spawnHorizontalInset = 0.35f;
        [SerializeField] private float randomRangeX = 2.5f;
        [SerializeField, Min(1)] private int maxSpawnsPerTick = 3;

        private int nextScore;
        private int lastScore;
        private int lastProcessedFrame = -1;
        private int spawnedCreatureCount;
        private int nextLoreMilestoneIndex;
        private int spawnedLoreCount;

        private void OnEnable()
        {
            ResolveReferences();
            ResetMilestones();

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += OnScoreChanged;
                ProcessScore(scoreManager.Score);
            }
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -= OnScoreChanged;
            }
        }

        private void ResolveReferences()
        {
            if (scoreManager == null)
            {
                scoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (pool == null)
            {
                pool = ObjectPool.Instance != null ? ObjectPool.Instance : FindAnyObjectByType<ObjectPool>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }
        }

        private void ResetMilestones()
        {
            int current = scoreManager != null ? scoreManager.Score : 0;
            lastScore = current;
            spawnedCreatureCount = 0;
            spawnedLoreCount = 0;
            if (scoreInterval <= 0)
            {
                nextScore = 0;
            }
            else
            {
                int bucket = current / scoreInterval;
                nextScore = (bucket + 1) * scoreInterval;
            }

            nextLoreMilestoneIndex = 0;
            if (loreCollectibleMilestones == null || loreCollectibleMilestones.Length == 0)
            {
                return;
            }

            while (nextLoreMilestoneIndex < loreCollectibleMilestones.Length &&
                   loreCollectibleMilestones[nextLoreMilestoneIndex] <= current)
            {
                nextLoreMilestoneIndex++;
            }
        }

        private void OnScoreChanged(int score)
        {
            ProcessScore(score);
        }

        private void ProcessScore(int score)
        {
            if (Time.frameCount == lastProcessedFrame)
            {
                return;
            }

            lastProcessedFrame = Time.frameCount;

            if (pool == null || targetCamera == null || runner == null)
            {
                ResolveReferences();
            }

            if (score < lastScore)
            {
                ResetMilestones();
                lastScore = score;
            }

            bool canSpawnCreatureMilestones = enableCreatureMilestones && scoreInterval > 0;
            int spawnCount = 0;
            while (canSpawnCreatureMilestones && score >= nextScore && spawnCount < maxSpawnsPerTick)
            {
                SpawnSpecialCreature();
                nextScore += scoreInterval;
                spawnCount++;
            }

            ProcessLoreMilestones(score);
            lastScore = score;
        }

        /// <summary>
        /// Runtime hook used by character switch workflow.
        /// </summary>
        public void SetRunner(RunnerController newRunner)
        {
            runner = newRunner;
        }

        private void SpawnSpecialCreature()
        {
            GameObject prefab = GetSpecialCreaturePrefab();
            if (prefab == null)
            {
                return;
            }

            int spawnIndex = spawnedCreatureCount;
            if (!TryBuildSpawnPosition(spawnIndex, out Vector3 world))
            {
                return;
            }

            if (pool != null)
            {
                pool.Get(prefab, world, Quaternion.identity);
            }
            else
            {
                Instantiate(prefab, world, Quaternion.identity);
            }

            spawnedCreatureCount++;
        }


        private void ProcessLoreMilestones(int score)
        {
            if (!enableLoreCollectibleMilestones || loreCollectiblePrefab == null || loreCollectibleMilestones == null || loreCollectibleMilestones.Length == 0)
            {
                return;
            }

            while (nextLoreMilestoneIndex < loreCollectibleMilestones.Length &&
                   score >= loreCollectibleMilestones[nextLoreMilestoneIndex])
            {
                SpawnLoreCollectible(nextLoreMilestoneIndex);
                nextLoreMilestoneIndex++;
            }
        }

        private void SpawnLoreCollectible(int entryIndex)
        {
            int spawnIndex = spawnedCreatureCount + spawnedLoreCount;
            if (!TryBuildSpawnPosition(spawnIndex, out Vector3 world))
            {
                return;
            }

            GameObject instance;
            if (pool != null)
            {
                instance = pool.Get(loreCollectiblePrefab, world, Quaternion.identity);
            }
            else
            {
                instance = Instantiate(loreCollectiblePrefab, world, Quaternion.identity);
            }

            if (instance != null)
            {
                Collectible collectible = instance.GetComponent<Collectible>();
                if (collectible != null)
                {
                    string entryId = ResolveLoreEntryId(entryIndex);
                    collectible.ConfigureLoreRelic(entryId, loreCollectibleScoreValue);
                }
            }

            spawnedLoreCount++;
        }

        private bool TryBuildSpawnPosition(int spawnIndex, out Vector3 world)
        {
            world = Vector3.zero;
            if (!TryGetBottomSpawnY(out float bottomY))
            {
                return false;
            }

            float x = GetSpawnX(spawnIndex);
            world = new Vector3(x, bottomY - spawnOffsetBelowScreen, 0f);
            return true;
        }

        private float GetSpawnX(int spawnIndex)
        {
            if (runner != null)
            {
                runner.GetHorizontalBounds(out float minX, out float maxX);
                float left = minX + spawnHorizontalInset;
                float right = maxX - spawnHorizontalInset;
                if (left > right)
                {
                    float center = (minX + maxX) * 0.5f;
                    left = center;
                    right = center;
                }

                return UnityEngine.Random.Range(left, right);
            }

            return UnityEngine.Random.Range(-randomRangeX, randomRangeX);
        }

        private GameObject GetSpecialCreaturePrefab()
        {
            if (specialCreaturePrefabs != null && specialCreaturePrefabs.Length > 0)
            {
                int score = scoreManager != null ? scoreManager.Score : 0;
                List<GameObject> candidates = new List<GameObject>(specialCreaturePrefabs.Length);
                foreach (GameObject prefab in specialCreaturePrefabs)
                {
                    if (prefab == null)
                    {
                        continue;
                    }

                    if (IsPrefabAvailable(prefab, score) && ProvidesMilestoneReward(prefab))
                    {
                        candidates.Add(prefab);
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Random.Range(0, candidates.Count);
                    return candidates[index];
                }
            }

            return null;
        }

        private bool IsPrefabAvailable(GameObject prefab, int score)
        {
            SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
            if (creature == null)
            {
                return true;
            }

            string entryId = creature.CodexEntryId;
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return true;
            }

            if (codexDatabase == null)
            {
                codexDatabase = CodexDatabase.Load();
            }

            CodexEntry entry = codexDatabase != null ? codexDatabase.FindEntry(CodexCategory.Creature, entryId) : null;
            if (entry == null)
            {
                return true;
            }

            return score >= Mathf.Max(0, entry.spawnScore);
        }

        private static bool ProvidesMilestoneReward(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
            if (creature == null)
            {
                return true;
            }

            return creature.ProvidesGameplayReward();
        }

        private string ResolveLoreEntryId(int entryIndex)
        {
            if (codexDatabase == null)
            {
                codexDatabase = CodexDatabase.Load();
            }

            if (codexDatabase != null)
            {
                string id = codexDatabase.GetEntryId(CodexCategory.Collection, entryIndex);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return id;
                }
            }

            return $"relic_{Mathf.Max(0, entryIndex)}";
        }

        private bool TryGetBottomSpawnY(out float bottomY)
        {
            bottomY = 0f;
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return false;
            }

            if (cam.orthographic)
            {
                bottomY = cam.transform.position.y - cam.orthographicSize;
                return true;
            }

            float distance = Mathf.Abs(cam.transform.position.z);
            Vector3 viewport = new Vector3(0.5f, 0f, distance);
            bottomY = cam.ViewportToWorldPoint(viewport).y;
            return true;
        }

    }
}
