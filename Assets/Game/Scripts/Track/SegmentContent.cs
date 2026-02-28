using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

namespace EndlessRunner
{
    public class SegmentContent : MonoBehaviour, IPoolable
    {
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private GameObject[] chestPrefabs;
        [SerializeField] private GameObject[] collectiblePrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private RunnerController runner;
        [SerializeField] private bool autoFitSpawnPointsToBounds = true;
        [SerializeField, Min(0f)] private float spawnInset = 0.4f;
        [Header("Progress Formula (Deterministic Count)")]
        [SerializeField, Min(1f)] private float progressScoreScale = 180f;
        [SerializeField, Min(0f)] private float hostileBaseCount = 1f;
        [SerializeField, Min(0f)] private float hostileGrowth = 1.1f;
        [SerializeField, Range(0f, 1f)] private float enemyRatioBase = 0.25f;
        [SerializeField, Range(0f, 1f)] private float enemyRatioGrowth = 0.4f;
        [SerializeField, Min(0f)] private float chestBaseCount = 0f;
        [SerializeField, Min(0f)] private float chestGrowth = 0f;
        [SerializeField, Min(0f)] private float collectibleBaseCount = 0f;
        [SerializeField, Min(0f)] private float collectibleGrowth = 0f;
        [SerializeField, Min(0f)] private float collectibleYOffset = 0.45f;
        [SerializeField] private bool autoCreateRuntimeCollectiblePrefab = false;

        private readonly List<GameObject> spawned = new();
        private ObjectPool pool;
        private static GameObject runtimeCollectiblePrefab;
        private static Sprite runtimeCollectibleSprite;
        private static int deterministicSpawnSerial;

        private void Awake()
        {
            pool = ObjectPool.Instance;
            ResolveRunner();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            enemyPrefabs = SanitizePrefabs(enemyPrefabs, "Enemy");
            obstaclePrefabs = SanitizePrefabs(obstaclePrefabs, "Obstacle");
            chestPrefabs = FilterPrefabs(chestPrefabs);
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
            ApplySpawnPointBounds();
            SpawnObstacles();
        }

        public void OnDespawned()
        {
            DespawnObstacles();
        }

        private void SpawnObstacles()
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
            bool canSpawnChests = chestPrefabs != null && chestPrefabs.Length > 0;
            bool canSpawnCollectibles = (collectiblePrefabs != null && collectiblePrefabs.Length > 0) || autoCreateRuntimeCollectiblePrefab;
            bool canSpawnEnemies = enemyPrefabs != null && enemyPrefabs.Length > 0;
            bool canSpawnObstacles = obstaclePrefabs != null && obstaclePrefabs.Length > 0;

            int chestCount = canSpawnChests
                ? ComputeProgressCount(chestBaseCount, chestGrowth, progress, pointCount)
                : 0;

            int collectibleCount = canSpawnCollectibles
                ? ComputeCollectibleCount(progress, pointCount - chestCount)
                : 0;

            int remainingForHostiles = Mathf.Max(0, pointCount - chestCount - collectibleCount);
            int hostileCount = ComputeProgressCount(hostileBaseCount, hostileGrowth, progress, remainingForHostiles);

            int enemyCount = 0;
            int obstacleCount = 0;
            if (canSpawnEnemies || canSpawnObstacles)
            {
                float enemyRatio = Mathf.Clamp01(enemyRatioBase + enemyRatioGrowth * (1f - Mathf.Exp(-progress)));
                enemyCount = canSpawnEnemies ? Mathf.Clamp(Mathf.RoundToInt(hostileCount * enemyRatio), 0, hostileCount) : 0;
                obstacleCount = canSpawnObstacles ? hostileCount - enemyCount : 0;
                if (!canSpawnObstacles)
                {
                    enemyCount = hostileCount;
                }
            }

            int seed = deterministicSpawnSerial * 97 + Mathf.FloorToInt(progress * 100f);
            List<int> pointOrder = BuildDeterministicPointOrder(pointCount, seed);
            int cursor = 0;

            SpawnCategory(chestPrefabs, chestCount, points, pointOrder, ref cursor, 0f, seed + 11);
            SpawnCollectibleCategory(collectibleCount, points, pointOrder, ref cursor, seed + 23);
            SpawnCategory(enemyPrefabs, enemyCount, points, pointOrder, ref cursor, 0f, seed + 37);
            SpawnCategory(obstaclePrefabs, obstacleCount, points, pointOrder, ref cursor, 0f, seed + 53);

            deterministicSpawnSerial++;
        }

        private void DespawnObstacles()
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

        private void SpawnCategory(
            GameObject[] prefabs,
            int count,
            List<Transform> points,
            List<int> pointOrder,
            ref int cursor,
            float yOffset,
            int seed)
        {
            if (count <= 0 || prefabs == null || prefabs.Length == 0 || points.Count == 0 || pointOrder.Count == 0)
            {
                return;
            }

            int actualCount = Mathf.Min(count, pointOrder.Count - cursor);
            for (int i = 0; i < actualCount; i++)
            {
                int pointIndex = pointOrder[cursor++];
                Transform point = points[pointIndex];
                if (point == null)
                {
                    continue;
                }

                GameObject prefab = GetDeterministicPrefab(prefabs, seed + i);
                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = point.position + Vector3.up * yOffset;
                GameObject instance = pool.Get(prefab, position, point.rotation);
                if (instance != null)
                {
                    spawned.Add(instance);
                }
            }
        }

        private void SpawnCollectibleCategory(
            int count,
            List<Transform> points,
            List<int> pointOrder,
            ref int cursor,
            int seed)
        {
            if (count <= 0 || points.Count == 0 || pointOrder.Count == 0)
            {
                return;
            }

            int actualCount = Mathf.Min(count, pointOrder.Count - cursor);
            for (int i = 0; i < actualCount; i++)
            {
                int pointIndex = pointOrder[cursor++];
                Transform point = points[pointIndex];
                if (point == null)
                {
                    continue;
                }

                GameObject prefab = PickCollectiblePrefab(seed + i);
                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = point.position + Vector3.up * collectibleYOffset;
                GameObject instance = pool.Get(prefab, position, Quaternion.identity);
                if (instance != null)
                {
                    spawned.Add(instance);
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

            ordered.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            int count = ordered.Count;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float x = Mathf.Lerp(left, right, t);
                Vector3 position = ordered[i].position;
                position.x = x;
                ordered[i].position = position;
            }
        }

#if UNITY_EDITOR
        private static GameObject[] SanitizePrefabs(GameObject[] prefabs, string fallbackName)
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
            string path = $"Assets/Game/Prefabs/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return new[] { prefab };
            }

            return Array.Empty<GameObject>();
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
