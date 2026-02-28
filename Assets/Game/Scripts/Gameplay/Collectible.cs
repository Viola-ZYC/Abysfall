using UnityEngine;

namespace EndlessRunner
{
    public class Collectible : MonoBehaviour
    {
        private enum CollectibleType
        {
            ScorePickup,
            LoreRelic
        }

        [SerializeField] private CollectibleType collectibleType = CollectibleType.ScorePickup;
        [SerializeField] private int value = 1;
        [SerializeField] private int collectionEntryId = -1;
        [SerializeField] private Color loreRelicColor = new Color(0.38f, 0.86f, 1f, 1f);
        [SerializeField] private Color scorePickupColor = new Color(1f, 0.95f, 0.3f, 1f);

        private SpriteRenderer cachedRenderer;

        private void Awake()
        {
            cachedRenderer = GetComponent<SpriteRenderer>();
            ApplyVisualStyle();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<RunnerController>() == null)
            {
                return;
            }

            if (collectibleType == CollectibleType.LoreRelic && collectionEntryId >= 0)
            {
                RunProgressStore.UnlockCollectionEntry(collectionEntryId);
            }

            if (value > 0)
            {
                ScoreManager.Instance?.AddCollectible(value);
            }

            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ConfigureLoreRelic(int entryId, int scoreValue = 0)
        {
            collectibleType = CollectibleType.LoreRelic;
            collectionEntryId = Mathf.Max(0, entryId);
            value = Mathf.Max(0, scoreValue);
            ApplyVisualStyle();
        }

        public void ConfigureScorePickup(int scoreValue = 1)
        {
            collectibleType = CollectibleType.ScorePickup;
            collectionEntryId = -1;
            value = Mathf.Max(0, scoreValue);
            ApplyVisualStyle();
        }

        private void ApplyVisualStyle()
        {
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<SpriteRenderer>();
            }

            if (cachedRenderer == null)
            {
                return;
            }

            cachedRenderer.color = collectibleType == CollectibleType.LoreRelic ? loreRelicColor : scorePickupColor;
        }
    }
}
