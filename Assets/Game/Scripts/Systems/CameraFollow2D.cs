using UnityEngine;

namespace EndlessRunner
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
        [SerializeField] private bool useInitialOffset = true;
        [SerializeField] private bool followX = false;
        [SerializeField] private bool followY = true;
        [SerializeField] private bool onlyFollowDown = true;
        [SerializeField, Min(0f)] private float smoothTime = 0f;

        [Header("Screen Side Boundaries")]
        [SerializeField] private bool autoFitSideWalls = true;
        [SerializeField] private bool useSafeArea = true;
        [SerializeField] private Transform leftWall;
        [SerializeField] private Transform rightWall;
        [SerializeField] private Transform gameplayBackground;
        [SerializeField] private RunnerController runner;
        [SerializeField] private bool resizeWallsVertically = true;
        [SerializeField] private bool autoFitBackgroundToBounds = true;
        [SerializeField, Min(0f)] private float wallVerticalPadding = 2f;
        [SerializeField, Min(0f)] private float backgroundVerticalPadding = 2f;
        [SerializeField, Min(0f)] private float horizontalInset = 0f;

        [Header("Portrait Resolution Adaptation")]
        [SerializeField] private bool adaptOrthographicSizeForAspect = true;
        [SerializeField] private Vector2 referenceResolution = new(1080f, 1920f);
        [SerializeField, Min(0.01f)] private float minSupportedAspect = 9f / 21f;
        [SerializeField, Min(0.01f)] private float maxSupportedAspect = 9f / 16f;
        [SerializeField, Min(0f)] private float minOrthographicScale = 0.85f;
        [SerializeField, Min(0f)] private float maxOrthographicScale = 1.35f;
        [SerializeField, Min(0f)] private float referenceOrthographicSizeOverride = 0f;

        private Vector3 velocity;
        private Camera targetCamera;
        private Vector2Int cachedScreenSize = Vector2Int.zero;
        private Rect cachedSafeArea = Rect.zero;
        private float cachedOrthoSize = -1f;
        private float cachedAspect = -1f;
        private float referenceOrthographicSize = 5f;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            InitializeReferenceOrthographicSize();
            ApplyAdaptiveOrthographicSizeIfNeeded(force: true);
            CacheBoundaryReferences();
            UpdateSideBoundariesIfNeeded();

            if (target != null && useInitialOffset)
            {
                offset = transform.position - target.position;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ApplyAdaptiveOrthographicSizeIfNeeded();

            Vector3 desired = target.position + offset;
            Vector3 current = transform.position;

            if (!followX)
            {
                desired.x = current.x;
            }

            if (!followY)
            {
                desired.y = current.y;
            }
            else if (onlyFollowDown)
            {
                desired.y = Mathf.Min(current.y, desired.y);
            }

            desired.z = offset.z;

            if (smoothTime > 0f)
            {
                transform.position = Vector3.SmoothDamp(current, desired, ref velocity, smoothTime);
            }
            else
            {
                transform.position = desired;
            }

            UpdateSideBoundariesIfNeeded();
        }

        private void InitializeReferenceOrthographicSize()
        {
            if (targetCamera == null)
            {
                return;
            }

            referenceOrthographicSize = referenceOrthographicSizeOverride > 0f
                ? referenceOrthographicSizeOverride
                : targetCamera.orthographicSize;

            if (referenceOrthographicSize <= 0f)
            {
                referenceOrthographicSize = 5f;
            }
        }

        private void ApplyAdaptiveOrthographicSizeIfNeeded(bool force = false)
        {
            if (!adaptOrthographicSizeForAspect || targetCamera == null || !targetCamera.orthographic)
            {
                return;
            }

            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            float referenceAspect = referenceResolution.x / Mathf.Max(1f, referenceResolution.y);
            if (referenceAspect <= 0f)
            {
                return;
            }

            float aspect = (float)screenSize.x / screenSize.y;
            float minAspect = Mathf.Min(minSupportedAspect, maxSupportedAspect);
            float maxAspect = Mathf.Max(minSupportedAspect, maxSupportedAspect);
            float clampedAspect = Mathf.Clamp(aspect, minAspect, maxAspect);

            float targetOrtho = referenceOrthographicSize * (referenceAspect / clampedAspect);
            float minOrtho = minOrthographicScale > 0f ? referenceOrthographicSize * minOrthographicScale : 0f;
            float maxOrtho = maxOrthographicScale > 0f ? referenceOrthographicSize * maxOrthographicScale : float.MaxValue;
            if (minOrtho > maxOrtho)
            {
                (minOrtho, maxOrtho) = (maxOrtho, minOrtho);
            }

            targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);
            targetOrtho = Mathf.Max(0.01f, targetOrtho);

            if (force || !Mathf.Approximately(targetCamera.orthographicSize, targetOrtho))
            {
                targetCamera.orthographicSize = targetOrtho;
                cachedOrthoSize = -1f;
            }
        }

        private void CacheBoundaryReferences()
        {
            if (leftWall == null)
            {
                Transform candidate = transform.Find("LeftWall");
                if (candidate != null)
                {
                    leftWall = candidate;
                }
            }

            if (rightWall == null)
            {
                Transform candidate = transform.Find("RightWall");
                if (candidate != null)
                {
                    rightWall = candidate;
                }
            }

            if (gameplayBackground == null)
            {
                Transform candidate = transform.Find("GameplayBackground");
                if (candidate != null)
                {
                    gameplayBackground = candidate;
                }
            }

            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }
        }

        private void UpdateSideBoundariesIfNeeded()
        {
            if (!autoFitSideWalls)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                if (targetCamera == null)
                {
                    return;
                }
            }

            if (!targetCamera.orthographic)
            {
                return;
            }

            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            Rect safeArea = useSafeArea ? Screen.safeArea : new Rect(0f, 0f, screenSize.x, screenSize.y);
            bool geometryChanged = screenSize != cachedScreenSize ||
                                   safeArea != cachedSafeArea ||
                                   !Mathf.Approximately(cachedOrthoSize, targetCamera.orthographicSize) ||
                                   !Mathf.Approximately(cachedAspect, targetCamera.aspect);

            if (!geometryChanged)
            {
                return;
            }

            float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            if (halfWidth <= 0f)
            {
                return;
            }

            float normalizedLeft = Mathf.Clamp01(safeArea.xMin / Mathf.Max(1f, screenSize.x));
            float normalizedRight = Mathf.Clamp01(safeArea.xMax / Mathf.Max(1f, screenSize.x));
            float leftEdge = Mathf.Lerp(-halfWidth, halfWidth, normalizedLeft) + horizontalInset;
            float rightEdge = Mathf.Lerp(-halfWidth, halfWidth, normalizedRight) - horizontalInset;

            if (leftEdge >= rightEdge)
            {
                return;
            }

            PositionWall(leftWall, leftEdge, true);
            PositionWall(rightWall, rightEdge, false);
            if (runner != null)
            {
                runner.SetHorizontalBounds(leftEdge, rightEdge);
            }

            FitBackgroundToBounds(leftEdge, rightEdge);

            cachedScreenSize = screenSize;
            cachedSafeArea = safeArea;
            cachedOrthoSize = targetCamera.orthographicSize;
            cachedAspect = targetCamera.aspect;
        }

        private void PositionWall(Transform wall, float edgeX, bool isLeftWall)
        {
            if (wall == null)
            {
                return;
            }

            float halfThickness = GetHalfThickness(wall);
            float worldX = transform.position.x + (isLeftWall ? edgeX - halfThickness : edgeX + halfThickness);

            Vector3 position = wall.position;
            position.x = worldX;
            wall.position = position;

            if (resizeWallsVertically)
            {
                Vector3 scale = wall.localScale;
                scale.y = targetCamera.orthographicSize * 2f + wallVerticalPadding;
                wall.localScale = scale;
            }
        }

        private void FitBackgroundToBounds(float leftEdge, float rightEdge)
        {
            if (!autoFitBackgroundToBounds || gameplayBackground == null || targetCamera == null)
            {
                return;
            }

            float halfHeight = targetCamera.orthographicSize;
            float centerX = transform.position.x + (leftEdge + rightEdge) * 0.5f;
            float centerY = transform.position.y;
            float width = Mathf.Max(0.01f, rightEdge - leftEdge);
            float height = Mathf.Max(0.01f, halfHeight * 2f + backgroundVerticalPadding);

            Vector3 position = gameplayBackground.position;
            position.x = centerX;
            position.y = centerY;
            gameplayBackground.position = position;

            Renderer renderer = gameplayBackground.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Vector2 currentSize = new Vector2(Mathf.Max(0.01f, renderer.bounds.size.x), Mathf.Max(0.01f, renderer.bounds.size.y));
            Vector3 scale = gameplayBackground.localScale;
            scale.x *= width / currentSize.x;
            scale.y *= height / currentSize.y;
            gameplayBackground.localScale = scale;
        }

        private static float GetHalfThickness(Transform wall)
        {
            if (wall == null)
            {
                return 0.25f;
            }

            Collider2D collider = wall.GetComponent<Collider2D>();
            if (collider != null)
            {
                return Mathf.Max(0.01f, collider.bounds.extents.x);
            }

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                return Mathf.Max(0.01f, renderer.bounds.extents.x);
            }

            return Mathf.Max(0.01f, Mathf.Abs(wall.lossyScale.x) * 0.5f);
        }

        /// <summary>
        /// Runtime hook: rebind camera follow target after character switch.
        /// </summary>
        public void SetTarget(Transform newTarget, RunnerController newRunner = null, bool recalculateOffset = false)
        {
            target = newTarget;
            if (newRunner != null)
            {
                runner = newRunner;
            }

            if (recalculateOffset && target != null)
            {
                offset = transform.position - target.position;
            }

            ApplyAdaptiveOrthographicSizeIfNeeded(force: true);
            UpdateSideBoundariesIfNeeded();
        }
    }
}
