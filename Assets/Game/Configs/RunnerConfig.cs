using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Runner Config")]
    public class RunnerConfig : ScriptableObject
    {
        [Min(1)] public int maxHealth = 3;
        public float horizontalSpeed = 6f;
        public float baseGravityScale = 2f;
        public float gravityIncreasePerSecond = 0.12f;
        public float maxGravityScale = 8f;
        public float maxFallSpeed = 0f;
        public float brakeUpwardImpulse = 8f;
        public bool resetVerticalVelocity = true;
        public float hitStopDuration = 0.04f;
        [Range(0.1f, 1f)] public float obstacleSlowMultiplier = 0.4f;
        [Range(0.05f, 2f)] public float obstacleSlowDuration = 0.5f;
        [Range(0.1f, 1f)] public float obstacleVelocityDamp = 0.5f;
    }
}
