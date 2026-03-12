using UnityEngine;
using UnityEngine.Serialization;

namespace EndlessRunner
{
    public class ChestMilestoneSpawner : MonoBehaviour
    {
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private ObjectPool pool;
        [FormerlySerializedAs("chestPrefab")]
        [SerializeField] private GameObject legacyChestPrefab;
        [SerializeField] private GameObject[] specialCreaturePrefabs;
        [SerializeField] private GameObject[] padCreaturePrefabs;
        [SerializeField] private GameObject loreCollectiblePrefab;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RunnerController runner;
        [SerializeField, Min(1)] private int scoreInterval = 100;
        [SerializeField, Min(1)] private int padScoreInterval = 50;
        [Header("Lore Collectible Milestones")]
        [SerializeField] private bool enableLoreCollectibleMilestones = true;
        [SerializeField] private int[] loreCollectibleMilestones = { 120, 280, 450, 650, 900, 1200 };
        [SerializeField, Min(0)] private int loreCollectibleScoreValue = 0;
        [SerializeField, Min(0f)] private float spawnOffsetBelowScreen = 0.2f;
        [SerializeField] private bool useLanePositions = true;
        [SerializeField] private bool useDynamicHorizontalBounds = true;
        [SerializeField, Min(0f)] private float spawnHorizontalInset = 0.35f;
        [SerializeField] private float[] lanePositions = { -2f, 0f, 2f };
        [SerializeField] private bool deterministicLaneSequence = true;
        [SerializeField, Range(0.01f, 0.99f)] private float deterministicXCycle = 0.6180339f;
        [SerializeField] private float randomRangeX = 2.5f;
        [SerializeField, Min(1)] private int maxSpawnsPerTick = 3;
        [SerializeField, Min(1)] private int maxPadSpawnsPerTick = 3;

        private int nextScore;
        private int lastScore;
        private int nextPadScore;
        private int lastPadScore;
        private int lastProcessedFrame = -1;
        private int spawnedCreatureCount;
        private int spawnedPadCount;
        private int nextLoreMilestoneIndex;
        private int spawnedLoreCount;

        private void OnEnable()
        {
            ResolveReferences();
            ResetMilestones();

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += OnScoreChanged;
            }
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -= OnScoreChanged;
            }
        }

        private void Update()
        {
            if (scoreManager == null)
            {
                ResolveReferences();
            }

            if (scoreManager == null)
            {
                return;
            }

            ProcessScore(scoreManager.Score);
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
            lastPadScore = current;
            spawnedCreatureCount = 0;
            spawnedPadCount = 0;
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

            if (padScoreInterval <= 0)
            {
                nextPadScore = 0;
            }
            else
            {
                int padBucket = current / padScoreInterval;
                nextPadScore = (padBucket + 1) * padScoreInterval;
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

            if (scoreInterval <= 0)
            {
                return;
            }

            if (score < lastScore)
            {
                ResetMilestones();
                lastScore = score;
            }

            int spawnCount = 0;
            while (score >= nextScore && spawnCount < maxSpawnsPerTick)
            {
                SpawnSpecialCreature();
                nextScore += scoreInterval;
                spawnCount++;
            }

            if (score < lastPadScore)
            {
                ResetMilestones();
                lastPadScore = score;
            }

            int padSpawnCount = 0;
            while (padScoreInterval > 0 && score >= nextPadScore && padSpawnCount < maxPadSpawnsPerTick)
            {
                SpawnPadCreature();
                nextPadScore += padScoreInterval;
                padSpawnCount++;
            }

            ProcessLoreMilestones(score);
            lastScore = score;
            lastPadScore = score;
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

            ResolveReferences();

            int spawnIndex = spawnedCreatureCount;
            float x = GetSpawnX(spawnIndex);
            if (!TryGetBottomSpawnY(out float bottomY))
            {
                return;
            }

            Vector3 world = new Vector3(x, bottomY - spawnOffsetBelowScreen, 0f);
            world.x = x;

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

        private void SpawnPadCreature()
        {
            GameObject prefab = GetPadCreaturePrefab();
            if (prefab == null)
            {
                return;
            }

            ResolveReferences();

            int spawnIndex = spawnedPadCount;
            float x = GetSpawnX(spawnIndex);
            if (!TryGetBottomSpawnY(out float bottomY))
            {
                return;
            }

            Vector3 world = new Vector3(x, bottomY - spawnOffsetBelowScreen, 0f);
            world.x = x;

            if (pool != null)
            {
                pool.Get(prefab, world, Quaternion.identity);
            }
            else
            {
                Instantiate(prefab, world, Quaternion.identity);
            }

            spawnedPadCount++;
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
            ResolveReferences();
            if (!TryGetBottomSpawnY(out float bottomY))
            {
                return;
            }

            int spawnIndex = spawnedCreatureCount + spawnedLoreCount;
            float x = GetSpawnX(spawnIndex);
            Vector3 world = new Vector3(x, bottomY - spawnOffsetBelowScreen, 0f);

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
                    collectible.ConfigureLoreRelic(entryIndex, loreCollectibleScoreValue);
                }
            }

            spawnedLoreCount++;
        }

        private float GetSpawnX(int spawnIndex)
        {
            if (useDynamicHorizontalBounds && runner != null)
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

                if (useLanePositions)
                {
                    int laneCount = lanePositions != null && lanePositions.Length > 0 ? lanePositions.Length : 3;
                    laneCount = Mathf.Max(1, laneCount);
                    int laneIndex = deterministicLaneSequence
                        ? PositiveModulo(spawnIndex + GetScoreBucket(), laneCount)
                        : 0;
                    float t = laneCount == 1 ? 0.5f : (float)laneIndex / (laneCount - 1);
                    return Mathf.Lerp(left, right, t);
                }

                float tLinear = deterministicLaneSequence
                    ? Mathf.Repeat((spawnIndex + 1) * deterministicXCycle, 1f)
                    : 0.5f;
                return Mathf.Lerp(left, right, tLinear);
            }

            if (useLanePositions && lanePositions != null && lanePositions.Length > 0)
            {
                int laneIndex = deterministicLaneSequence
                    ? PositiveModulo(spawnIndex + GetScoreBucket(), lanePositions.Length)
                    : 0;
                return lanePositions[laneIndex];
            }

            float normalized = deterministicLaneSequence
                ? Mathf.Repeat((spawnIndex + 1) * deterministicXCycle, 1f)
                : 0.5f;
            return Mathf.Lerp(-randomRangeX, randomRangeX, normalized);
        }

        private int GetScoreBucket()
        {
            if (scoreInterval <= 0)
            {
                return 0;
            }

            int score = scoreManager != null ? scoreManager.Score : 0;
            return score / scoreInterval;
        }

        private GameObject GetSpecialCreaturePrefab()
        {
            if (specialCreaturePrefabs != null && specialCreaturePrefabs.Length > 0)
            {
                int index = Random.Range(0, specialCreaturePrefabs.Length);
                return specialCreaturePrefabs[index];
            }

            return legacyChestPrefab;
        }

        private GameObject GetPadCreaturePrefab()
        {
            if (padCreaturePrefabs != null && padCreaturePrefabs.Length > 0)
            {
                int index = Random.Range(0, padCreaturePrefabs.Length);
                return padCreaturePrefabs[index];
            }

            return null;
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

        private static int PositiveModulo(int value, int mod)
        {
            if (mod <= 0)
            {
                return 0;
            }

            int result = value % mod;
            return result < 0 ? result + mod : result;
        }
    }
}
