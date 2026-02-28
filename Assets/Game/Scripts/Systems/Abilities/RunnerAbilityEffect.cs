using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Abilities/Runner Modifier")]
    public class RunnerAbilityEffect : AbilityEffect
    {
        public RunnerAbilityModifiers modifiers = RunnerAbilityModifiers.Default();

        public override void Apply(AbilityContext context, int stacks)
        {
            if (context == null || context.Runner == null || stacks <= 0)
            {
                return;
            }

            RunnerAbilityModifiers scaled = ScaleModifiers(stacks);
            context.Runner.AddAbilityModifiers(scaled);
        }

        public override void Remove(AbilityContext context, int stacks)
        {
            if (context == null || context.Runner == null || stacks <= 0)
            {
                return;
            }

            RunnerAbilityModifiers scaled = ScaleModifiers(stacks);
            context.Runner.RemoveAbilityModifiers(scaled);
        }

        private RunnerAbilityModifiers ScaleModifiers(int stacks)
        {
            RunnerAbilityModifiers scaled = modifiers;
            scaled.speedMultiplier = Mathf.Pow(Mathf.Max(0.1f, modifiers.speedMultiplier), stacks);
            scaled.gravityMultiplier = Mathf.Pow(Mathf.Max(0.1f, modifiers.gravityMultiplier), stacks);
            scaled.maxHealthBonus = modifiers.maxHealthBonus * stacks;
            scaled.brakeImpulseBonus = modifiers.brakeImpulseBonus * stacks;
            scaled.hitStopBonus = modifiers.hitStopBonus * stacks;
            return scaled;
        }
    }
}
