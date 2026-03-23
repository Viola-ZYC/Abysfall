using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EndlessRunner;

public class PlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator Smoke_FrameAdvances()
    {
        var startFrame = Time.frameCount;
        yield return null;
        Assert.Greater(Time.frameCount, startFrame);
    }

    [UnityTest]
    public IEnumerator Smoke_CanCreateAndDestroyGameObject()
    {
        var go = new GameObject("PlayModeSmoke");
        Assert.IsNotNull(go);
        Object.Destroy(go);
        yield return null;
        Assert.IsTrue(go == null);
    }

    [UnityTest]
    public IEnumerator SampleScene_CoreSystemsPresent()
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

        var camera = Object.FindObjectOfType<Camera>();
        Assert.IsNotNull(camera, "SampleScene should contain a Camera.");

        var gameManager = Object.FindObjectOfType<GameManager>();
        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");

        var runner = Object.FindObjectOfType<RunnerController>();
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        Assert.IsNotNull(runner.GetComponent<Rigidbody2D>(), "Runner should have Rigidbody2D.");
        Assert.IsNotNull(runner.GetComponent<Collider2D>(), "Runner should have Collider2D.");

        var scoreManager = Object.FindObjectOfType<ScoreManager>();
        Assert.IsNotNull(scoreManager, "SampleScene should contain ScoreManager.");

        var trackManager = Object.FindObjectOfType<TrackManager>();
        Assert.IsNotNull(trackManager, "SampleScene should contain TrackManager.");

        var abilityManager = Object.FindObjectOfType<AbilityManager>();
        Assert.IsNotNull(abilityManager, "SampleScene should contain AbilityManager.");

        gameManager.BeginRun();
        yield return null;

        Assert.AreEqual(GameState.Running, gameManager.State, "Game should be running after BeginRun.");

        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }

        var segments = Object.FindObjectsOfType<TrackSegment>();
        Assert.Greater(segments.Length, 0, "Track should spawn at least one segment.");

        var background = Object.FindObjectOfType<InfiniteVerticalTilemap>();
        Assert.IsNotNull(background, "SampleScene should contain InfiniteVerticalTilemap.");
        var tilemap = background != null ? background.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>() : null;
        Assert.IsNotNull(tilemap, "Background should include a Tilemap child.");
    }
}
