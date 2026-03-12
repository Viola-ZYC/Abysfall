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

        private readonly List<Transform> segments = new();
        private float segmentHeight;
        private float segmentStep;

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

            float cameraY = followTarget != null ? followTarget.position.y : targetCamera.transform.position.y;
            float camHalfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;

            float minY = float.PositiveInfinity;
            for (int i = 0; i < segments.Count; i++)
            {
                minY = Mathf.Min(minY, segments[i].position.y);
            }

            float recycleThreshold = camHalfHeight + segmentHeight * 0.5f + recycleBuffer;
            for (int i = 0; i < segments.Count; i++)
            {
                Transform seg = segments[i];
                float above = seg.position.y - cameraY;
                if (above > recycleThreshold)
                {
                    seg.position = new Vector3(seg.position.x, minY - segmentStep, seg.position.z);
                    minY = seg.position.y;
                }
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
            Tilemap tilemap = segments[0].GetComponentInChildren<Tilemap>();
            if (tilemap != null)
            {
                float scale = Mathf.Abs(tilemap.transform.lossyScale.y);
                height = tilemap.localBounds.size.y * (scale <= 0f ? 1f : scale);
            }
            else
            {
                TilemapRenderer renderer = segments[0].GetComponentInChildren<TilemapRenderer>();
                if (renderer != null)
                {
                    height = renderer.bounds.size.y;
                }
            }

            segmentHeight = Mathf.Max(0.01f, height);
            segmentStep = Mathf.Max(0.01f, segmentHeight - Mathf.Max(0f, segmentOverlap));
        }

        private void StackSegments()
        {
            if (segments.Count == 0 || segmentStep <= 0f)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                Vector3 pos = segments[i].position;
                pos.x = transform.position.x;
                pos.y = transform.position.y - segmentStep * i;
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
    }
}
