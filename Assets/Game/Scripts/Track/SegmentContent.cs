using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EndlessRunner
{
    public class SegmentContent : MonoBehaviour, IPoolable
    {
        private struct SpawnPlan
        {
            public GameObject[] rewardPrefabs;
            public GameObject[] hazardPrefabs;
            public int rewardCount;
            public int hazardCount;
            public int collectibleCount;

            public int TotalCount => collectibleCount + rewardCount + hazardCount;
        }

        [SerializeField] private GameObject[] creaturePrefabs;
        [SerializeField] private GameObject[] collectiblePrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private RunnerController runner;
        [SerializeField] private CodexDatabase codexDatabase;
        [SerializeField] private bool autoFitSpawnPointsToBounds = true;
        [SerializeField, Min(0f)] private float spawnInset = 0.4f;
        [Header("Camera Spawn Filter")]
        [SerializeField] private bool skipSpawnInsideCamera = true;
        [SerializeField, Min(0f)] private float cameraSpawnPadding = 0.5f;
        [Header("Progress Formula (Deterministic Count)")]
        [SerializeField, Min(1f)] private float progressScoreScale = 180f;
        [SerializeField, Min(0f)] private float creatureBaseCount = 2f;
        [SerializeField, Min(0f)] private float creatureGrowth = 1.1f;
        [Header("Hazard / Reward Ratio")]
        [SerializeField, Range(0f, 1f)] private float hazardRatioStart = 0.167f;
        [SerializeField, Range(0f, 1f)] private float hazardRatioEnd = 0.667f;
        [SerializeField, Min(0)] private int hazardRatioScoreMax = 3000;
        [SerializeField, Min(0f)] private float collectibleBaseCount = 0f;
        [SerializeField, Min(0f)] private float collectibleGrowth = 0f;
        [SerializeField, Min(0f)] private float collectibleYOffset = 0.45f;
        [SerializeField] private bool autoCreateRuntimeCollectiblePrefab = false;

        private readonly List<GameObject> spawned = new();
        private ObjectPool pool;
        private Camera gameplayCamera;
        private static GameObject runtimeCollectiblePrefab;
        private static Sprite runtimeCollectibleSprite;
        private static int deterministicSpawnSerial;

        private void Awake()
        {
            pool = ObjectPool.Instance;
            ResolveRunner();
            ResolveCodex();
            ResolveCamera();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            creaturePrefabs = SanitizeCreaturePrefabs(creaturePrefabs);
            collectiblePrefabs = FilterPrefabs(collectiblePrefabs);
        }
#endif

        public void OnSpawned()
        {
            if (pool == null)
            {
                pool = ObjectPool.Instance;
            }

            ResolveRunner();
            ResolveCodex();
            ResolveCamera();
            ApplySpawnPointBounds();
            SpawnContent();
        }

        public void OnDespawned()
        {
            DespawnContent();
        }

        private void SpawnContent()
        {
            if (pool == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            List<Transform> points = GetValidSpawnPoints();
            if (points.Count == 0)
            {
                return;
            }

            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            float progress = GetProgressFactor(score);
            int pointCount = points.Count;
            SpawnPlan plan = BuildSpawnPlan(score, progress, pointCount);
            if (plan.TotalCount <= 0)
            {
                deterministicSpawnSerial++;
                return;
            }

            int seed = deterministicSpawnSerial * 97 + Mathf.FloorToInt(progress * 100f);
            List<int> pointOrder = BuildDeterministicPointOrder(pointCount, seed);
            HashSet<int> occupiedPoints = new();
            Predicate<Transform> visibilityFilter = BuildVisibilityFilter();

            SpawnCollectibleCategory(plan.collectibleCount, points, pointOrder, occupiedPoints, seed + 23);
            SpawnCategory(plan.rewardPrefabs, plan.rewardCount, points, pointOrder, occupiedPoints, visibilityFilter, 0f, seed + 37);
            SpawnCategory(plan.hazardPrefabs, plan.hazardCount, points, pointOrder, occupiedPoints, visibilityFilter, 0f, seed + 53);

            deterministicSpawnSerial++;
        }

        private void DespawnContent()
        {
            if (pool == null)
            {
                spawned.Clear();
                return;
            }

            foreach (GameObject instance in spawned)
            {
                pool.Release(instance);
            }

            spawned.Clear();
        }

        private void ResolveRunner()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }
        }

        private void ResolveCodex()
        {
            if (codexDatabase == null)
            {
                codexDatabase = CodexDatabase.Load();
            }
        }

        private void ResolveCamera()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = FindAnyObjectByType<Camera>();
            }
        }

        private Predicate<Transform> BuildVisibilityFilter()
        {
            if (!skipSpawnInsideCamera)
            {
                return null;
            }

            ResolveCamera();
            if (gameplayCamera == null)
            {
                return null;
            }

            float cameraBottomY;
            if (gameplayCamera.orthographic)
            {
                cameraBottomY = gameplayCamera.transform.position.y - gameplayCamera.orthographicSize;
            }
            else
            {
                float depth = Mathf.Abs(transform.position.z - gameplayCamera.transform.position.z);
                Vector3 bottom = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, Mathf.Max(0.01f, depth)));
                cameraBottomY = bottom.y;
            }

            float threshold = cameraBottomY - Mathf.Max(0f, cameraSpawnPadding);
            return point => point != null && point.position.y < threshold;
        }

        private List<Transform> GetValidSpawnPoints()
        {
            List<Transform> points = new(spawnPoints.Length);
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    points.Add(point);
                }
            }

            return points;
        }

        private float GetProgressFactor(int score)
        {
            float safeScore = Mathf.Max(0f, score);
            float scale = Mathf.Max(1f, progressScoreScale);
            return Mathf.Log(1f + safeScore / scale);
        }

        private static int ComputeProgressCount(float baseCount, float growth, float progress, int maxCount)
        {
            if (maxCount <= 0)
            {
                return 0;
            }

            int count = Mathf.FloorToInt(Mathf.Max(0f, baseCount) + Mathf.Max(0f, growth) * Mathf.Max(0f, progress));
            return Mathf.Clamp(count, 0, maxCount);
        }

        private int ComputeCollectibleCount(float progress, int maxCount)
        {
            if (maxCount <= 0)
            {
                return 0;
            }

            // sqrt term keeps collectible growth smooth while still increasing with progress.
            float value = Mathf.Max(0f, collectibleBaseCount) + Mathf.Max(0f, collectibleGrowth) * Mathf.Sqrt(1f + Mathf.Max(0f, progress));
            return Mathf.Clamp(Mathf.FloorToInt(value), 0, maxCount);
        }

        private SpawnPlan BuildSpawnPlan(int score, float progress, int pointCount)
        {
            GameObject[] eligible = FilterPrefabsByScore(creaturePrefabs, score, CodexCategory.Creature);
            PartitionCreatures(eligible, out GameObject[] rewardPool, out GameObject[] hazardPool);

            SpawnPlan plan = new SpawnPlan
            {
                rewardPrefabs = rewardPool,
                hazardPrefabs = hazardPool,
                collectibleCount = 0,
                rewardCount = 0,
                hazardCount = 0
            };

            if (pointCount <= 0)
            {
                return plan;
            }

            bool canSpawnCollectibles = (collectiblePrefabs != null && collectiblePrefabs.Length > 0) || autoCreateRuntimeCollectiblePrefab;

            plan.collectibleCount = canSpawnCollectibles
                ? ComputeCollectibleCount(progress, pointCount)
                : 0;

            int remainingSlots = Mathf.Max(0, pointCount - plan.collectibleCount);
            int totalCreatures = ComputeProgressCount(creatureBaseCount, creatureGrowth, progress, remainingSlots);

            if (totalCreatures > 0)
            {
                float t = hazardRatioScoreMax > 0 ? Mathf.Clamp01((float)score / hazardRatioScoreMax) : 1f;
                float hazardFraction = Mathf.Lerp(hazardRatioStart, hazardRatioEnd, t);

                int hazardCount = hazardPool.Length > 0 ? Mathf.Max(1, Mathf.RoundToInt(totalCreatures * hazardFraction)) : 0;
                hazardCount = Mathf.Clamp(hazardCount, 0, totalCreatures);
                int rewardCount = rewardPool.Length > 0 ? totalCreatures - hazardCount : 0;
                hazardCount = totalCreatures - rewardCount;

                if (rewardPool.Length == 0)
                {
                    hazardCount = totalCreatures;
                    rewardCount = 0;
                }

                plan.hazardCount = hazardCount;
                plan.rewardCount = rewardCount;
            }

            return plan;
        }

        private static void PartitionCreatures(GameObject[] prefabs, out GameObject[] reward, out GameObject[] hazard)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                reward = Array.Empty<GameObject>();
                hazard = Array.Empty<GameObject>();
                return;
            }

            List<GameObject> rewardList = new();
            List<GameObject> hazardList = new();

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) continue;

                SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
                if (creature == null) continue;

                if (creature.IsHazard())
                {
                    hazardList.Add(prefab);
                }
                else
                {
                    rewardList.Add(prefab);
                }
            }

            reward = rewardList.ToArray();
            hazard = hazardList.ToArray();
        }

        private void SpawnCategory(
            GameObject[] prefabs,
            int count,
            List<Transform> points,
            List<int> pointOrder,
            HashSet<int> occupiedPoints,
            Predicate<Transform> pointFilter,
            float yOffset,
            int seed)
        {
            if (count <= 0 || prefabs == null || prefabs.Length == 0 || points.Count == 0 || pointOrder.Count == 0)
            {
                return;
            }

            int spawnedCount = 0;
            for (int i = 0; i < pointOrder.Count && spawnedCount < count; i++)
            {
                int pointIndex = pointOrder[i];
                if (occupiedPoints != null && occupiedPoints.Contains(pointIndex))
                {
                    continue;
                }

                Transform point = points[pointIndex];
                if (point == null)
                {
                    continue;
                }

                if (pointFilter != null && !pointFilter(point))
                {
                    continue;
                }

                GameObject prefab = GetDeterministicPrefab(prefabs, seed + spawnedCount);
                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = point.position + Vector3.up * yOffset;
                GameObject instance = pool.Get(prefab, position, point.rotation);
                if (instance != null)
                {
                    spawned.Add(instance);
                    occupiedPoints?.Add(pointIndex);
                    spawnedCount++;
                }
            }
        }

        private void SpawnCollectibleCategory(
            int count,
            List<Transform> points,
            List<int> pointOrder,
            HashSet<int> occupiedPoints,
            int seed)
        {
            if (count <= 0 || points.Count == 0 || pointOrder.Count == 0)
            {
                return;
            }

            int spawnedCount = 0;
            for (int i = 0; i < pointOrder.Count && spawnedCount < count; i++)
            {
                int pointIndex = pointOrder[i];
                if (occupiedPoints != null && occupiedPoints.Contains(pointIndex))
                {
                    continue;
                }

                Transform point = points[pointIndex];
                if (point == null)
                {
                    continue;
                }

                GameObject prefab = PickCollectiblePrefab(seed + spawnedCount);
                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = point.position + Vector3.up * collectibleYOffset;
                GameObject instance = pool.Get(prefab, position, Quaternion.identity);
                if (instance != null)
                {
                    spawned.Add(instance);
                    occupiedPoints?.Add(pointIndex);
                    spawnedCount++;
                }
            }
        }

        private static List<int> BuildDeterministicPointOrder(int count, int seed)
        {
            List<int> order = new(count);
            if (count <= 0)
            {
                return order;
            }

            bool[] used = new bool[count];
            int index = PositiveModulo(seed, count);
            int step = count > 1 ? count - 1 : 1;

            for (int i = 0; i < count; i++)
            {
                int guard = 0;
                while (used[index] && guard < count)
                {
                    index = (index + 1) % count;
                    guard++;
                }

                order.Add(index);
                used[index] = true;
                index = (index + step) % count;
            }

            return order;
        }

        private static GameObject GetDeterministicPrefab(GameObject[] prefabs, int seed)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            return prefabs[PositiveModulo(seed, prefabs.Length)];
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

        private GameObject PickCollectiblePrefab(int seed)
        {
            if (collectiblePrefabs != null && collectiblePrefabs.Length > 0)
            {
                return collectiblePrefabs[PositiveModulo(seed, collectiblePrefabs.Length)];
            }

            if (!autoCreateRuntimeCollectiblePrefab)
            {
                return null;
            }

            if (runtimeCollectiblePrefab == null)
            {
                runtimeCollectiblePrefab = CreateRuntimeCollectiblePrefab();
            }

            return runtimeCollectiblePrefab;
        }

        private GameObject[] FilterPrefabsByScore(GameObject[] prefabs, int score, CodexCategory category)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return prefabs;
            }

            if (codexDatabase == null)
            {
                codexDatabase = CodexDatabase.Load();
            }

            List<GameObject> filtered = new(prefabs.Length);
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (!TryResolveSpawnEntry(prefab, category, out CodexCategory entryCategory, out string entryId))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entryId))
                {
                    filtered.Add(prefab);
                    continue;
                }

                CodexEntry entry = codexDatabase != null ? codexDatabase.FindEntry(entryCategory, entryId) : null;
                int minScore = entry != null ? Mathf.Max(0, entry.spawnScore) : 0;
                if (score >= minScore)
                {
                    filtered.Add(prefab);
                }
            }

            return filtered.ToArray();
        }

        private static bool TryResolveSpawnEntry(GameObject prefab, CodexCategory requestedCategory, out CodexCategory entryCategory, out string entryId)
        {
            entryCategory = requestedCategory;
            entryId = string.Empty;
            if (prefab == null)
            {
                return false;
            }

            switch (requestedCategory)
            {
                case CodexCategory.Creature:
                    SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
                    if (creature == null)
                    {
                        return false;
                    }

                    entryCategory = CodexCategory.Creature;
                    entryId = creature.CodexEntryId;
                    return true;

                default:
                    return false;
            }
        }

        private static GameObject CreateRuntimeCollectiblePrefab()
        {
            GameObject prefab = new("RuntimeCollectiblePrefab");
            prefab.SetActive(false);
            prefab.hideFlags = HideFlags.HideAndDontSave;

            SpriteRenderer spriteRenderer = prefab.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetRuntimeCollectibleSprite();
            spriteRenderer.color = new Color(1f, 0.83f, 0.2f, 1f);
            spriteRenderer.sortingOrder = 6;

            CircleCollider2D collider = prefab.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.25f;

            prefab.AddComponent<Collectible>();
            prefab.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            return prefab;
        }

        private static Sprite GetRuntimeCollectibleSprite()
        {
            if (runtimeCollectibleSprite != null)
            {
                return runtimeCollectibleSprite;
            }

            runtimeCollectibleSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            runtimeCollectibleSprite.name = "RuntimeCollectibleSprite";
            return runtimeCollectibleSprite;
        }

        private void ApplySpawnPointBounds()
        {
            if (!autoFitSpawnPointsToBounds || spawnPoints == null || spawnPoints.Length == 0 || runner == null)
            {
                return;
            }

            runner.GetHorizontalBounds(out float minX, out float maxX);
            float left = minX + spawnInset;
            float right = maxX - spawnInset;
            if (left > right)
            {
                float center = (minX + maxX) * 0.5f;
                left = center;
                right = center;
            }

            List<Transform> ordered = new(spawnPoints.Length);
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    ordered.Add(point);
                }
            }

            if (ordered.Count == 0)
            {
                return;
            }

            foreach (Transform point in ordered)
            {
                float x = UnityEngine.Random.Range(left, right);
                Vector3 position = point.position;
                position.x = x;
                point.position = position;
            }
        }

#if UNITY_EDITOR
        private static GameObject[] SanitizeCreaturePrefabs(GameObject[] prefabs)
        {
            return SanitizePrefabs(
                prefabs,
                "Creature",
                prefab => prefab.GetComponent<SpecialCreature>() != null);
        }

        private static GameObject[] SanitizePrefabs(GameObject[] prefabs, string fallbackName, Predicate<GameObject> isValidPrefab)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return TryLoadFallback(fallbackName);
            }

            List<GameObject> filtered = new(prefabs.Length);
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (prefab.GetType() != typeof(GameObject))
                {
                    Debug.LogWarning("SegmentContent prefab reference is not a GameObject, removing it.", prefab);
                    continue;
                }

                if (isValidPrefab != null && !isValidPrefab(prefab))
                {
                    continue;
                }

                filtered.Add(prefab);
            }

            if (filtered.Count == 0)
            {
                return TryLoadFallback(fallbackName);
            }

            return filtered.ToArray();
        }

        private static GameObject[] TryLoadFallback(string name)
        {
            string path = GetFallbackPath(name);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return new[] { prefab };
            }

            return Array.Empty<GameObject>();
        }

        private static string GetFallbackPath(string name)
        {
            if (string.Equals(name, "Creature", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets/Game/Prefabs/Creatures/SpecialCreature_Normal.prefab";
            }

            return $"Assets/Game/Prefabs/{name}.prefab";
        }

        private static GameObject[] FilterPrefabs(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return Array.Empty<GameObject>();
            }

            List<GameObject> filtered = new(prefabs.Length);
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (prefab.GetType() != typeof(GameObject))
                {
                    continue;
                }

                filtered.Add(prefab);
            }

            return filtered.ToArray();
        }
#endif
    }
}
