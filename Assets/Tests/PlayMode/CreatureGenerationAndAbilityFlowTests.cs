using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EndlessRunner;
using Object = UnityEngine.Object;

public class CreatureGenerationAndAbilityFlowTests
{
    private const string SceneName = "SampleScene";

    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly FieldInfo CreaturePrefabsField =
        typeof(SegmentContent).GetField("creaturePrefabs", InstanceNonPublic);

    private static readonly FieldInfo SpecialCreaturePrefabsField =
        typeof(MilestoneSpawnController).GetField("specialCreaturePrefabs", InstanceNonPublic);

    private static readonly MethodInfo FilterPrefabsByScoreMethod =
        typeof(SegmentContent).GetMethod("FilterPrefabsByScore", InstanceNonPublic);

    private static readonly MethodInfo TryHandleHazardContactMethod =
        typeof(RunnerController).GetMethod("TryHandleHazardContact", InstanceNonPublic);

    private static readonly MethodInfo GetEffectiveHorizontalSpeedMethod =
        typeof(RunnerController).GetMethod("GetEffectiveHorizontalSpeed", InstanceNonPublic);

    private static readonly (string entryId, int spawnScore)[] RewardCreatureThresholds =
    {
        ("creature_normal", 0),
        ("creature_speed_boost", 100),
        ("creature_tough_skin", 300),
        ("creature_lightweight", 500),
        ("creature_echo", 700),
        ("creature_abyss_eye", 900)
    };

    [UnitySetUp]
    public IEnumerator SetUpScene()
    {
        Time.timeScale = 1f;

        AsyncOperation load = SceneManager.LoadSceneAsync(SceneName);
        while (!load.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return new WaitForFixedUpdate();

        AbilityAcquiredUI popup = Object.FindAnyObjectByType<AbilityAcquiredUI>();
        if (popup != null)
        {
            Object.Destroy(popup);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ConnectedCreatureSpawnFiltering_MatchesConfiguredThresholds()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");

        gameManager.BeginRun();
        yield return null;
        yield return new WaitForFixedUpdate();

        SegmentContent segmentContent = Object.FindAnyObjectByType<SegmentContent>();
        Assert.IsNotNull(segmentContent, "Gameplay should spawn at least one SegmentContent instance.");

        GameObject[] creaturePrefabs = ReadPrefabArray(segmentContent, CreaturePrefabsField);

        Assert.IsNotNull(creaturePrefabs, "SegmentContent creature prefab array should exist.");

        for (int i = 0; i < RewardCreatureThresholds.Length; i++)
        {
            (string entryId, int spawnScore) = RewardCreatureThresholds[i];
            GameObject[] eligibleAtScore =
                FilterPrefabs(segmentContent, creaturePrefabs, spawnScore, CodexCategory.Creature);

            AssertPrefabPresence(
                eligibleAtScore,
                entryId,
                shouldExist: true,
                $"Reward creature {entryId} should be eligible once score reaches {spawnScore}.");

            if (spawnScore > 0)
            {
                GameObject[] eligibleBeforeScore =
                    FilterPrefabs(segmentContent, creaturePrefabs, spawnScore - 1, CodexCategory.Creature);

                AssertPrefabPresence(
                    eligibleBeforeScore,
                    entryId,
                    shouldExist: false,
                    $"Reward creature {entryId} should stay locked before score {spawnScore}.");
            }
        }

        GameObject[] rewardPoolAtHighScore =
            FilterPrefabs(segmentContent, creaturePrefabs, 1200, CodexCategory.Creature);
        AssertPrefabPresence(
            rewardPoolAtHighScore,
            "creature_basic",
            shouldExist: false,
            "Basic hazard creature should not leak into the reward creature pool.");

    }

    [UnityTest]
    public IEnumerator BasicHazardContact_DamagesRunner_WithoutReplacingCurrentAbility()
    {
        YieldInstruction fixedStep = new WaitForFixedUpdate();
        RunnerController runner = StartRunAndGetRunner();
        AbilityManager abilityManager = Object.FindAnyObjectByType<AbilityManager>();
        MilestoneSpawnController milestoneSpawnController = Object.FindAnyObjectByType<MilestoneSpawnController>();
        Rigidbody2D body = runner != null ? runner.GetComponent<Rigidbody2D>() : null;

        Assert.IsNotNull(abilityManager, "SampleScene should contain AbilityManager.");
        Assert.IsNotNull(milestoneSpawnController, "SampleScene should contain MilestoneSpawnController.");
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");

        GameObject[] specialCreaturePrefabs = ReadPrefabArray(milestoneSpawnController, SpecialCreaturePrefabsField);

        SpecialCreature speedCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(specialCreaturePrefabs, "creature_speed_boost"),
            "speed creature");
        runner.OnAttackHit(speedCreature);
        yield return null;
        AssertCurrentAbility(abilityManager, "speed_boost");

        int healthBefore = runner.CurrentHealth;
        body.linearVelocity = new Vector2(4f, -7f);

        SpecialCreature basicCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(specialCreaturePrefabs, "creature_basic"),
            "basic creature");
        Collider2D basicCollider = basicCreature.GetComponent<Collider2D>();
        Assert.IsNotNull(basicCollider, "Basic creature should have Collider2D.");

        bool handled = InvokeTryHandleHazardContact(runner, basicCollider);
        yield return fixedStep;

        Assert.IsTrue(handled, "Basic creature contact should be handled as a hazard.");
        Assert.AreEqual(healthBefore - 1, runner.CurrentHealth, "Basic creature should damage the runner.");
        AssertCurrentAbility(abilityManager, "speed_boost");
        Assert.Less(body.linearVelocity.sqrMagnitude, 0.0001f,
            "Basic creature contact should reset the runner's current motion state.");
    }

    [UnityTest]
    public IEnumerator PassiveRewardCreatures_ReplaceAbility_AndModifyRunnerStats()
    {
        YieldInstruction fixedStep = new WaitForFixedUpdate();
        RunnerController runner = StartRunAndGetRunner();
        AbilityManager abilityManager = Object.FindAnyObjectByType<AbilityManager>();
        ScoreManager scoreManager = Object.FindAnyObjectByType<ScoreManager>();
        MilestoneSpawnController milestoneSpawnController = Object.FindAnyObjectByType<MilestoneSpawnController>();
        Rigidbody2D body = runner != null ? runner.GetComponent<Rigidbody2D>() : null;

        Assert.IsNotNull(abilityManager, "SampleScene should contain AbilityManager.");
        Assert.IsNotNull(scoreManager, "SampleScene should contain ScoreManager.");
        Assert.IsNotNull(milestoneSpawnController, "SampleScene should contain MilestoneSpawnController.");
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");

        GameObject[] creaturePrefabs = ReadPrefabArray(milestoneSpawnController, SpecialCreaturePrefabsField);

        yield return ResetGameplayState(runner, abilityManager, scoreManager);
        SpecialCreature normalCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(creaturePrefabs, "creature_normal"),
            "normal creature");
        runner.OnAttackHit(normalCreature);
        yield return null;
        AssertCurrentAbility(abilityManager, "normal");
        Assert.IsFalse(abilityManager.TryActivateCurrentAbility(),
            "Normal replacement ability should not expose an active skill.");

        yield return ResetGameplayState(runner, abilityManager, scoreManager);
        float baseHorizontalSpeed = InvokeGetEffectiveHorizontalSpeed(runner);
        SpecialCreature speedCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(creaturePrefabs, "creature_speed_boost"),
            "speed creature");
        runner.OnAttackHit(speedCreature);
        yield return null;
        AssertCurrentAbility(abilityManager, "speed_boost");
        float boostedHorizontalSpeed = InvokeGetEffectiveHorizontalSpeed(runner);
        Assert.Greater(
            boostedHorizontalSpeed,
            baseHorizontalSpeed * 1.19f,
            $"Speed Boost should raise effective horizontal speed. Base={baseHorizontalSpeed}, Boosted={boostedHorizontalSpeed}");

        yield return ResetGameplayState(runner, abilityManager, scoreManager);
        int baseMaxHealth = runner.MaxHealth;
        SpecialCreature toughCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(creaturePrefabs, "creature_tough_skin"),
            "tough creature");
        runner.OnAttackHit(toughCreature);
        yield return null;
        AssertCurrentAbility(abilityManager, "tough_skin");
        Assert.AreEqual(baseMaxHealth + 1, runner.MaxHealth,
            "Tough Skin should increase the runner's maximum health by 1.");

        yield return ResetGameplayState(runner, abilityManager, scoreManager);
        float baseGravityScale = body.gravityScale;
        SpecialCreature lightweightCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(creaturePrefabs, "creature_lightweight"),
            "lightweight creature");
        runner.OnAttackHit(lightweightCreature);
        yield return null;
        yield return fixedStep;
        AssertCurrentAbility(abilityManager, "lightweight");
        Assert.Less(
            body.gravityScale,
            baseGravityScale - 0.05f,
            $"Lightweight should reduce effective gravity. Base={baseGravityScale}, Current={body.gravityScale}");
    }

    [UnityTest]
    public IEnumerator ActiveRewardCreatures_AccumulateCharges_AndActivate()
    {
        YieldInstruction fixedStep = new WaitForFixedUpdate();
        RunnerController runner = StartRunAndGetRunner();
        AbilityManager abilityManager = Object.FindAnyObjectByType<AbilityManager>();
        ScoreManager scoreManager = Object.FindAnyObjectByType<ScoreManager>();
        MilestoneSpawnController milestoneSpawnController = Object.FindAnyObjectByType<MilestoneSpawnController>();
        Rigidbody2D body = runner != null ? runner.GetComponent<Rigidbody2D>() : null;

        Assert.IsNotNull(abilityManager, "SampleScene should contain AbilityManager.");
        Assert.IsNotNull(scoreManager, "SampleScene should contain ScoreManager.");
        Assert.IsNotNull(milestoneSpawnController, "SampleScene should contain MilestoneSpawnController.");
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");

        GameObject[] creaturePrefabs = ReadPrefabArray(milestoneSpawnController, SpecialCreaturePrefabsField);

        yield return ResetGameplayState(runner, abilityManager, scoreManager);
        SpecialCreature echoCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(creaturePrefabs, "creature_echo"),
            "echo creature");
        runner.OnAttackHit(echoCreature);
        yield return null;
        AssertCurrentAbility(abilityManager, "air_jump_charge_100m");

        ScoreChargeTracker airJumpTracker = FindChargeTracker(runner, "air_jump_charge_100m");
        Assert.IsNotNull(airJumpTracker, "Air Jump ability should attach a score charge tracker.");
        Assert.AreEqual(0, airJumpTracker.Charges, "Air Jump charges should start at zero after replacing the ability.");

        scoreManager.AddCollectible(10);
        yield return null;
        Assert.GreaterOrEqual(airJumpTracker.Charges, 1,
            "Air Jump should gain at least one charge after reaching 100 score.");

        body.linearVelocity = Vector2.down * 2f;
        bool airJumpActivated = abilityManager.TryActivateCurrentAbility();
        yield return fixedStep;

        Assert.IsTrue(airJumpActivated, "Air Jump should activate when a charge is available.");
        Assert.Greater(body.linearVelocity.y, 0.1f, "Air Jump should launch the runner upward.");
        Assert.AreEqual(0, airJumpTracker.Charges, "Air Jump should consume one charge when activated.");

        yield return ResetGameplayState(runner, abilityManager, scoreManager);
        SpecialCreature dashCreature = InstantiateCreaturePrefab(
            GetPrefabByCodexId(creaturePrefabs, "creature_abyss_eye"),
            "abyss eye creature");
        runner.OnAttackHit(dashCreature);
        yield return null;
        AssertCurrentAbility(abilityManager, "dash_charge_100m");

        ScoreChargeTracker dashTracker = FindChargeTracker(runner, "dash_charge_100m");
        Assert.IsNotNull(dashTracker, "Dash ability should attach a score charge tracker.");
        Assert.AreEqual(0, dashTracker.Charges, "Dash charges should start at zero after replacing the ability.");

        scoreManager.AddCollectible(10);
        yield return null;
        Assert.GreaterOrEqual(dashTracker.Charges, 1,
            "Dash should gain at least one charge after reaching 100 score.");

        body.linearVelocity = Vector2.zero;
        bool dashActivated = abilityManager.TryActivateCurrentAbility();
        yield return fixedStep;

        Assert.IsTrue(dashActivated, "Dash should activate when a charge is available.");
        Assert.Greater(body.linearVelocity.x, 0.1f, "Dash should apply a positive horizontal impulse.");
        Assert.AreEqual(0, dashTracker.Charges, "Dash should consume one charge when activated.");
    }

    private static RunnerController StartRunAndGetRunner()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        gameManager.BeginRun();

        RunnerController runner = Object.FindAnyObjectByType<RunnerController>();
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        return runner;
    }

    private static IEnumerator ResetGameplayState(
        RunnerController runner,
        AbilityManager abilityManager,
        ScoreManager scoreManager)
    {
        Assert.IsNotNull(runner, "RunnerController should exist.");
        Assert.IsNotNull(abilityManager, "AbilityManager should exist.");
        Assert.IsNotNull(scoreManager, "ScoreManager should exist.");

        GameManager gameManager = GameManager.Instance;
        gameManager?.ClearPauseRequests();
        abilityManager.ResetRun();
        runner.ResetRunner();
        scoreManager.ResetScore();

        yield return null;
        yield return new WaitForFixedUpdate();
    }

    private static GameObject[] ReadPrefabArray(Object source, FieldInfo field)
    {
        Assert.IsNotNull(field, "Expected private SegmentContent prefab field to exist.");
        return field.GetValue(source) as GameObject[];
    }

    private static GameObject[] FilterPrefabs(
        SegmentContent segmentContent,
        GameObject[] prefabs,
        int score,
        CodexCategory category)
    {
        Assert.IsNotNull(FilterPrefabsByScoreMethod, "Expected SegmentContent.FilterPrefabsByScore private method.");
        object result = FilterPrefabsByScoreMethod.Invoke(segmentContent, new object[] { prefabs, score, category });
        return result as GameObject[];
    }

    private static void AssertPrefabPresence(
        IReadOnlyList<GameObject> prefabs,
        string codexEntryId,
        bool shouldExist,
        string message)
    {
        bool found = ContainsPrefabWithCodexId(prefabs, codexEntryId);
        if (shouldExist)
        {
            Assert.IsTrue(found, message);
            return;
        }

        Assert.IsFalse(found, message);
    }

    private static bool ContainsPrefabWithCodexId(IReadOnlyList<GameObject> prefabs, string codexEntryId)
    {
        if (prefabs == null)
        {
            return false;
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
            {
                continue;
            }

            SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
            if (creature != null && creature.CodexEntryId == codexEntryId)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject GetPrefabByCodexId(IEnumerable<GameObject> prefabs, string codexEntryId)
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
            if (creature != null && creature.CodexEntryId == codexEntryId)
            {
                return prefab;
            }
        }

        Assert.Fail($"Could not find prefab with codex entry id '{codexEntryId}'.");
        return null;
    }

    private static SpecialCreature InstantiateCreaturePrefab(GameObject prefab, string label)
    {
        Assert.IsNotNull(prefab, $"Expected {label} prefab reference.");
        GameObject instance = Object.Instantiate(prefab);
        SpecialCreature creature = instance.GetComponent<SpecialCreature>();
        Assert.IsNotNull(creature, $"{label} prefab should contain SpecialCreature.");
        creature.OnSpawned();
        return creature;
    }

    private static bool InvokeTryHandleHazardContact(RunnerController runner, Collider2D collider)
    {
        Assert.IsNotNull(TryHandleHazardContactMethod,
            "Expected RunnerController.TryHandleHazardContact private method.");
        object result = TryHandleHazardContactMethod.Invoke(runner, new object[] { collider });
        return result is bool handled && handled;
    }

    private static float InvokeGetEffectiveHorizontalSpeed(RunnerController runner)
    {
        Assert.IsNotNull(GetEffectiveHorizontalSpeedMethod,
            "Expected RunnerController.GetEffectiveHorizontalSpeed private method.");
        object result = GetEffectiveHorizontalSpeedMethod.Invoke(runner, null);
        return result is float speed ? speed : 0f;
    }

    private static void AssertCurrentAbility(AbilityManager abilityManager, string abilityId)
    {
        Assert.IsNotNull(abilityManager.CurrentAbility, $"Expected current ability '{abilityId}' to be assigned.");
        Assert.AreEqual(
            abilityId,
            abilityManager.CurrentAbility.abilityId,
            $"Expected current ability to be '{abilityId}'.");
    }

    private static ScoreChargeTracker FindChargeTracker(RunnerController runner, string chargeId)
    {
        ScoreChargeTracker[] trackers = runner.GetComponents<ScoreChargeTracker>();
        for (int i = 0; i < trackers.Length; i++)
        {
            if (trackers[i] != null && trackers[i].ChargeId == chargeId)
            {
                return trackers[i];
            }
        }

        return null;
    }
}
