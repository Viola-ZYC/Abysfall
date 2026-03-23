using System;
using UnityEngine;

namespace EndlessRunner
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private Transform player;
        [SerializeField] private int pointsPerMeter = 1;
        [SerializeField] private int pointsPerCollectible = 10;

        public int Score { get; private set; }
        public event Action<int> ScoreChanged;

        private float startY;
        private int collectibleCount;
        private bool isRunning = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            isRunning = GameManager.Instance == null || GameManager.Instance.State == GameState.Running;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnGameStateChanged;
                isRunning = GameManager.Instance.State == GameState.Running;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!isRunning || player == null)
            {
                return;
            }

            RecalculateScore();
        }

        public void ResetScore()
        {
            collectibleCount = 0;
            startY = player != null ? player.position.y : 0f;
            Score = 0;
            ScoreChanged?.Invoke(Score);
        }

        public void AddCollectible(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            collectibleCount += amount;
            RecalculateScore();
        }

        /// <summary>
        /// Runtime hook: updates the player transform used for distance scoring.
        /// </summary>
        public void SetPlayer(Transform newPlayer, bool resetStartHeight = false)
        {
            player = newPlayer;
            if (resetStartHeight)
            {
                startY = player != null ? player.position.y : 0f;
            }
        }

        private void RecalculateScore()
        {
            if (player == null)
            {
                return;
            }

            float distance = Mathf.Max(0f, startY - player.position.y);
            int newScore = Mathf.FloorToInt(distance * pointsPerMeter) + collectibleCount * pointsPerCollectible;
            if (newScore != Score)
            {
                Score = newScore;
                ScoreChanged?.Invoke(Score);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            isRunning = state == GameState.Running;
        }
    }
}
