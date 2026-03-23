using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class TrackManager : MonoBehaviour
    {
        [SerializeField] private TrackConfig config;
        [SerializeField] private Transform player;
        [SerializeField] private ObjectPool pool;
        [SerializeField] private ScoreManager scoreManager;
        [Header("Adaptive Distances")]
        [SerializeField] private bool adaptDistancesToCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.1f)] private float referenceCameraHalfHeight = 5f;
        [SerializeField, Min(0f)] private float minDistanceScale = 0.85f;
        [SerializeField, Min(0f)] private float maxDistanceScale = 1.35f;
        [Header("Deterministic Segment Pattern")]
        [SerializeField] private bool deterministicSegmentSelection = true;
        [SerializeField, Min(1f)] private float segmentProgressScale = 220f;
        [SerializeField, Min(0f)] private float segmentProgressWeight = 1.35f;
        [SerializeField, Min(1)] private int segmentSequenceStride = 1;

        private readonly Queue<TrackSegment> activeSegments = new();
        private float nextSpawnY;
        private int spawnedSegmentCount;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (player == null || pool == null || targetCamera == null || scoreManager == null)
            {
                ResolveReferences();
            }

            if (config == null || player == null)
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Running)
            {
                return;
            }

            float distanceScale = GetDistanceScale();
            float recycleDistance = config.recycleDistance * distanceScale;
            float spawnAheadDistance = config.spawnAheadDistance * distanceScale;

            while (activeSegments.Count > 0)
            {
                TrackSegment segment = activeSegments.Peek();
                if (player.position.y < segment.EndY - recycleDistance)
                {
                    activeSegments.Dequeue();
                    ReleaseSegment(segment);
                }
                else
                {
                    break;
                }
            }

            while (nextSpawnY > player.position.y - spawnAheadDistance)
            {
                SpawnSegment();
            }
        }

        public void ResetTrack()
        {
            ResolveReferences();
            ClearSegments();
            nextSpawnY = player != null ? player.position.y : 0f;
            spawnedSegmentCount = 0;

            if (config == null)
            {
                return;
            }

            for (int i = 0; i < config.initialSegments; i++)
            {
                SpawnSegment();
            }
        }

        /// <summary>
        /// Runtime hook: updates the tracked player transform for spawn/recycle logic.
        /// </summary>
        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        private void SpawnSegment()
        {
            if (config == null || config.segmentPrefabs == null || config.segmentPrefabs.Length == 0)
            {
                return;
            }

            TrackSegment prefab = SelectSegmentPrefab();
            if (prefab == null)
            {
                return;
            }

            Vector3 origin = transform.position;
            Vector3 position = new Vector3(origin.x, nextSpawnY, origin.z);
            TrackSegment segment;

            if (pool != null)
            {
                GameObject instance = pool.Get(prefab.gameObject, position, Quaternion.identity);
                segment = instance != null ? instance.GetComponent<TrackSegment>() : null;
            }
            else
            {
                segment = Instantiate(prefab, position, Quaternion.identity);
            }

            if (segment == null)
            {
                return;
            }

            activeSegments.Enqueue(segment);
            spawnedSegmentCount++;

            float endY = segment.EndY;
            nextSpawnY = Mathf.Min(nextSpawnY - segment.Length, endY);
        }

        private TrackSegment SelectSegmentPrefab()
        {
            if (config == null || config.segmentPrefabs == null || config.segmentPrefabs.Length == 0)
            {
                return null;
            }

            int index;
            if (deterministicSegmentSelection)
            {
                int score = scoreManager != null ? scoreManager.Score : 0;
                float progress = Mathf.Log(1f + Mathf.Max(0f, score) / Mathf.Max(1f, segmentProgressScale));
                int progressOffset = Mathf.FloorToInt(progress * Mathf.Max(0f, segmentProgressWeight));
                int stride = Mathf.Max(1, segmentSequenceStride);
                index = PositiveModulo(spawnedSegmentCount * stride + progressOffset, config.segmentPrefabs.Length);
            }
            else
            {
                index = PositiveModulo(spawnedSegmentCount, config.segmentPrefabs.Length);
            }

            return config.segmentPrefabs[index];
        }

        private void ReleaseSegment(TrackSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            if (pool != null)
            {
                pool.Release(segment.gameObject);
            }
            else
            {
                Destroy(segment.gameObject);
            }
        }

        private void ClearSegments()
        {
            while (activeSegments.Count > 0)
            {
                ReleaseSegment(activeSegments.Dequeue());
            }
        }

        private float GetDistanceScale()
        {
            if (!adaptDistancesToCamera)
            {
                return 1f;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null || !targetCamera.orthographic)
            {
                return 1f;
            }

            float baseHalfHeight = Mathf.Max(0.1f, referenceCameraHalfHeight);
            float distanceScale = targetCamera.orthographicSize / baseHalfHeight;

            float minScale = minDistanceScale > 0f ? minDistanceScale : 0f;
            float maxScale = maxDistanceScale > 0f ? maxDistanceScale : float.MaxValue;
            if (minScale > maxScale)
            {
                (minScale, maxScale) = (maxScale, minScale);
            }

            return Mathf.Clamp(distanceScale, minScale, maxScale);
        }

        private void ResolveReferences()
        {
            if (pool == null)
            {
                pool = ObjectPool.Instance;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (player == null)
            {
                RunnerController runner = FindAnyObjectByType<RunnerController>();
                player = runner != null ? runner.transform : null;
            }

            if (scoreManager == null)
            {
                scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
            }
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
