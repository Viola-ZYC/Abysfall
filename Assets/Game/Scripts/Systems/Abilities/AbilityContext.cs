using UnityEngine;

namespace EndlessRunner
{
    public class AbilityContext
    {
        public RunnerController Runner { get; }
        public ScoreManager Score { get; }
        public GameManager Game { get; }

        public AbilityContext(RunnerController runner, ScoreManager score, GameManager game)
        {
            Runner = runner;
            Score = score;
            Game = game;
        }
    }
}
