using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// 角色能力类型。后续扩展新能力时，在这里新增枚举并在 CharacterAbilityController 中实现即可。
    /// </summary>
    public enum CharacterAbilityType
    {
        None = 0,
        SingleAirJumpOnBlock = 1
    }

    /// <summary>
    /// 角色配置数据（推荐做成 ScriptableObject 资产，方便美术/策划改配置）。
    /// </summary>
    [CreateAssetMenu(menuName = "EndlessRunner/Character Definition", fileName = "CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string displayName = "Balanced Loadout";
        [TextArea] [SerializeField] private string description = "Standard ability setup.";

        [Header("Prefab")]
        [SerializeField] private RunnerController characterPrefab;

        [Header("Ability")]
        [SerializeField] private CharacterAbilityType abilityType = CharacterAbilityType.None;
        [SerializeField, Min(0f)] private float airJumpImpulse = 8f;
        [SerializeField, Min(1)] private int airJumpCharges = 1;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public RunnerController CharacterPrefab => characterPrefab;
        public CharacterAbilityType AbilityType => abilityType;
        public float AirJumpImpulse => airJumpImpulse;
        public int AirJumpCharges => Mathf.Max(1, airJumpCharges);
    }
}
