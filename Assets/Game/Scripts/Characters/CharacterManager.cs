using System;
using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// 角色总管理器：
    /// 1) 负责在游戏开局生成默认角色
    /// 2) 负责点击 UI 后切换角色（销毁旧角色、生成新角色）
    /// 3) 负责把新角色重绑定给 GameManager/Score/Track/Camera/HUD 等系统
    /// 4) 可在开局自动初始化新一轮游戏（重置分数、赛道、状态）
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        [Header("Character Data (能力/装备方案)")]
        [SerializeField] private CharacterDefinition[] characterDefinitions;
        [SerializeField] private int defaultCharacterIndex = 0;

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool destroyOldCharacterOnSwitch = true;
        [SerializeField] private bool initializeNewGameOnStart = true;

        [Header("Optional Runtime References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private HUDController hudController;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private AbilitySelectionUI abilitySelectionUI;
        [SerializeField] private ChestMilestoneSpawner chestMilestoneSpawner;

        private RunnerController currentRunner;
        private int currentCharacterIndex = -1;

        public RunnerController CurrentRunner => currentRunner;
        public int CurrentCharacterIndex => currentCharacterIndex;
        public int CharacterCount => characterDefinitions != null ? characterDefinitions.Length : 0;

        /// <summary>
        /// 参数：新角色索引、角色定义、新生成的 RunnerController。
        /// </summary>
        public event Action<int, CharacterDefinition, RunnerController> CharacterChanged;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (initializeNewGameOnStart)
            {
                InitializeNewGame();
            }
        }

        /// <summary>
        /// 开局初始化：切到默认角色 + 触发 BeginRun。
        /// </summary>
        public void InitializeNewGame()
        {
            if (!SelectCharacter(defaultCharacterIndex, forceRecreate: true))
            {
                return;
            }

            if (gameManager != null)
            {
                gameManager.BeginRun();
                return;
            }

            // 兼容无 GameManager 的最小场景。
            scoreManager?.ResetScore();
            trackManager?.ResetTrack();
        }

        /// <summary>
        /// 由 UI 调用，切换角色。
        /// </summary>
        public bool SelectCharacter(int index, bool forceRecreate = false)
        {
            if (!TryGetDefinition(index, out CharacterDefinition definition))
            {
                return false;
            }

            if (!forceRecreate && index == currentCharacterIndex && currentRunner != null)
            {
                CharacterChanged?.Invoke(currentCharacterIndex, definition, currentRunner);
                return true;
            }

            if (definition.CharacterPrefab == null)
            {
                Debug.LogError($"Character definition '{definition.name}' has no prefab assigned.", this);
                return false;
            }

            ResolveReferences();
            Vector3 spawnPosition = ResolveSpawnPosition();
            Quaternion spawnRotation = ResolveSpawnRotation();

            RunnerController oldRunner = currentRunner;
            RunnerController newRunner = Instantiate(definition.CharacterPrefab, spawnPosition, spawnRotation);
            if (newRunner == null)
            {
                Debug.LogError("Character instantiate failed.", this);
                return false;
            }

            // 给新角色挂载并配置能力执行器（无则自动加）。
            CharacterAbilityController abilityController = newRunner.GetComponent<CharacterAbilityController>();
            if (abilityController == null)
            {
                abilityController = newRunner.gameObject.AddComponent<CharacterAbilityController>();
            }
            abilityController.Configure(definition);

            currentRunner = newRunner;
            currentCharacterIndex = index;
            RebindRuntimeSystems(newRunner);

            if (destroyOldCharacterOnSwitch && oldRunner != null && oldRunner != newRunner)
            {
                Destroy(oldRunner.gameObject);
            }

            CharacterChanged?.Invoke(currentCharacterIndex, definition, currentRunner);
            return true;
        }

        public CharacterDefinition GetCharacterDefinition(int index)
        {
            return TryGetDefinition(index, out CharacterDefinition definition) ? definition : null;
        }

        private bool TryGetDefinition(int index, out CharacterDefinition definition)
        {
            definition = null;
            if (characterDefinitions == null || characterDefinitions.Length == 0)
            {
                Debug.LogError("CharacterManager: characterDefinitions is empty.", this);
                return false;
            }

            if (index < 0 || index >= characterDefinitions.Length)
            {
                Debug.LogWarning($"Character index out of range: {index}.", this);
                return false;
            }

            definition = characterDefinitions[index];
            if (definition == null)
            {
                Debug.LogError($"CharacterManager: character definition at index {index} is null.", this);
                return false;
            }

            return true;
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
            }

            if (scoreManager == null)
            {
                scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindAnyObjectByType<ScoreManager>();
            }

            if (trackManager == null)
            {
                trackManager = FindAnyObjectByType<TrackManager>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<CameraFollow2D>();
            }

            if (hudController == null)
            {
                hudController = FindAnyObjectByType<HUDController>();
            }

            if (abilityManager == null)
            {
                abilityManager = FindAnyObjectByType<AbilityManager>();
            }

            if (abilitySelectionUI == null)
            {
                abilitySelectionUI = FindAnyObjectByType<AbilitySelectionUI>();
            }

            if (chestMilestoneSpawner == null)
            {
                chestMilestoneSpawner = FindAnyObjectByType<ChestMilestoneSpawner>();
            }

            if (currentRunner == null)
            {
                currentRunner = FindAnyObjectByType<RunnerController>();
            }
        }

        private void RebindRuntimeSystems(RunnerController newRunner)
        {
            if (newRunner == null)
            {
                return;
            }

            Transform runnerTransform = newRunner.transform;

            gameManager?.SetRunner(newRunner);
            scoreManager?.SetPlayer(runnerTransform);
            trackManager?.SetPlayer(runnerTransform);
            cameraFollow?.SetTarget(runnerTransform, newRunner);
            hudController?.SetRunner(newRunner);
            abilityManager?.SetRunner(newRunner);
            abilitySelectionUI?.SetRunner(newRunner);
            chestMilestoneSpawner?.SetRunner(newRunner);
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            if (currentRunner != null)
            {
                return currentRunner.transform.position;
            }

            return Vector3.zero;
        }

        private Quaternion ResolveSpawnRotation()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.rotation;
            }

            if (currentRunner != null)
            {
                return currentRunner.transform.rotation;
            }

            return Quaternion.identity;
        }
    }
}
