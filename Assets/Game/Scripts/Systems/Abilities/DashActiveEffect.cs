using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Abilities/Active/Dash")]
    public class DashActiveEffect : ActiveAbilityEffect
    {
        public string chargeId = "dash_charge_100m";
        [Min(0f)] public float dashImpulse = 6f;
        public bool resetHorizontalVelocity = true;
        public bool preferInputDirection = true;
        [Range(0f, 1f)] public float minInputThreshold = 0.1f;

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

            float direction = 0f;
            if (preferInputDirection)
            {
                InputRouter input = Object.FindAnyObjectByType<InputRouter>();
                if (input != null)
                {
                    float axis = input.Horizontal;
                    if (Mathf.Abs(axis) >= minInputThreshold)
                    {
                        direction = Mathf.Sign(axis);
                    }
                }
            }

            if (Mathf.Abs(direction) < 0.01f)
            {
                float vx = body.linearVelocity.x;
                if (Mathf.Abs(vx) >= 0.01f)
                {
                    direction = Mathf.Sign(vx);
                }
            }

            if (Mathf.Abs(direction) < 0.01f)
            {
                direction = 1f;
            }

            if (!tracker.TryConsume())
            {
                return false;
            }

            Vector2 velocity = body.linearVelocity;
            if (resetHorizontalVelocity)
            {
                velocity.x = 0f;
            }

            body.linearVelocity = velocity;
            body.AddForce(new Vector2(dashImpulse * direction, 0f), ForceMode2D.Impulse);
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
