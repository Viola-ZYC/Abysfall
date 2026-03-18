using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Abilities/Active/Air Jump")]
    public class AirJumpActiveEffect : ActiveAbilityEffect
    {
        public string chargeId = "air_jump_charge_100m";
        [Min(0f)] public float jumpImpulse = 8f;
        public bool resetDownwardVelocity = true;

        public override bool Activate(AbilityContext context)
        {
            if (context == null || context.Runner == null)
            {
                return false;
            }

            if (context.Game != null && context.Game.State != GameState.Running)
            {
                return false;
            }

            ScoreChargeTracker tracker = FindTracker(context.Runner, chargeId);
            if (tracker == null || tracker.Charges <= 0)
            {
                return false;
            }

            Rigidbody2D body = context.Runner.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                return false;
            }

            if (!tracker.TryConsume())
            {
                return false;
            }

            Vector2 velocity = body.linearVelocity;
            if (resetDownwardVelocity && velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            body.linearVelocity = velocity;
            body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            return true;
        }

        private static ScoreChargeTracker FindTracker(RunnerController runner, string id)
        {
            if (runner == null)
            {
                return null;
            }

            ScoreChargeTracker[] trackers = runner.GetComponents<ScoreChargeTracker>();
            foreach (ScoreChargeTracker tracker in trackers)
            {
                if (tracker != null && tracker.ChargeId == id)
                {
                    return tracker;
                }
            }

            return null;
        }
    }
}
