using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EndlessRunner
{
    [ExecuteAlways]
    public class InfiniteVerticalTilemap : MonoBehaviour
    {
        private const float DefaultSegmentVisualScaleMultiplier = 1.2f;

        private static readonly LayerGroupDefinition[] GroupDefinitions =
        {
            new LayerGroupDefinition("BaseRoot", "Layer_Base"),
            new LayerGroupDefinition("SuperFarRoot", "Layer_SuperFar"),
            new LayerGroupDefinition("FarRoot", "Layer_Far", "Layer_FarLight"),
            new LayerGroupDefinition("CloseRoot", "Layer_Close", "Layer_CloseLight"),
            new LayerGroupDefinition("WallRoot", "LeftWall", "RightWall")
        };

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
        [SerializeField] private bool alignTopSegmentToFollowTarget = true;

        [Header("Parallax")]
        [SerializeField, Range(0f, 1f)] private float baseLayerVerticalFollow = 0.97f;
        [SerializeField, Range(0f, 1f)] private float superFarLayerVerticalFollow = 0.9f;
        [SerializeField, Range(0f, 1f)] private float farLayerVerticalFollow = 0.79f;
        [SerializeField, Range(0f, 1f)] private float closeLayerVerticalFollow = 0.63f;
        [SerializeField, Range(0f, 1f)] private float wallLayerVerticalFollow = 0f;
        [SerializeField] private bool fitWallsToViewport = true;
        [SerializeField, Min(0f)] private float wallViewportInset = 0.15f;

        private readonly List<LayerGroupRuntime> groups = new();
        private float segmentHeight;
        private float segmentStep;
        private float segmentBottomOffset;
        private float segmentCenterOffsetX;
        private bool layoutInitialized;
        private bool initializedForPlayMode;
        private bool hasCapturedFollowTargetOffset;
        private float followTargetOffsetY;
        private Vector3 lastAnchorPosition;
        private bool hasLastAnchorPosition;
        private float wallLeftBoundWorldX;
        private float wallRightBoundWorldX;
        private bool hasWallBoundsOverride;
#if UNITY_EDITOR
        private bool queuedEditorValidationRebuild;
        private bool applyingQueuedValidationRebuild;
#endif

        private sealed class LayerGroupDefinition
        {
            public LayerGroupDefinition(string rootName, params string[] sourceChildren)
            {
                RootName = rootName;
                SourceChildren = sourceChildren;
            }

            public string RootName { get; }
            public string[] SourceChildren { get; }

            public bool ContainsChild(string childName)
            {
                for (int i = 0; i < SourceChildren.Length; i++)
                {
                    if (SourceChildren[i] == childName)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class LayerGroupRuntime
        {
            public LayerGroupRuntime(LayerGroupDefinition definition)
            {
                Definition = definition;
            }

            public LayerGroupDefinition Definition { get; }
            public Transform Root { get; set; }
            public readonly List<Transform> Segments = new();
        }

        private void Awake()
        {
            InitializeLayout(forceRestack: true);
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            CancelQueuedEditorValidationRebuild();
#endif
            InitializeLayout(forceRestack: true);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !buildInEditMode)
            {
                layoutInitialized = false;
                return;
            }

            if (applyingQueuedValidationRebuild)
            {
                return;
            }

            QueueEditorValidationRebuild();
#endif
        }

        private void OnTransformChildrenChanged()
        {
            layoutInitialized = false;
            hasLastAnchorPosition = false;
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            CancelQueuedEditorValidationRebuild();
        }
#endif

        private void LateUpdate()
        {
            if (!EnsureLayoutReady())
            {
                return;
            }

            if (!TryGetViewportMetrics(out float cameraY, out float cameraHalfHeight))
            {
                return;
            }

            ApplyAnchorDrivenParallax();

            float cameraBottom = cameraY - cameraHalfHeight;
            float cameraTop = cameraY + cameraHalfHeight;
            for (int i = 0; i < groups.Count; i++)
            {
                RecycleGroup(groups[i], cameraTop, cameraBottom);
            }

            AlignWallsToViewport();
        }

        private bool ShouldRunInCurrentMode()
        {
            return Application.isPlaying || buildInEditMode;
        }

#if UNITY_EDITOR
        private void QueueEditorValidationRebuild()
        {
            layoutInitialized = false;
            if (queuedEditorValidationRebuild)
            {
                return;
            }

            queuedEditorValidationRebuild = true;
            EditorApplication.delayCall += ExecuteQueuedEditorValidationRebuild;
        }

        private void ExecuteQueuedEditorValidationRebuild()
        {
            EditorApplication.delayCall -= ExecuteQueuedEditorValidationRebuild;
            queuedEditorValidationRebuild = false;

            if (this == null)
            {
                return;
            }

            applyingQueuedValidationRebuild = true;
            try
            {
                InitializeLayout(forceRestack: true);
            }
            finally
            {
                applyingQueuedValidationRebuild = false;
            }
        }

        private void CancelQueuedEditorValidationRebuild()
        {
            if (!queuedEditorValidationRebuild)
            {
                return;
            }

            EditorApplication.delayCall -= ExecuteQueuedEditorValidationRebuild;
            queuedEditorValidationRebuild = false;
        }
#endif

        private void InitializeLayout(bool forceRestack)
        {
            if (!ShouldRunInCurrentMode())
            {
                layoutInitialized = false;
                hasLastAnchorPosition = false;
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

            EnsureGroupedHierarchy();
            CacheGroups();
            ApplySegmentScaleMultiplier();
            RecalculateSegmentMetrics();
            CaptureFollowTargetOffset();

            if (forceRestack)
            {
                StackSegments();
            }

            SyncAnchorTrackingToCurrentAnchor();
            AlignWallsToViewport();

            layoutInitialized = groups.Count == GroupDefinitions.Length &&
                                segmentHeight > 0f &&
                                segmentStep > 0f &&
                                AreAllGroupsPopulated();
            initializedForPlayMode = Application.isPlaying;
        }

        private bool EnsureLayoutReady()
        {
            if (!ShouldRunInCurrentMode())
            {
                return false;
            }

            bool needsInitialization = !layoutInitialized ||
                                       initializedForPlayMode != Application.isPlaying ||
                                       segmentHeight <= 0f ||
                                       segmentStep <= 0f ||
                                       groups.Count != GroupDefinitions.Length ||
                                       !AreAllGroupsPopulated() ||
                                       !HasValidGroupedHierarchy();

            if (needsInitialization)
            {
                InitializeLayout(forceRestack: true);
            }

            return layoutInitialized;
        }

        public void SetWallBounds(float leftWorldX, float rightWorldX)
        {
            if (leftWorldX > rightWorldX)
            {
                (leftWorldX, rightWorldX) = (rightWorldX, leftWorldX);
            }

            wallLeftBoundWorldX = leftWorldX;
            wallRightBoundWorldX = rightWorldX;
            hasWallBoundsOverride = true;
            AlignWallsToViewport();
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

        private bool TryGetAnchorPosition(out Vector3 anchorPosition)
        {
            if (followTarget != null)
            {
                anchorPosition = followTarget.position;
                return true;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                anchorPosition = targetCamera.transform.position;
                return true;
            }

            anchorPosition = transform.position;
            return false;
        }

        private void SyncAnchorTrackingToCurrentAnchor()
        {
            if (!TryGetAnchorPosition(out Vector3 anchorPosition))
            {
                hasLastAnchorPosition = false;
                return;
            }

            lastAnchorPosition = anchorPosition;
            hasLastAnchorPosition = true;
        }

        private void ApplyAnchorDrivenParallax()
        {
            if (!TryGetAnchorPosition(out Vector3 anchorPosition))
            {
                hasLastAnchorPosition = false;
                return;
            }

            if (!hasLastAnchorPosition)
            {
                lastAnchorPosition = anchorPosition;
                hasLastAnchorPosition = true;
                return;
            }

            Vector3 anchorDelta = anchorPosition - lastAnchorPosition;
            if (anchorDelta.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                LayerGroupRuntime runtime = groups[i];
                float verticalFollow = GetVerticalFollowMultiplier(runtime.Definition.RootName);
                if (Mathf.Approximately(verticalFollow, 0f))
                {
                    continue;
                }

                TranslateSegmentsVertically(runtime.Segments, anchorDelta.y * verticalFollow);
            }

            lastAnchorPosition = anchorPosition;
        }

        private void EnsureGroupedHierarchy()
        {
            if (HasValidGroupedHierarchy())
            {
                return;
            }

            ClearDirectChildren();

            for (int groupIndex = 0; groupIndex < GroupDefinitions.Length; groupIndex++)
            {
                LayerGroupDefinition definition = GroupDefinitions[groupIndex];
                Transform groupRoot = CreateGroupRoot(definition.RootName, groupIndex);
                for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                {
                    Transform segment = CreateGroupedSegment(definition, segmentIndex);
                    segment.SetParent(groupRoot, false);
                }
            }
        }

        private bool HasValidGroupedHierarchy()
        {
            if (transform.childCount != GroupDefinitions.Length)
            {
                return false;
            }

            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                LayerGroupDefinition definition = GroupDefinitions[i];
                Transform groupRoot = transform.Find(definition.RootName);
                if (groupRoot == null || groupRoot.parent != transform || groupRoot.childCount != segmentCount)
                {
                    return false;
                }

                for (int childIndex = 0; childIndex < groupRoot.childCount; childIndex++)
                {
                    if (!SegmentMatchesDefinition(groupRoot.GetChild(childIndex), definition))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ClearDirectChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyForLayoutRebuild(transform.GetChild(i).gameObject);
            }
        }

        private Transform CreateGroupRoot(string rootName, int siblingIndex)
        {
            GameObject rootObject = new GameObject(rootName);
            Transform root = rootObject.transform;
            root.SetParent(transform, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.SetSiblingIndex(siblingIndex);
            return root;
        }

        private Transform CreateGroupedSegment(LayerGroupDefinition definition, int segmentIndex)
        {
            GameObject instance = CreateInstance();
            instance.name = $"Seg_{segmentIndex}";
            PruneSegmentToGroup(instance.transform, definition);
            return instance.transform;
        }

        private void PruneSegmentToGroup(Transform segment, LayerGroupDefinition definition)
        {
            for (int i = segment.childCount - 1; i >= 0; i--)
            {
                Transform child = segment.GetChild(i);
                if (!definition.ContainsChild(child.name))
                {
                    DestroyForLayoutRebuild(child.gameObject);
                }
            }
        }

        private bool SegmentMatchesDefinition(Transform segment, LayerGroupDefinition definition)
        {
            if (segment == null || segment.childCount != definition.SourceChildren.Length)
            {
                return false;
            }

            for (int i = 0; i < definition.SourceChildren.Length; i++)
            {
                if (segment.Find(definition.SourceChildren[i]) == null)
                {
                    return false;
                }
            }

            for (int i = 0; i < segment.childCount; i++)
            {
                if (!definition.ContainsChild(segment.GetChild(i).name))
                {
                    return false;
                }
            }

            return true;
        }

        private void CacheGroups()
        {
            groups.Clear();

            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                LayerGroupDefinition definition = GroupDefinitions[i];
                LayerGroupRuntime runtime = new LayerGroupRuntime(definition);
                runtime.Root = transform.Find(definition.RootName);

                if (runtime.Root != null)
                {
                    for (int childIndex = 0; childIndex < runtime.Root.childCount; childIndex++)
                    {
                        Transform segment = runtime.Root.GetChild(childIndex);
                        if (!HasSegmentVisuals(segment))
                        {
                            continue;
                        }

                        runtime.Segments.Add(segment);
                    }
                }

                groups.Add(runtime);
            }
        }

        private bool AreAllGroupsPopulated()
        {
            if (groups.Count != GroupDefinitions.Length)
            {
                return false;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Root == null || groups[i].Segments.Count != segmentCount)
                {
                    return false;
                }
            }

            return true;
        }

        private float GetVerticalFollowMultiplier(string rootName)
        {
            return rootName switch
            {
                "BaseRoot" => Mathf.Clamp01(baseLayerVerticalFollow),
                "SuperFarRoot" => Mathf.Clamp01(superFarLayerVerticalFollow),
                "FarRoot" => Mathf.Clamp01(farLayerVerticalFollow),
                "CloseRoot" => Mathf.Clamp01(closeLayerVerticalFollow),
                "WallRoot" => Mathf.Clamp01(wallLayerVerticalFollow),
                _ => 1f
            };
        }

        private static void TranslateSegmentsVertically(List<Transform> runtimeSegments, float deltaY)
        {
            if (Mathf.Approximately(deltaY, 0f))
            {
                return;
            }

            for (int i = 0; i < runtimeSegments.Count; i++)
            {
                Transform segment = runtimeSegments[i];
                Vector3 position = segment.position;
                position.y += deltaY;
                segment.position = position;
            }
        }

        private void ApplySegmentScaleMultiplier()
        {
            float scaleMultiplier = GetSegmentVisualScaleMultiplier();
            Vector3 baseScale = GetBackgroundPrefabBaseLocalScale();
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                List<Transform> runtimeSegments = groups[groupIndex].Segments;
                for (int segmentIndex = 0; segmentIndex < runtimeSegments.Count; segmentIndex++)
                {
                    Transform segment = runtimeSegments[segmentIndex];
                    segment.localScale = new Vector3(
                        baseScale.x * scaleMultiplier,
                        baseScale.y * scaleMultiplier,
                        baseScale.z);
                }
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

        private Vector3 GetBackgroundPrefabBaseLocalScale()
        {
            if (backgroundPrefab != null)
            {
                return backgroundPrefab.transform.localScale;
            }

            return Vector3.one;
        }

        private void RecalculateSegmentMetrics()
        {
            segmentHeight = 0f;
            segmentStep = 0f;
            segmentBottomOffset = 0f;
            segmentCenterOffsetX = 0f;

            GameObject sample = CreateInstance();
            if (sample == null)
            {
                return;
            }

            Transform sampleTransform = sample.transform;
            sampleTransform.SetParent(transform, false);
            sampleTransform.localPosition = Vector3.zero;

            Vector3 baseScale = GetBackgroundPrefabBaseLocalScale();
            float scaleMultiplier = GetSegmentVisualScaleMultiplier();
            sampleTransform.localScale = new Vector3(
                baseScale.x * scaleMultiplier,
                baseScale.y * scaleMultiplier,
                baseScale.z);

            if (TryGetSegmentBounds(sampleTransform, out Bounds bounds))
            {
                segmentHeight = Mathf.Max(0.01f, bounds.size.y);
                segmentBottomOffset = Mathf.Max(0.01f, sampleTransform.position.y - bounds.min.y);
                segmentCenterOffsetX = sampleTransform.position.x - bounds.center.x;
                segmentStep = Mathf.Max(0.01f, segmentHeight - Mathf.Max(0f, segmentOverlap));
            }

            DestroyForLayoutRebuild(sample);
        }

        private void StackSegments()
        {
            if (!AreAllGroupsPopulated() || segmentStep <= 0f)
            {
                return;
            }

            float baseBottom = transform.position.y - segmentHeight * 0.5f;
            if (alignTopSegmentToFollowTarget)
            {
                Transform anchor = followTarget != null ? followTarget : (targetCamera != null ? targetCamera.transform : null);
                if (anchor != null)
                {
                    baseBottom = anchor.position.y + followTargetOffsetY - segmentHeight * 0.5f;
                }
            }

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                LayerGroupRuntime runtime = groups[groupIndex];
                float rootX = runtime.Root != null ? runtime.Root.position.x : transform.position.x;

                for (int segmentIndex = 0; segmentIndex < runtime.Segments.Count; segmentIndex++)
                {
                    Transform segment = runtime.Segments[segmentIndex];
                    Vector3 position = segment.position;
                    position.x = rootX + segmentCenterOffsetX;
                    float targetBottom = baseBottom - segmentStep * segmentIndex;
                    position.y = targetBottom + segmentBottomOffset;
                    segment.position = position;
                }
            }
        }

        private void CaptureFollowTargetOffset()
        {
            if (!alignTopSegmentToFollowTarget)
            {
                hasCapturedFollowTargetOffset = false;
                followTargetOffsetY = 0f;
                return;
            }

            Transform anchor = followTarget != null ? followTarget : (targetCamera != null ? targetCamera.transform : null);
            if (anchor == null)
            {
                return;
            }

            if (!Application.isPlaying || !hasCapturedFollowTargetOffset)
            {
                followTargetOffsetY = transform.position.y - anchor.position.y;
                hasCapturedFollowTargetOffset = true;
            }
        }

        private void RecycleGroup(LayerGroupRuntime runtime, float cameraTop, float cameraBottom)
        {
            for (int iteration = 0; iteration < runtime.Segments.Count; iteration++)
            {
                if (!TryGetLowestAndHighestSegments(runtime.Segments, out Transform lowestSegment, out Transform highestSegment))
                {
                    break;
                }

                if (ReferenceEquals(lowestSegment, highestSegment) ||
                    GetSegmentBottom(highestSegment) <= cameraTop + Mathf.Max(0f, recycleBuffer))
                {
                    break;
                }

                float targetBottom = GetSegmentBottom(lowestSegment) - segmentStep;
                SetSegmentBottom(highestSegment, targetBottom);
            }

            for (int iteration = 0; iteration < runtime.Segments.Count; iteration++)
            {
                if (!TryGetLowestAndHighestSegments(runtime.Segments, out Transform lowestSegment, out Transform highestSegment))
                {
                    break;
                }

                if (ReferenceEquals(lowestSegment, highestSegment) ||
                    GetSegmentTop(lowestSegment) >= cameraBottom - Mathf.Max(0f, recycleBuffer))
                {
                    break;
                }

                float targetBottom = GetSegmentBottom(highestSegment) + segmentStep;
                SetSegmentBottom(lowestSegment, targetBottom);
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

        private void DestroyForLayoutRebuild(Object targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                DestroyImmediate(targetObject);
                return;
            }

            DestroyImmediate(targetObject);
        }

        private static bool TryGetSegmentBounds(Transform segment, out Bounds bounds)
        {
            Renderer[] renderers = segment.GetComponentsInChildren<Renderer>(true);
            bool foundRenderer = false;
            bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer childRenderer = renderers[i];
                if (childRenderer == null)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = childRenderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
            }

            return foundRenderer;
        }

        private bool HasSegmentVisuals(Transform segment)
        {
            if (segment == null)
            {
                return false;
            }

            Renderer[] renderers = segment.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void AlignWallsToViewport()
        {
            if (!fitWallsToViewport || !TryGetWallBounds(out float leftWorldX, out float rightWorldX))
            {
                return;
            }

            LayerGroupRuntime wallGroup = GetGroupByRootName("WallRoot");
            if (wallGroup == null)
            {
                return;
            }

            for (int i = 0; i < wallGroup.Segments.Count; i++)
            {
                AlignWallSegmentToBounds(wallGroup.Segments[i], leftWorldX, rightWorldX);
            }
        }

        private bool TryGetWallBounds(out float leftWorldX, out float rightWorldX)
        {
            if (hasWallBoundsOverride)
            {
                leftWorldX = wallLeftBoundWorldX;
                rightWorldX = wallRightBoundWorldX;
                return rightWorldX > leftWorldX;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null || !targetCamera.orthographic)
            {
                leftWorldX = 0f;
                rightWorldX = 0f;
                return false;
            }

            float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            if (halfWidth <= 0f)
            {
                leftWorldX = 0f;
                rightWorldX = 0f;
                return false;
            }

            float inset = Mathf.Max(0f, wallViewportInset);
            leftWorldX = targetCamera.transform.position.x - halfWidth + inset;
            rightWorldX = targetCamera.transform.position.x + halfWidth - inset;
            return rightWorldX > leftWorldX;
        }

        private LayerGroupRuntime GetGroupByRootName(string rootName)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Definition.RootName == rootName)
                {
                    return groups[i];
                }
            }

            return null;
        }

        private static void AlignWallSegmentToBounds(Transform segment, float leftWorldX, float rightWorldX)
        {
            if (segment == null)
            {
                return;
            }

            Transform leftWall = segment.Find("LeftWall");
            Transform rightWall = segment.Find("RightWall");

            if (leftWall != null)
            {
                SetWallWorldX(leftWall, leftWorldX, true);
            }

            if (rightWall != null)
            {
                SetWallWorldX(rightWall, rightWorldX, false);
            }
        }

        private static void SetWallWorldX(Transform wall, float innerEdgeWorldX, bool isLeftWall)
        {
            if (wall == null)
            {
                return;
            }

            float offsetToInnerEdge = wall.position.x - GetInnerEdgeWorldX(wall, isLeftWall);
            Vector3 position = wall.position;
            position.x = innerEdgeWorldX + offsetToInnerEdge;
            wall.position = position;
        }

        private static float GetInnerEdgeWorldX(Transform wall, bool isLeftWall)
        {
            if (wall == null)
            {
                return 0f;
            }

            Collider2D collider = wall.GetComponent<Collider2D>();
            if (collider != null)
            {
                return isLeftWall ? collider.bounds.max.x : collider.bounds.min.x;
            }

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                return isLeftWall ? renderer.bounds.max.x : renderer.bounds.min.x;
            }

            return wall.position.x;
        }

        private float GetSegmentBottom(Transform segment)
        {
            return segment.position.y - segmentBottomOffset;
        }

        private float GetSegmentTop(Transform segment)
        {
            return GetSegmentBottom(segment) + segmentHeight;
        }

        private bool TryGetLowestAndHighestSegments(List<Transform> runtimeSegments, out Transform lowestSegment, out Transform highestSegment)
        {
            lowestSegment = null;
            highestSegment = null;

            float lowestY = float.PositiveInfinity;
            float highestY = float.NegativeInfinity;

            for (int i = 0; i < runtimeSegments.Count; i++)
            {
                Transform segment = runtimeSegments[i];
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

        private void SetSegmentBottom(Transform segment, float targetBottom)
        {
            Vector3 position = segment.position;
            position.y = targetBottom + segmentBottomOffset;
            segment.position = position;
        }
    }
}
