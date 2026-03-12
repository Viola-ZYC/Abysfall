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

        private bool consumed;

        public AbilityDefinition Ability => ability;

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
    }
}
