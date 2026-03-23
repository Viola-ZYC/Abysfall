using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class AbilityManager : MonoBehaviour
    {
        [SerializeField] private AbilityDefinition[] abilityPool;
        [SerializeField] private RunnerController runner;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField, Min(1)] private int choicesPerRoll = 3;
        [SerializeField] private bool allowDuplicates = true;

        [Serializable]
        private struct AbilityStack
        {
            public AbilityDefinition ability;
            public int stacks;
        }

        [SerializeField] private List<AbilityStack> acquired = new();

        private readonly Dictionary<AbilityDefinition, int> acquiredLookup = new();
        private AbilityContext context;
        private AbilityDefinition currentAbility;
        private float nextActiveTime;

        public enum AbilityChangeType
        {
            Acquired = 0,
            Replaced = 1
        }

        public event Action<AbilityDefinition, int> AbilityAcquired;
        public event Action<IReadOnlyList<AbilityDefinition>> AbilityChoicesRolled;
        public event Action<AbilityDefinition> AbilityReplaced;
        public event Action<AbilityDefinition, AbilityChangeType, int> AbilityChanged;

        public AbilityDefinition CurrentAbility => currentAbility;

        private void Awake()
        {
            ResolveReferences();
            RebuildLookup();
            BuildContext();
        }

        private void OnValidate()
        {
            if (choicesPerRoll < 1)
            {
                choicesPerRoll = 1;
            }
        }

        public void ResetRun()
        {
            RemoveAllAbilities();
            acquired.Clear();
            acquiredLookup.Clear();
            currentAbility = null;
            nextActiveTime = 0f;
        }

        public IReadOnlyList<AbilityDefinition> RollChoices()
        {
            return RollChoices(choicesPerRoll);
        }

        public bool ChooseAbility(AbilityDefinition ability)
        {
            return AcquireAbility(ability);
        }

        public IReadOnlyList<AbilityDefinition> RollChoices(int count)
        {
            List<AbilityDefinition> candidates = GetCandidates();
            List<AbilityDefinition> results = new();
            if (candidates.Count == 0 || count <= 0)
            {
                AbilityChoicesRolled?.Invoke(results);
                return results;
            }

            int pickCount = Mathf.Min(count, candidates.Count);
            for (int i = 0; i < pickCount; i++)
            {
                AbilityDefinition picked = PickWeighted(candidates);
                if (picked == null)
                {
                    break;
                }

                results.Add(picked);
                candidates.Remove(picked);
            }

            AbilityChoicesRolled?.Invoke(results);
            return results;
        }

        public AbilityDefinition RollRandomAbility()
        {
            List<AbilityDefinition> candidates = GetCandidates();
            if (candidates.Count == 0)
            {
                return null;
            }

            return PickWeighted(candidates);
        }

        public bool GrantRandomAbility()
        {
            AbilityDefinition ability = RollRandomAbility();
            return AcquireAbility(ability);
        }

        public bool AcquireAbility(AbilityDefinition ability)
        {
            return AcquireAbilityInternal(ability, notifyChange: true);
        }

        public bool ReplaceAbility(AbilityDefinition ability)
        {
            if (ability == null)
            {
                return false;
            }

            RemoveAllAbilities();
            acquired.Clear();
            acquiredLookup.Clear();
            currentAbility = null;

            bool acquiredAbility = AcquireAbilityInternal(ability, notifyChange: false);
            if (!acquiredAbility)
            {
                return false;
            }

            currentAbility = ability;
            nextActiveTime = 0f;
            AbilityReplaced?.Invoke(ability);
            AbilityChanged?.Invoke(ability, AbilityChangeType.Replaced, GetStacks(ability));
            return true;
        }

        public bool TryActivateCurrentAbility()
        {
            if (currentAbility == null)
            {
                return false;
            }

            if (currentAbility.isPassive || currentAbility.activeEffect == null)
            {
                return false;
            }

            if (Time.time < nextActiveTime)
            {
                return false;
            }

            EnsureContext();
            bool activated = currentAbility.activeEffect.Activate(context);
            if (activated)
            {
                float cooldown = Mathf.Max(0f, currentAbility.activeCooldown);
                nextActiveTime = cooldown > 0f ? Time.time + cooldown : Time.time;
            }

            return activated;
        }

        public int GetStacks(AbilityDefinition ability)
        {
            if (ability == null)
            {
                return 0;
            }

            return acquiredLookup.TryGetValue(ability, out int stacks) ? stacks : 0;
        }

        public bool HasAbility(AbilityDefinition ability)
        {
            return GetStacks(ability) > 0;
        }

        /// <summary>
        /// Runtime hook used by character switch workflow.
        /// </summary>
        public void SetRunner(RunnerController newRunner)
        {
            runner = newRunner;
            EnsureContext();
        }

        private void ApplyEffects(AbilityDefinition ability, int stacks)
        {
            if (ability == null || stacks <= 0)
            {
                return;
            }

            AbilityEffect[] effects = ability.effects;
            if (effects == null)
            {
                return;
            }

            EnsureContext();
            foreach (AbilityEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Apply(context, stacks);
            }
        }

        private void RemoveEffects(AbilityDefinition ability, int stacks)
        {
            if (ability == null || stacks <= 0)
            {
                return;
            }

            AbilityEffect[] effects = ability.effects;
            if (effects == null)
            {
                return;
            }

            EnsureContext();
            foreach (AbilityEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Remove(context, stacks);
            }
        }

        private void RemoveAllAbilities()
        {
            EnsureContext();
            foreach (AbilityStack entry in acquired)
            {
                RemoveEffects(entry.ability, entry.stacks);
            }
        }

        private bool AcquireAbilityInternal(AbilityDefinition ability, bool notifyChange)
        {
            if (ability == null)
            {
                return false;
            }

            int currentStacks = GetStacks(ability);
            int maxStacks = Mathf.Max(1, ability.maxStacks);
            if (currentStacks >= maxStacks)
            {
                return false;
            }

            ApplyEffects(ability, 1);
            int newStacks = currentStacks + 1;
            SetStacks(ability, newStacks);
            AbilityAcquired?.Invoke(ability, newStacks);
            if (notifyChange)
            {
                AbilityChanged?.Invoke(ability, AbilityChangeType.Acquired, newStacks);
            }
            return true;
        }

        private void SetStacks(AbilityDefinition ability, int stacks)
        {
            if (ability == null)
            {
                return;
            }

            if (stacks <= 0)
            {
                acquiredLookup.Remove(ability);
                for (int i = acquired.Count - 1; i >= 0; i--)
                {
                    if (acquired[i].ability == ability)
                    {
                        acquired.RemoveAt(i);
                        break;
                    }
                }
                return;
            }

            acquiredLookup[ability] = stacks;
            for (int i = 0; i < acquired.Count; i++)
            {
                if (acquired[i].ability == ability)
                {
                    AbilityStack updated = acquired[i];
                    updated.stacks = stacks;
                    acquired[i] = updated;
                    return;
                }
            }

            acquired.Add(new AbilityStack { ability = ability, stacks = stacks });
        }

        private List<AbilityDefinition> GetCandidates()
        {
            List<AbilityDefinition> candidates = new();
            if (abilityPool == null || abilityPool.Length == 0)
            {
                return candidates;
            }

            foreach (AbilityDefinition ability in abilityPool)
            {
                if (ability == null)
                {
                    continue;
                }

                int stacks = GetStacks(ability);
                int maxStacks = Mathf.Max(1, ability.maxStacks);

                if (!allowDuplicates && stacks > 0)
                {
                    continue;
                }

                if (stacks >= maxStacks)
                {
                    continue;
                }

                candidates.Add(ability);
            }

            return candidates;
        }

        private AbilityDefinition PickWeighted(List<AbilityDefinition> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            foreach (AbilityDefinition ability in candidates)
            {
                if (ability == null)
                {
                    continue;
                }

                totalWeight += ability.GetWeight();
            }

            if (totalWeight <= 0f)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            foreach (AbilityDefinition ability in candidates)
            {
                if (ability == null)
                {
                    continue;
                }

                cumulative += ability.GetWeight();
                if (roll <= cumulative)
                {
                    return ability;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private void RebuildLookup()
        {
            acquiredLookup.Clear();
            foreach (AbilityStack entry in acquired)
            {
                if (entry.ability == null || entry.stacks <= 0)
                {
                    continue;
                }

                acquiredLookup[entry.ability] = entry.stacks;
            }
        }

        private void BuildContext()
        {
            context = new AbilityContext(runner, scoreManager, gameManager);
        }

        private void EnsureContext()
        {
            if (context == null || context.Runner != runner || context.Score != scoreManager || context.Game != gameManager)
            {
                BuildContext();
            }
        }

        private void ResolveReferences()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<RunnerController>();
            }

            if (scoreManager == null)
            {
                scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
            }

            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            }
        }
    }
}
