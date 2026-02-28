using System;
using UnityEngine;

namespace EndlessRunner
{
    [Serializable]
    public struct RunnerAbilityModifiers
    {
        [Range(0.1f, 5f)] public float speedMultiplier;
        [Range(0.1f, 5f)] public float gravityMultiplier;
        public int maxHealthBonus;
        public float brakeImpulseBonus;
        public float hitStopBonus;

        public static RunnerAbilityModifiers Default()
        {
            return new RunnerAbilityModifiers
            {
                speedMultiplier = 1f,
                gravityMultiplier = 1f,
                maxHealthBonus = 0,
                brakeImpulseBonus = 0f,
                hitStopBonus = 0f
            };
        }
    }
}
