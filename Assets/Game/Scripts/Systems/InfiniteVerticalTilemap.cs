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
        private const string FallbackSpriteResourcePath = "Art/Sprite/Cave Background/Cave";
        private const string FallbackSpriteNamePrimary = "Back";
        private const string FallbackSpriteNameSecondary = "Mid";
        private const string FallbackSpriteChildName = "__BackgroundFallbackSprite";
        private const float DefaultSegmentVisualScaleMultiplier = 1.2f;

        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private GameObject backgroundPrefab;

        [Header("Layout")]
        [SerializeField, Min(1)] private int segmentCount = 3;
        [SerializeField, Min(0f)] private float recycleBuffer = 1f;
        [SerializeField, Min(0f)] private float segmentOverlap = 0.05f;
        [SerializeField, Min(1f)] private float segmentVisualScaleMultiplier = 1.2f;
        [SerializeField] private bool buildInEditMode = true;
        [SerializeField] private bool keepInSync = false;
        [SerializeField] private bool alignTopSegmentToFollowTarget = true;

        private readonly List<Transform> segments = new();
        private readonly Dictionary<Transform, Vector3> segmentBaseLocalScales = new();
        private float segmentHeight;
        private float segmentWidth;
        private float segmentStep;
        private float segmentBottomOffset;
        private float segmentCenterOffsetX;
        private bool layoutInitialized;
        private bool initializedForPlayMode;
        private Sprite fallbackSprite;

        private void Awake()
        {
            InitializeLayout(forceRestack: true);
        }

        private void OnEnable()
        {
            InitializeLayout(forceRestack: true);
        }

        private void OnValidate()
        {
            InitializeLayout(forceRestack: true);
        }

        private void OnTransformChildrenChanged()
        {
            layoutInitialized = false;
        }

        private void LateUpdate()
        {
            if (!EnsureLayoutReady())
            {
                return;
            }

            if (!TryGetViewportMetrics(out float cameraY, out float camHalfHeight))
            {
                return;
            }

            float cameraBottom = cameraY - camHalfHeight;

            for (int iteration = 0; iteration < segments.Count; iteration++)
            {
                if (!TryGetLowestAndHighestSegments(out Transform lowestSegment, out Transform highestSegment))
                {
                    break;
                }

                if (ReferenceEquals(lowestSegment, highestSegment) || cameraBottom > GetSegmentCenterY(lowestSegment))
                {
                    break;
                }

                float targetBottom = GetSegmentBottom(lowestSegment) - segmentStep;
                SetSegmentBottom(highestSegment, targetBottom);
            }
        }

        private bool ShouldRunInCurrentMode()
        {
            return Application.isPlaying || buildInEditMode;
        }

        private void InitializeLayout(bool forceRestack)
        {
            if (!ShouldRunInCurrentMode())
            {
                layoutInitialized = false;
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (backgroundPrefab == null)
            {
                layoutInitialized = false;
                return;
            }

            EnsureSegments();
            CacheSegments();
            ApplySegmentScaleMultiplier();
            RecalculateSegmentHeight();

            if (forceRestack)
            {
                StackSegments();
            }

            EnsureFallbackVisuals();

            layoutInitialized = segments.Count > 0 && segmentHeight > 0f && segmentStep > 0f;
            initializedForPlayMode = Application.isPlaying;
        }

        private bool EnsureLayoutReady()
        {
            if (!ShouldRunInCurrentMode())
            {
                return false;
            }

            // Rebuild when switching into play mode so stale editor positions do not leak into runtime.
            bool needsInitialization = !layoutInitialized ||
                                       initializedForPlayMode != Application.isPlaying ||
                                       segments.Count == 0 ||
                                       segmentHeight <= 0f ||
                                       segmentStep <= 0f;

            if (needsInitialization)
            {
                InitializeLayout(forceRestack: true);
            }

            return layoutInitialized;
        }

        private bool TryGetViewportMetrics(out float centerY, out float halfHeight)
        {
            centerY = transform.position.y;
            halfHeight = 5f;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                centerY = targetCamera.transform.position.y;
                halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;
                return true;
            }

            if (followTarget != null)
            {
                centerY = followTarget.position.y;
                return true;
            }

            return false;
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
            HashSet<Transform> activeSegments = new();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponentInChildren<TilemapRenderer>() != null)
                {
                    segments.Add(child);
                    activeSegments.Add(child);

                    if (!segmentBaseLocalScales.ContainsKey(child))
                    {
                        segmentBaseLocalScales[child] = child.localScale;
                    }
                }
            }

            if (segmentBaseLocalScales.Count == activeSegments.Count)
            {
                return;
            }

            List<Transform> staleSegments = null;
            foreach (Transform segment in segmentBaseLocalScales.Keys)
            {
                if (activeSegments.Contains(segment))
                {
                    continue;
                }

                staleSegments ??= new List<Transform>();
                staleSegments.Add(segment);
            }

            if (staleSegments == null)
            {
                return;
            }

            for (int i = 0; i < staleSegments.Count; i++)
            {
                segmentBaseLocalScales.Remove(staleSegments[i]);
            }
        }

        private void ApplySegmentScaleMultiplier()
        {
            float scaleMultiplier = GetSegmentVisualScaleMultiplier();
            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i];
                if (!segmentBaseLocalScales.TryGetValue(segment, out Vector3 baseScale))
                {
                    baseScale = segment.localScale;
                    segmentBaseLocalScales[segment] = baseScale;
                }

                segment.localScale = new Vector3(
                    baseScale.x * scaleMultiplier,
                    baseScale.y * scaleMultiplier,
                    baseScale.z);
            }
        }

        private float GetSegmentVisualScaleMultiplier()
        {
            if (segmentVisualScaleMultiplier <= 0f)
            {
                return DefaultSegmentVisualScaleMultiplier;
            }

            return Mathf.Max(1f, segmentVisualScaleMultiplier);
        }

        private void RecalculateSegmentHeight()
        {
            if (segments.Count == 0)
            {
                return;
            }

            float height = 0f;
            float width = 0f;
            segmentBottomOffset = 0f;
            segmentCenterOffsetX = 0f;

            Transform sample = segments[0];
            Tilemap tilemap = sample.GetComponentInChildren<Tilemap>();
            if (tilemap != null && TryGetUsedCellBounds(tilemap, out BoundsInt usedBounds))
            {
                Vector3 minWorld = tilemap.CellToWorld(new Vector3Int(usedBounds.xMin, usedBounds.yMin, 0));
                Vector3 maxWorldY = tilemap.CellToWorld(new Vector3Int(usedBounds.xMin, usedBounds.yMax, 0));
                Vector3 maxWorldX = tilemap.CellToWorld(new Vector3Int(usedBounds.xMax, usedBounds.yMin, 0));
                height = Mathf.Abs(maxWorldY.y - minWorld.y);
                width = Mathf.Abs(maxWorldX.x - minWorld.x);
                segmentBottomOffset = sample.position.y - minWorld.y;
                float centerX = (minWorld.x + maxWorldX.x) * 0.5f;
                segmentCenterOffsetX = sample.position.x - centerX;
            }
            else if (TryGetSegmentBounds(sample, out Bounds bounds))
            {
                height = bounds.size.y;
                width = bounds.size.x;
                segmentBottomOffset = sample.position.y - bounds.min.y;
                segmentCenterOffsetX = sample.position.x - bounds.center.x;
            }

            segmentWidth = Mathf.Max(0.01f, width);
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
                pos.x = transform.position.x + segmentCenterOffsetX;
                float targetBottom = baseBottom - segmentStep * i;
                pos.y = targetBottom + GetSegmentBottomOffset(segments[i]);
                segments[i].position = pos;
            }
        }

        private void EnsureFallbackVisuals()
        {
            if (!Application.isPlaying || segments.Count == 0 || segmentWidth <= 0f || segmentHeight <= 0f)
            {
                return;
            }

            Sprite sprite = GetFallbackSprite();
            if (sprite == null)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                EnsureFallbackVisual(segments[i], sprite);
            }
        }

        private Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(FallbackSpriteResourcePath);
            if (sprites == null || sprites.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == FallbackSpriteNamePrimary)
                {
                    fallbackSprite = sprites[i];
                    return fallbackSprite;
                }
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == FallbackSpriteNameSecondary)
                {
                    fallbackSprite = sprites[i];
                    return fallbackSprite;
                }
            }

            fallbackSprite = sprites[0];
            return fallbackSprite;
        }

        private void EnsureFallbackVisual(Transform segment, Sprite sprite)
        {
            if (segment == null || sprite == null)
            {
                return;
            }

            Transform fallback = segment.Find(FallbackSpriteChildName);
            if (fallback == null)
            {
                GameObject fallbackObject = new GameObject(FallbackSpriteChildName);
                fallbackObject.transform.SetParent(segment, false);
                fallback = fallbackObject.transform;
            }

            SpriteRenderer renderer = fallback.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = fallback.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = -100;
            renderer.color = Color.white;

            fallback.localPosition = new Vector3(
                -segmentCenterOffsetX,
                -segmentBottomOffset + segmentHeight * 0.5f,
                0f);

            Vector2 spriteSize = sprite.bounds.size;
            Vector3 parentScale = segment.lossyScale;
            float scaleX = segmentWidth / Mathf.Max(0.01f, spriteSize.x * Mathf.Max(0.01f, parentScale.x));
            float scaleY = segmentHeight / Mathf.Max(0.01f, spriteSize.y * Mathf.Max(0.01f, parentScale.y));
            fallback.localScale = new Vector3(scaleX, scaleY, 1f);
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

        private float GetSegmentCenterY(Transform segment)
        {
            return GetSegmentBottom(segment) + segmentHeight * 0.5f;
        }

        private bool TryGetLowestAndHighestSegments(out Transform lowestSegment, out Transform highestSegment)
        {
            lowestSegment = null;
            highestSegment = null;

            float lowestY = float.PositiveInfinity;
            float highestY = float.NegativeInfinity;

            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i];
                float positionY = segment.position.y;
                if (positionY < lowestY)
                {
                    lowestY = positionY;
                    lowestSegment = segment;
                }

                if (positionY > highestY)
                {
                    highestY = positionY;
                    highestSegment = segment;
                }
            }

            return lowestSegment != null && highestSegment != null;
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
