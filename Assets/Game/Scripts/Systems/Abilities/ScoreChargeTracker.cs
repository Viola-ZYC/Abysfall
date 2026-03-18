using UnityEngine;

namespace EndlessRunner
{
    public class ScoreChargeTracker : MonoBehaviour
    {
        [SerializeField] private string chargeId = "charge";
        [SerializeField, Min(1)] private int scoreInterval = 100;
        [SerializeField, Min(0)] private int maxCharges = 0;
        [SerializeField] private int charges;

        private ScoreManager scoreManager;
        private int nextScore;
        private int lastScore;
        private bool subscribed;

        public string ChargeId => chargeId;
        public int Charges => charges;

        public void Configure(string id, ScoreManager manager, int interval, int maxChargeCount, bool resetProgress)
        {
            chargeId = id ?? string.Empty;
            scoreInterval = Mathf.Max(1, interval);
            maxCharges = Mathf.Max(0, maxChargeCount);
            if (resetProgress)
            {
                charges = 0;
            }

            AttachScoreManager(manager, resetProgress);
        }

        public bool TryConsume(int amount = 1)
        {
            int consume = Mathf.Max(1, amount);
            if (charges < consume)
            {
                return false;
            }

            charges -= consume;
            return true;
        }

        public void AddCharges(int amount)
        {
            int add = Mathf.Max(0, amount);
            if (add <= 0)
            {
                return;
            }

            if (maxCharges > 0)
            {
                charges = Mathf.Clamp(charges + add, 0, maxCharges);
            }
            else
            {
                charges += add;
            }
        }

        private void OnDisable()
        {
            Detach();
        }

        private void AttachScoreManager(ScoreManager manager, bool resetProgress)
        {
            if (scoreManager == manager && subscribed)
            {
                if (resetProgress)
                {
                    ResetProgress(scoreManager != null ? scoreManager.Score : 0);
                }

                return;
            }

            Detach();
            scoreManager = manager;
            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += OnScoreChanged;
                subscribed = true;
                ResetProgress(scoreManager.Score);
            }
            else if (resetProgress)
            {
                ResetProgress(0);
            }
        }

        private void Detach()
        {
            if (scoreManager != null && subscribed)
            {
                scoreManager.ScoreChanged -= OnScoreChanged;
            }

            subscribed = false;
            scoreManager = null;
        }

        private void ResetProgress(int score)
        {
            lastScore = score;
            if (scoreInterval <= 0)
            {
                nextScore = 0;
                return;
            }

            int bucket = score / scoreInterval;
            nextScore = (bucket + 1) * scoreInterval;
        }

        private void OnScoreChanged(int score)
        {
            if (scoreInterval <= 0)
            {
                return;
            }

            if (score < lastScore)
            {
                charges = 0;
                ResetProgress(score);
            }

            lastScore = score;
            while (score >= nextScore)
            {
                AddCharges(1);
                nextScore += scoreInterval;
            }
        }
    }
}
