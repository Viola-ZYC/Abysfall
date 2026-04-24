using System;
using UnityEngine;

namespace EndlessRunner
{
    public class SpecialCreature : CreatureBase
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
                TryShowCodexPopup(oncePerRun: true);
            }

            AbilityDefinition rewardAbility = ResolveRewardAbility();
            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }

            if (rewardMode == RewardMode.ReplaceAbility && rewardAbility != null)
            {
                abilityManager?.ReplaceAbility(rewardAbility);
            }
            base.OnHitByAttack();
        }

        public bool ReplacesAbilityOnHit()
        {
            if (rewardMode != RewardMode.ReplaceAbility)
            {
                return false;
            }

            return ResolveRewardAbility() != null;
        }

        public bool ProvidesGameplayReward()
        {
            if (!ReplacesAbilityOnHit())
            {
                return false;
            }

            return HasMeaningfulReward(ResolveRewardAbility());
        }

        public bool IsHazard()
        {
            return !ReplacesAbilityOnHit() && !HasCodexRewardAbility();
        }

        public bool HasCodexRewardAbility()
        {
            CodexEntry entry = GetCodexEntry();
            return entry != null && !string.IsNullOrWhiteSpace(entry.abilityId);
        }

        private AbilityDefinition ResolveRewardAbility()
        {
            if (ability != null)
            {
                return ability;
            }

            CodexEntry entry = GetCodexEntry();
            if (entry == null || string.IsNullOrWhiteSpace(entry.abilityId))
            {
                return null;
            }

            AbilityDefinition[] loadedAbilities = Resources.FindObjectsOfTypeAll<AbilityDefinition>();
            for (int i = 0; i < loadedAbilities.Length; i++)
            {
                AbilityDefinition candidate = loadedAbilities[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.abilityId, entry.abilityId, StringComparison.Ordinal))
                {
                    ability = candidate;
                    return candidate;
                }
            }

            return null;
        }

        private static bool HasMeaningfulReward(AbilityDefinition rewardAbility)
        {
            return rewardAbility != null &&
                   (rewardAbility.activeEffect != null ||
                    (rewardAbility.effects != null && rewardAbility.effects.Length > 0));
        }

        private void TryShowCodexPopup(bool oncePerRun)
        {
            AbilityAcquiredUI popup = FindAnyObjectByType<AbilityAcquiredUI>();
            if (popup == null)
            {
                return;
            }

            CodexEntry entry = GetCodexEntry();
            if (entry == null)
            {
                return;
            }

            if (oncePerRun)
            {
                popup.ShowCodexEntryOncePerRun(codexCategory, entry);
                return;
            }

            popup.ShowCodexEntry(codexCategory, entry);
        }

        private CodexEntry GetCodexEntry()
        {
            if (string.IsNullOrWhiteSpace(codexEntryId))
            {
                return null;
            }

            CodexDatabase database = CodexDatabase.Load();
            return database != null ? database.FindEntry(codexCategory, codexEntryId) : null;
        }
    }
}
