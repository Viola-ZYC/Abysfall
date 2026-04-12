using UnityEngine;

namespace EndlessRunner
{
    public class SpecialCreature : Enemy
    {
        public enum RewardMode
        {
            ReplaceAbility = 0,
            None = 1
        }

        [SerializeField] private AbilityDefinition ability;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private RewardMode rewardMode = RewardMode.ReplaceAbility;
        [SerializeField] private CodexCategory codexCategory = CodexCategory.Creature;
        [SerializeField] private string codexEntryId = string.Empty;
        [SerializeField] private bool unlockCodexOnHit = true;

        private bool consumed;

        public AbilityDefinition Ability => ability;
        public string CodexEntryId => codexEntryId;

        public override void OnSpawned()
        {
            base.OnSpawned();
            consumed = false;
        }

        public override void OnHitByAttack()
        {
            if (consumed)
            {
                return;
            }

            consumed = true;
            bool newlyUnlocked = false;
            if (unlockCodexOnHit && !string.IsNullOrWhiteSpace(codexEntryId))
            {
                newlyUnlocked = RunProgressStore.UnlockCodexEntry(codexCategory, codexEntryId, 1);
            }

            if (newlyUnlocked)
            {
                TryShowCodexPopup();
            }

            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }

            if (rewardMode == RewardMode.ReplaceAbility)
            {
                abilityManager?.ReplaceAbility(ability);
            }
            base.OnHitByAttack();
        }

        private void TryShowCodexPopup()
        {
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
