using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EndlessRunner
{
    [ExecuteAlways]
    public class InfiniteVerticalTilemap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private GameObject backgroundPrefab;

        [Header("Layout")]
        [SerializeField, Min(1)] private int segmentCount = 3;
        [SerializeField, Min(0f)] private float recycleBuffer = 1f;
        [SerializeField, Min(0f)] private float segmentOverlap = 0.05f;
        [SerializeField] private bool buildInEditMode = true;
        [SerializeField] private bool keepInSync = false;
        [SerializeField] private bool alignTopSegmentToFollowTarget = true;

        private readonly List<Transform> segments = new();
        private float segmentHeight;
        private float segmentStep;
        private float segmentBottomOffset;

        private void Awake()
        {
            if (!Application.isPlaying && !buildInEditMode)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (backgroundPrefab == null)
            {
                enabled = false;
                return;
            }

            EnsureSegments();
            CacheSegments();
            RecalculateSegmentHeight();
            StackSegments();
        }

        private void OnEnable()
        {
            if (Application.isPlaying || !buildInEditMode)
            {
                return;
            }

            if (backgroundPrefab == null)
            {
                return;
            }

            EnsureSegments();
            CacheSegments();
            RecalculateSegmentHeight();
            StackSegments();
        }

        private void OnValidate()
        {
            if (Application.isPlaying || !buildInEditMode)
            {
                return;
            }

            if (backgroundPrefab == null)
            {
                return;
            }

            EnsureSegments();
            CacheSegments();
            RecalculateSegmentHeight();
            StackSegments();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !buildInEditMode)
            {
                return;
            }

            if (segments.Count == 0 || targetCamera == null || segmentHeight <= 0f || segmentStep <= 0f)
            {
                return;
            }

            float cameraY = targetCamera != null ? targetCamera.transform.position.y : transform.position.y;
            if (targetCamera == null && followTarget != null)
            {
                cameraY = followTarget.position.y;
            }
            float camHalfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;
            float cameraBottom = cameraY - camHalfHeight;

            float recycleThreshold = segmentStep + recycleBuffer;
            for (int iteration = 0; iteration < segments.Count; iteration++)
            {
                float minBottom = float.PositiveInfinity;
                Transform recycleCandidate = null;
                float maxAbove = float.NegativeInfinity;

                for (int i = 0; i < segments.Count; i++)
                {
                    Transform seg = segments[i];
                    float segBottom = GetSegmentBottom(seg);
                    minBottom = Mathf.Min(minBottom, segBottom);

                    float aboveBottom = segBottom - cameraBottom;
                    if (aboveBottom > recycleThreshold && aboveBottom > maxAbove)
                    {
                        maxAbove = aboveBottom;
                        recycleCandidate = seg;
                    }
                }

                if (recycleCandidate == null)
                {
                    break;
                }

                float targetBottom = minBottom - segmentStep;
                SetSegmentBottom(recycleCandidate, targetBottom);
            }
        }

        private void EnsureSegments()
        {
            int existing = transform.childCount;
            if (existing > segmentCount && keepInSync)
            {
                for (int i = transform.childCount - 1; i >= segmentCount; i--)
                {
                    GameObject child = transform.GetChild(i).gameObject;
                    if (Application.isPlaying)
                    {
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
                }

                existing = transform.childCount;
            }

            if (existing >= segmentCount)
            {
                return;
            }

            for (int i = existing; i < segmentCount; i++)
            {
                GameObject instance = CreateInstance();
                instance.transform.SetParent(transform, false);
                instance.name = $"BG_{i}";
            }
        }

        private void CacheSegments()
        {
            segments.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponentInChildren<TilemapRenderer>() != null)
                {
                    segments.Add(child);
                }
            }
        }

        private void RecalculateSegmentHeight()
        {
            if (segments.Count == 0)
            {
                return;
            }

            float height = 0f;
            segmentBottomOffset = 0f;

            Transform sample = segments[0];
            Tilemap tilemap = sample.GetComponentInChildren<Tilemap>();
            if (tilemap != null && TryGetUsedCellBounds(tilemap, out BoundsInt usedBounds))
            {
                Vector3 minWorld = tilemap.CellToWorld(new Vector3Int(usedBounds.xMin, usedBounds.yMin, 0));
                Vector3 maxWorld = tilemap.CellToWorld(new Vector3Int(usedBounds.xMin, usedBounds.yMax, 0));
                height = Mathf.Abs(maxWorld.y - minWorld.y);
                segmentBottomOffset = sample.position.y - minWorld.y;
            }
            else if (TryGetSegmentBounds(sample, out Bounds bounds))
            {
                height = bounds.size.y;
                segmentBottomOffset = sample.position.y - bounds.min.y;
            }

            segmentHeight = Mathf.Max(0.01f, height);
            segmentStep = Mathf.Max(0.01f, segmentHeight - Mathf.Max(0f, segmentOverlap));
            segmentBottomOffset = Mathf.Max(0.01f, segmentBottomOffset > 0f ? segmentBottomOffset : segmentHeight * 0.5f);
        }

        private void StackSegments()
        {
            if (segments.Count == 0 || segmentStep <= 0f)
            {
                return;
            }

            float baseBottom = GetSegmentBottom(segments[0]);
            if (alignTopSegmentToFollowTarget)
            {
                Transform anchor = followTarget != null ? followTarget : (targetCamera != null ? targetCamera.transform : null);
                if (anchor != null)
                {
                    baseBottom = anchor.position.y - segmentHeight * 0.5f;
                }
            }
            for (int i = 0; i < segments.Count; i++)
            {
                Vector3 pos = segments[i].position;
                pos.x = transform.position.x;
                float targetBottom = baseBottom - segmentStep * i;
                pos.y = targetBottom + GetSegmentBottomOffset(segments[i]);
                segments[i].position = pos;
            }
        }

        private GameObject CreateInstance()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(backgroundPrefab) as GameObject;
                if (instance != null)
                {
                    return instance;
                }
            }
#endif

            return Instantiate(backgroundPrefab);
        }

        private static bool TryGetUsedCellBounds(Tilemap tilemap, out BoundsInt bounds)
        {
            BoundsInt cellBounds = tilemap.cellBounds;
            bool found = false;
            int minX = 0;
            int minY = 0;
            int maxX = 0;
            int maxY = 0;

            foreach (Vector3Int position in cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(position))
                {
                    continue;
                }

                if (!found)
                {
                    minX = position.x;
                    maxX = position.x;
                    minY = position.y;
                    maxY = position.y;
                    found = true;
                }
                else
                {
                    minX = Mathf.Min(minX, position.x);
                    maxX = Mathf.Max(maxX, position.x);
                    minY = Mathf.Min(minY, position.y);
                    maxY = Mathf.Max(maxY, position.y);
                }
            }

            if (!found)
            {
                bounds = cellBounds;
                return false;
            }

            bounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
            return true;
        }

        private static bool TryGetSegmentBounds(Transform segment, out Bounds bounds)
        {
            TilemapRenderer renderer = segment.GetComponentInChildren<TilemapRenderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            Tilemap tilemap = segment.GetComponentInChildren<Tilemap>();
            if (tilemap != null)
            {
                Bounds local = tilemap.localBounds;
                Vector3 worldCenter = tilemap.transform.TransformPoint(local.center);
                Vector3 worldSize = Vector3.Scale(local.size, tilemap.transform.lossyScale);
                bounds = new Bounds(worldCenter, worldSize);
                return true;
            }

            bounds = default;
            return false;
        }

        private float GetSegmentBottom(Transform segment)
        {
            return segment.position.y - segmentBottomOffset;
        }

        private float GetSegmentBottomOffset(Transform segment)
        {
            return segmentBottomOffset;
        }

        private void SetSegmentBottom(Transform segment, float targetBottom)
        {
            Vector3 pos = segment.position;
            pos.y = targetBottom + segmentBottomOffset;
            segment.position = pos;
        }
    }
}
