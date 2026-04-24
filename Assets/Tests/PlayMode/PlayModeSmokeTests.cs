using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EndlessRunner;
using Object = UnityEngine.Object;

public class PlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator Smoke_FrameAdvances()
    {
        yield return RunWithLogging(nameof(Smoke_FrameAdvances), Smoke_FrameAdvances_Impl);
    }

    [UnityTest]
    public IEnumerator Smoke_CanCreateAndDestroyGameObject()
    {
        yield return RunWithLogging(nameof(Smoke_CanCreateAndDestroyGameObject), Smoke_CanCreateAndDestroyGameObject_Impl);
    }

    [UnityTest]
    public IEnumerator SampleScene_CoreSystemsPresent()
    {
        yield return RunWithLogging(nameof(SampleScene_CoreSystemsPresent), SampleScene_CoreSystemsPresent_Impl);
    }

    private IEnumerator Smoke_FrameAdvances_Impl()
    {
        var startFrame = Time.frameCount;
        yield return null;
        Assert.Greater(Time.frameCount, startFrame);
    }

    private IEnumerator Smoke_CanCreateAndDestroyGameObject_Impl()
    {
        var go = new GameObject("PlayModeSmoke");
        Assert.IsNotNull(go);
        Object.Destroy(go);
        yield return null;
        Assert.IsTrue(go == null);
    }

    private IEnumerator SampleScene_CoreSystemsPresent_Impl()
    {
        Time.timeScale = 1f;
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            var load = SceneManager.LoadSceneAsync("SampleScene");
            while (!load.isDone)
            {
                yield return null;
            }
        }

        yield return null;

        var camera = Object.FindAnyObjectByType<Camera>();
        Assert.IsNotNull(camera, "SampleScene should contain a Camera.");

        var gameManager = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");

        var runner = Object.FindAnyObjectByType<RunnerController>();
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        Assert.IsNotNull(runner.GetComponent<Rigidbody2D>(), "Runner should have Rigidbody2D.");
        Assert.IsNotNull(runner.GetComponent<Collider2D>(), "Runner should have Collider2D.");

        var scoreManager = Object.FindAnyObjectByType<ScoreManager>();
        Assert.IsNotNull(scoreManager, "SampleScene should contain ScoreManager.");

        var trackManager = Object.FindAnyObjectByType<TrackManager>();
        Assert.IsNotNull(trackManager, "SampleScene should contain TrackManager.");

        var abilityManager = Object.FindAnyObjectByType<AbilityManager>();
        Assert.IsNotNull(abilityManager, "SampleScene should contain AbilityManager.");

        gameManager.BeginRun();
        yield return null;

        Assert.AreEqual(GameState.Running, gameManager.State, "Game should be running after BeginRun.");

        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }

        var segments = Object.FindObjectsByType<TrackSegment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Assert.Greater(segments.Length, 0, "Track should spawn at least one segment.");

        var background = Object.FindAnyObjectByType<InfiniteVerticalTilemap>();
        Assert.IsNotNull(background, "SampleScene should contain InfiniteVerticalTilemap.");
        var backgroundRenderer = background != null ? background.GetComponentInChildren<Renderer>(true) : null;
        Assert.IsNotNull(backgroundRenderer, "Background should include at least one renderable layer.");
        Assert.IsNotNull(background.transform.Find("BaseRoot"), "Background should expose the grouped BaseRoot container.");
        Assert.IsNotNull(background.transform.Find("WallRoot"), "Background should expose the grouped WallRoot container.");
    }

    private static IEnumerator RunWithLogging(string testName, Func<IEnumerator> body)
    {
        Exception caught = null;
        IEnumerator routine = null;

        try
        {
            routine = body();
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        if (caught != null)
        {
            LogFail(testName, caught);
            throw caught;
        }

        while (true)
        {
            bool movedNext = false;
            object current = null;

            try
            {
                movedNext = routine.MoveNext();
                if (movedNext)
                {
                    current = routine.Current;
                }
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            if (caught != null)
            {
                LogFail(testName, caught);
                throw caught;
            }

            if (!movedNext)
            {
                break;
            }

            yield return current;
        }

        Debug.Log($"[PlayModeTest] {testName}: PASS");
    }

    private static void LogFail(string testName, Exception ex)
    {
        Debug.LogError($"[PlayModeTest] {testName}: FAIL - {ex.GetType().Name}: {ex.Message}");
    }
}
