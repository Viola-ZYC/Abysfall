using UnityEngine;

namespace EndlessRunner
{
    [RequireComponent(typeof(Collider2D))]
    public class MovingObstacle : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0f)] private float moveSpeed = 1.8f;
        [SerializeField] private bool useRunnerBounds = true;
        [SerializeField] private float fallbackMinX = -3f;
        [SerializeField] private float fallbackMaxX = 3f;
        [SerializeField, Min(0f)] private float boundaryPadding = 0f;

        private RunnerController runner;
        private Collider2D obstacleCollider;
        private int horizontalDirection;
        private bool stopped;

        private void Awake()
        {
            obstacleCollider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            ResetMovement();
        }

        private void Update()
        {
            if (stopped || moveSpeed <= 0f)
            {
                return;
            }

            float minX;
            float maxX;
            GetHorizontalBounds(out minX, out maxX);

            float halfWidth = GetHalfWidth();
            float padding = Mathf.Max(0f, boundaryPadding);
            float leftLimit = minX + halfWidth + padding;
            float rightLimit = maxX - halfWidth - padding;
            if (leftLimit > rightLimit)
            {
                float center = (minX + maxX) * 0.5f;
                leftLimit = center;
                rightLimit = center;
            }

            Vector3 position = transform.position;
            float nextX = position.x + horizontalDirection * moveSpeed * Time.deltaTime;

            if (horizontalDirection < 0 && nextX <= leftLimit)
            {
                nextX = leftLimit;
                stopped = true;
            }
            else if (horizontalDirection > 0 && nextX >= rightLimit)
            {
                nextX = rightLimit;
                stopped = true;
            }

            transform.position = new Vector3(nextX, position.y, position.z);
        }

        public void OnSpawned()
        {
            ResetMovement();
        }

        public void OnDespawned()
        {
            stopped = true;
        }

        private void ResetMovement()
        {
            ResolveRunner();
            horizontalDirection = Random.value < 0.5f ? -1 : 1;
            stopped = false;
        }

        private void ResolveRunner()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }
        }

        private void GetHorizontalBounds(out float minX, out float maxX)
        {
            ResolveRunner();
            if (useRunnerBounds && runner != null)
            {
                runner.GetHorizontalBounds(out minX, out maxX);
                return;
            }

            minX = fallbackMinX;
            maxX = fallbackMaxX;
            if (minX > maxX)
            {
                (minX, maxX) = (maxX, minX);
            }
        }

        private float GetHalfWidth()
        {
            if (obstacleCollider == null)
            {
                obstacleCollider = GetComponent<Collider2D>();
            }

            return obstacleCollider != null ? obstacleCollider.bounds.extents.x : 0f;
        }
    }
}
