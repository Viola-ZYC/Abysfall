using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Abilities/Score Charge Effect")]
    public class ScoreChargeAbilityEffect : AbilityEffect
    {
        public string chargeId = "charge";
        [Min(1)] public int scoreInterval = 100;
        [Min(0)] public int maxCharges = 0;
        public bool resetOnApply = true;

        public override void Apply(AbilityContext context, int stacks)
        {
            if (context == null || context.Runner == null)
            {
                return;
            }

            ScoreChargeTracker tracker = FindOrCreateTracker(context.Runner, chargeId);
            tracker.Configure(chargeId, context.Score, scoreInterval, maxCharges, resetOnApply);
        }

        public override void Remove(AbilityContext context, int stacks)
        {
            if (context == null || context.Runner == null)
            {
                return;
            }

            ScoreChargeTracker tracker = FindTracker(context.Runner, chargeId);
            if (tracker != null)
            {
                Destroy(tracker);
            }
        }

        private static ScoreChargeTracker FindOrCreateTracker(RunnerController runner, string id)
        {
            ScoreChargeTracker tracker = FindTracker(runner, id);
            if (tracker == null)
            {
                tracker = runner.gameObject.AddComponent<ScoreChargeTracker>();
            }

            return tracker;
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
