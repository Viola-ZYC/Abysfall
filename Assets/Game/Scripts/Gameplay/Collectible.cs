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
        [SerializeField] private CodexCategory codexCategory = CodexCategory.Collection;
        [SerializeField] private string codexEntryId = string.Empty;
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

            bool newlyUnlocked = false;
            if (collectibleType == CollectibleType.LoreRelic && !string.IsNullOrWhiteSpace(codexEntryId))
            {
                newlyUnlocked = RunProgressStore.UnlockCodexEntry(codexCategory, codexEntryId, 1);
            }

            if (value > 0)
            {
                ScoreManager.Instance?.AddCollectible(value);
            }

            if (newlyUnlocked)
            {
                TryShowCollectiblePopup();
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

        public void ConfigureLoreRelic(string entryId, int scoreValue = 0)
        {
            collectibleType = CollectibleType.LoreRelic;
            codexCategory = CodexCategory.Collection;
            codexEntryId = entryId ?? string.Empty;
            value = Mathf.Max(0, scoreValue);
            ApplyVisualStyle();
        }

        public void ConfigureScorePickup(int scoreValue = 1)
        {
            collectibleType = CollectibleType.ScorePickup;
            codexEntryId = string.Empty;
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

        private void TryShowCollectiblePopup()
        {
            if (string.IsNullOrWhiteSpace(codexEntryId))
            {
                return;
            }

            AbilityAcquiredUI popup = FindAnyObjectByType<AbilityAcquiredUI>();
            if (popup == null)
            {
                return;
            }

            CodexDatabase database = CodexDatabase.Load();
            CodexEntry entry = database != null ? database.FindEntry(codexCategory, codexEntryId) : null;
            if (entry == null)
            {
                return;
            }

            popup.ShowCodexEntry(codexCategory, entry);
        }
    }
}
