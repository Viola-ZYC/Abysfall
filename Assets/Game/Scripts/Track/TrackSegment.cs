using UnityEngine;

namespace EndlessRunner
{
    public class TrackSegment : MonoBehaviour
    {
        [SerializeField] private Transform endPoint;
        [SerializeField] private float length = 20f;
        [SerializeField] private RunnerController runner;
        [SerializeField] private bool autoFitWidthToBounds = true;
        [SerializeField, Min(0f)] private float widthInset = 0f;

        private Renderer segmentRenderer;
        private Vector3 initialLocalScale;
        private float initialWorldWidth;

        public float Length => length;
        public Transform EndPoint => endPoint;
        public float EndY => endPoint != null ? endPoint.position.y : transform.position.y - length;

        private void Awake()
        {
            segmentRenderer = GetComponent<Renderer>();
            initialLocalScale = transform.localScale;
            initialWorldWidth = segmentRenderer != null ? segmentRenderer.bounds.size.x : 0f;
            ResolveRunner();
        }

        private void OnEnable()
        {
            ResolveRunner();
            FitWidthToBounds();
        }

        private void ResolveRunner()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }
        }

        private void FitWidthToBounds()
        {
            if (!autoFitWidthToBounds || runner == null || segmentRenderer == null || initialWorldWidth <= 0.0001f)
            {
                return;
            }

            runner.GetHorizontalBounds(out float minX, out float maxX);
            float targetWidth = Mathf.Max(0.01f, (maxX - minX) - Mathf.Max(0f, widthInset) * 2f);
            float xScale = initialLocalScale.x * (targetWidth / initialWorldWidth);

            Vector3 scale = transform.localScale;
            scale.x = xScale;
            transform.localScale = scale;
        }
    }
}
