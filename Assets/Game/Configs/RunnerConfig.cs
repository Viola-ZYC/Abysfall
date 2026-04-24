using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Runner Config")]
    public class RunnerConfig : ScriptableObject
    {
        [Min(1)] public int maxHealth = 3;
        public float horizontalSpeed = 6f;
        [Min(0f)] public float horizontalFollowResponsiveness = 12f;
        public float baseGravityScale = 2f;
        public float gravityIncreasePerSecond = 0.12f;
        public bool useScoreBasedGravity = true;
        [Min(0)] public int gravityScoreThreshold1 = 500;
        [Min(0)] public int gravityScoreThreshold2 = 2000;
        [Min(0f)] public float gravityIncreaseStage1 = 0.005f;
        [Min(0f)] public float gravityIncreaseStage2 = 0.01f;
        [Min(0f)] public float gravityIncreaseStage3 = 0.05f;
        public float maxGravityScale = 8f;
        public float maxFallSpeed = 0f;
        public float brakeUpwardImpulse = 8f;
        public bool resetVerticalVelocity = true;
        public float hitStopDuration = 0.04f;
        [Range(0.1f, 1f)] public float contactSlowMultiplier = 0.4f;
        [Range(0.05f, 2f)] public float contactSlowDuration = 0.5f;
        [Range(0.1f, 1f)] public float contactVelocityDamp = 0.5f;
    }
}
