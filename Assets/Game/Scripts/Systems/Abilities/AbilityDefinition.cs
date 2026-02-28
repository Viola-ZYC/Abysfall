using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Abilities/Ability")]
    public class AbilityDefinition : ScriptableObject
    {
        public string abilityId = "ability_id";
        public string displayName = "New Ability";
        [TextArea] public string description;
        public Sprite icon;
        [Min(1)] public int maxStacks = 1;
        [Min(0f)] public float weight = 1f;
        public AbilityEffect[] effects;

        public float GetWeight()
        {
            return Mathf.Max(0f, weight);
        }
    }
}
