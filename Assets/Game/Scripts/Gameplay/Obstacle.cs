using UnityEngine;

namespace EndlessRunner
{
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] private string codexEntryId = string.Empty;
        [SerializeField] private bool unlockCodexOnSpawn = true;
        private bool consumed;

        public string CodexEntryId => codexEntryId;

        private void OnEnable()
        {
            consumed = false;
            if (unlockCodexOnSpawn && !string.IsNullOrWhiteSpace(codexEntryId))
            {
                RunProgressStore.UnlockCodexEntry(CodexCategory.Obstacle, codexEntryId, 1);
            }
        }

        public bool Consume()
        {
            if (consumed)
            {
                return false;
            }

            consumed = true;
            return true;
        }
    }
}
