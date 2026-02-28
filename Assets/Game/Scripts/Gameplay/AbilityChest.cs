using UnityEngine;

namespace EndlessRunner
{
    public class AbilityChest : MonoBehaviour
    {
        [SerializeField] private AbilitySelectionUI selectionUI;
        [SerializeField] private RunnerController runner;
        [SerializeField] private bool destroyAfterOpen = true;

        private bool consumed;

        private void OnEnable()
        {
            consumed = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed)
            {
                return;
            }

            RunnerController otherRunner = other.GetComponent<RunnerController>();
            if (otherRunner == null)
            {
                return;
            }

            if (runner == null)
            {
                runner = otherRunner;
            }

            runner.ResetVelocity();

            if (selectionUI == null)
            {
                selectionUI = FindAnyObjectByType<AbilitySelectionUI>();
            }

            if (selectionUI == null)
            {
                return;
            }

            if (!selectionUI.Open())
            {
                return;
            }

            consumed = true;
            if (destroyAfterOpen)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            if (ObjectPool.Instance != null && ObjectPool.Instance.IsPooled(gameObject))
            {
                ObjectPool.Instance.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
