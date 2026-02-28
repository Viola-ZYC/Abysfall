using UnityEngine;

namespace EndlessRunner
{
    public abstract class AbilityEffect : ScriptableObject
    {
        public abstract void Apply(AbilityContext context, int stacks);

        public virtual void Remove(AbilityContext context, int stacks)
        {
        }
    }
}
