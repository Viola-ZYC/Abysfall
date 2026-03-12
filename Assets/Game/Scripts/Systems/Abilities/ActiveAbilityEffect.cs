using UnityEngine;

namespace EndlessRunner
{
    public abstract class ActiveAbilityEffect : ScriptableObject
    {
        public abstract bool Activate(AbilityContext context);
    }
}
