using UnityEngine;

namespace EndlessRunner
{
    public class SpecialCreature : Enemy
    {
        [SerializeField] private AbilityDefinition ability;
        [SerializeField] private AbilityManager abilityManager;

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

            abilityManager?.ReplaceAbility(ability);
            base.OnHitByAttack();
        }
    }
}
