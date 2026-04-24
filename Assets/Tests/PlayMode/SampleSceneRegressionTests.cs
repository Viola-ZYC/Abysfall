using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EndlessRunner;
using Object = UnityEngine.Object;

public class SampleSceneRegressionTests
{
    private const string SceneName = "SampleScene";

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
    }

    [UnityTest]
    public IEnumerator BackgroundSegments_Recycle_AfterRunnerMovesFarDown()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        RunnerController runner = Object.FindAnyObjectByType<RunnerController>();
        InfiniteVerticalTilemap background = Object.FindAnyObjectByType<InfiniteVerticalTilemap>();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        Assert.IsNotNull(background, "SampleScene should contain InfiniteVerticalTilemap.");
        Assert.IsTrue(background.enabled, "InfiniteVerticalTilemap should be enabled so the background can recycle during gameplay.");

        gameManager.BeginRun();
        yield return null;
        yield return new WaitForFixedUpdate();

        Transform[] segments = GetBackgroundSegments(background.transform);
        Assert.GreaterOrEqual(segments.Length, 2, "Background should contain multiple loop segments.");

        float[] before = CaptureSegmentYPositions(segments);

        Rigidbody2D body = runner.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");

        Vector2 movedPosition = new Vector2(body.position.x, body.position.y - 80f);
        body.position = movedPosition;
        runner.transform.position = new Vector3(movedPosition.x, movedPosition.y, runner.transform.position.z);
        body.linearVelocity = Vector2.down * 5f;

        for (int i = 0; i < 8; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        float[] after = CaptureSegmentYPositions(segments);
        bool anySegmentMoved = false;
        for (int i = 0; i < segments.Length; i++)
        {
            if (Mathf.Abs(after[i] - before[i]) > 0.5f)
            {
                anySegmentMoved = true;
                break;
            }
        }

        Assert.IsTrue(anySegmentMoved, "Expected at least one background segment to recycle after the runner moved far downward.");
    }

    [UnityTest]
    public IEnumerator BackgroundLayers_UseDistinctParallaxRates_WhenRunnerMovesDown()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        RunnerController runner = Object.FindAnyObjectByType<RunnerController>();
        InfiniteVerticalTilemap background = Object.FindAnyObjectByType<InfiniteVerticalTilemap>();
        Camera camera = Object.FindAnyObjectByType<Camera>();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        Assert.IsNotNull(background, "SampleScene should contain InfiniteVerticalTilemap.");
        Assert.IsNotNull(camera, "SampleScene should contain a Camera.");

        gameManager.BeginRun();
        yield return null;
        yield return new WaitForFixedUpdate();

        Transform superFarSegment = GetFirstSegmentInGroup(background.transform, "SuperFarRoot");
        Transform farSegment = GetFirstSegmentInGroup(background.transform, "FarRoot");
        Transform closeSegment = GetFirstSegmentInGroup(background.transform, "CloseRoot");
        Transform wallSegment = GetFirstSegmentInGroup(background.transform, "WallRoot");

        Assert.IsNotNull(superFarSegment, "SuperFarRoot should contain a parallax segment.");
        Assert.IsNotNull(farSegment, "FarRoot should contain a parallax segment.");
        Assert.IsNotNull(closeSegment, "CloseRoot should contain a parallax segment.");
        Assert.IsNotNull(wallSegment, "WallRoot should contain a parallax segment.");

        float startCameraY = camera.transform.position.y;
        float startSuperFarY = superFarSegment.position.y;
        float startFarY = farSegment.position.y;
        float startCloseY = closeSegment.position.y;
        float startWallY = wallSegment.position.y;

        Rigidbody2D body = runner.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");

        Vector3 runnerViewport = camera.WorldToViewportPoint(runner.transform.position);
        float targetViewportY = 0.55f;
        float requiredDropDistance = Mathf.Max(0f, runnerViewport.y - targetViewportY) * camera.orthographicSize * 2f + 0.25f;
        Vector2 movedPosition = new Vector2(body.position.x, body.position.y - Mathf.Max(6f, requiredDropDistance));
        body.position = movedPosition;
        runner.transform.position = new Vector3(movedPosition.x, movedPosition.y, runner.transform.position.z);
        body.linearVelocity = Vector2.down * 6f;

        float cameraDeltaY = 0f;
        bool cameraStartedFollowing = false;
        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;

            cameraDeltaY = camera.transform.position.y - startCameraY;
            if (cameraDeltaY < -0.25f)
            {
                cameraStartedFollowing = true;
                break;
            }
        }

        Assert.IsTrue(
            cameraStartedFollowing,
            $"Camera should start following downward after the runner crosses the initial viewport threshold. CameraDelta={cameraDeltaY}, RunnerViewport={camera.WorldToViewportPoint(runner.transform.position).y}");

        for (int i = 0; i < 6; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        cameraDeltaY = camera.transform.position.y - startCameraY;
        Assert.Less(cameraDeltaY, -0.5f, "Camera should move downward once the runner reaches the follow threshold.");

        float superFarScreenShift = (superFarSegment.position.y - startSuperFarY) - cameraDeltaY;
        float farScreenShift = (farSegment.position.y - startFarY) - cameraDeltaY;
        float closeScreenShift = (closeSegment.position.y - startCloseY) - cameraDeltaY;
        float wallScreenShift = (wallSegment.position.y - startWallY) - cameraDeltaY;

        Assert.Less(superFarScreenShift, farScreenShift - 0.05f, "Super-far background should scroll less on screen than the far layer.");
        Assert.Less(farScreenShift, closeScreenShift - 0.05f, "Far background should scroll less on screen than the close layer.");
        Assert.Less(closeScreenShift, wallScreenShift - 0.05f, "Wall layer should scroll the most on screen.");
    }

    [UnityTest]
    public IEnumerator Runner_StillFalls_WhenPressedAgainstSideWall()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        RunnerController runner = Object.FindAnyObjectByType<RunnerController>();
        InfiniteVerticalTilemap background = Object.FindAnyObjectByType<InfiniteVerticalTilemap>();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        Assert.IsNotNull(background, "SampleScene should contain InfiniteVerticalTilemap.");

        List<BoxCollider2D> wallColliders = GetWallColliders(background.transform);
        Assert.GreaterOrEqual(wallColliders.Count, 2, "Background prefab should provide both side wall colliders.");

        for (int i = 0; i < wallColliders.Count; i++)
        {
            Assert.IsTrue(wallColliders[i].isTrigger, $"{wallColliders[i].name} should be a trigger so it does not physically stop the runner from falling.");
        }

        gameManager.BeginRun();
        yield return null;
        yield return new WaitForFixedUpdate();

        Rigidbody2D body = runner.GetComponent<Rigidbody2D>();
        Collider2D runnerCollider = runner.GetComponent<Collider2D>();
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");
        Assert.IsNotNull(runnerCollider, "Runner should have Collider2D.");

        runner.GetHorizontalBounds(out float minX, out _);
        float startY = body.position.y;
        BoxCollider2D leftWall = GetClosestWallCollider(wallColliders, startY, "LeftWall");
        BoxCollider2D rightWall = GetClosestWallCollider(wallColliders, startY, "RightWall");
        Assert.IsNotNull(leftWall, "Background should expose at least one left wall collider near the runner.");
        Assert.IsNotNull(rightWall, "Background should expose at least one right wall collider near the runner.");
        Assert.Less(Mathf.Abs(leftWall.bounds.max.x - minX), 0.2f, "Left wall visual/collider should align with the runner's left gameplay bound.");
        runner.GetHorizontalBounds(out _, out float maxX);
        Assert.Less(Mathf.Abs(rightWall.bounds.min.x - maxX), 0.2f, "Right wall visual/collider should align with the runner's right gameplay bound.");

        float targetX = Mathf.Max(minX, leftWall.bounds.max.x - runnerCollider.bounds.extents.x + 0.01f);
        Vector2 wallContactPosition = new Vector2(targetX, startY);

        body.position = wallContactPosition;
        runner.transform.position = new Vector3(wallContactPosition.x, wallContactPosition.y, runner.transform.position.z);
        body.linearVelocity = new Vector2(0f, -2f);

        for (int i = 0; i < 12; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Less(body.position.y, startY - 0.25f, "Runner should keep moving downward after touching the side wall.");
        Assert.Less(body.linearVelocity.y, -0.1f, "Runner should retain downward velocity near the side wall.");
    }

    [UnityTest]
    public IEnumerator Creatures_StartOffscreen_ThenBecomeVisible_WithValidSprites()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        InfiniteVerticalTilemap background = Object.FindAnyObjectByType<InfiniteVerticalTilemap>();
        RunnerController runner = Object.FindAnyObjectByType<RunnerController>();
        Camera camera = Object.FindAnyObjectByType<Camera>();

        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");
        Assert.IsNotNull(background, "SampleScene should contain InfiniteVerticalTilemap.");
        Assert.IsNotNull(runner, "SampleScene should contain RunnerController.");
        Assert.IsNotNull(camera, "SampleScene should contain a Camera.");

        gameManager.BeginRun();

        for (int i = 0; i < 3; i++)
        {
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        SpecialCreature[] initialCreatures =
            Object.FindObjectsByType<SpecialCreature>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<SpecialCreature> initiallyVisibleCreatures = GetVisibleCreatures(camera, initialCreatures);
        string segmentDiagnostics = DescribeSegmentPrefabSources();

        Assert.AreEqual(
            0,
            initiallyVisibleCreatures.Count,
            $"Creatures should start outside the camera viewport.\nVisible creatures:\n{DescribeCreatures(initiallyVisibleCreatures)}\nAll creatures:\n{DescribeCreatures(initialCreatures)}\nSegment sources:\n{segmentDiagnostics}");

        Rigidbody2D body = runner.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(body, "Runner should have Rigidbody2D.");

        Vector2 movedPosition = new Vector2(body.position.x, body.position.y - 8f);
        body.position = movedPosition;
        runner.transform.position = new Vector3(movedPosition.x, movedPosition.y, runner.transform.position.z);
        body.linearVelocity = Vector2.down * 12f;

        SpecialCreature[] creatures = initialCreatures;
        List<SpecialCreature> visibleCreatures = initiallyVisibleCreatures;
        bool creatureEnteredViewport = false;

        for (int i = 0; i < 90; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;

            creatures = Object.FindObjectsByType<SpecialCreature>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            visibleCreatures = GetVisibleCreatures(camera, creatures);
            if (visibleCreatures.Count > 0)
            {
                creatureEnteredViewport = true;
                break;
            }
        }

        Assert.IsTrue(creatureEnteredViewport,
            $"Expected at least one creature to enter the camera viewport after the runner descended. Camera={camera.transform.position}, Runner={runner.transform.position}\nAll creatures:\n{DescribeCreatures(creatures)}\nSegment sources:\n{segmentDiagnostics}");

        int wallSortingOrder = GetMaxWallSortingOrder(background.transform);
        bool hasRewardCreature = false;
        bool hasHazardCreature = false;
        StringBuilder diagnostics = new StringBuilder();

        for (int i = 0; i < creatures.Length; i++)
        {
            SpecialCreature creature = creatures[i];
            Assert.IsNotNull(creature, "Spawned creature reference should not be null.");

            SpriteRenderer spriteRenderer = creature.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(spriteRenderer, $"{creature.name} should have a SpriteRenderer.");
            Assert.IsTrue(spriteRenderer.enabled, $"{creature.name} SpriteRenderer should be enabled.");
            Assert.IsNotNull(spriteRenderer.sprite, $"{creature.name} spawned without an assigned sprite.");
            Assert.Greater(spriteRenderer.color.a, 0.01f, $"{creature.name} sprite alpha should stay visible.");
            Assert.Greater(spriteRenderer.sortingOrder, wallSortingOrder,
                $"{creature.name} should render above the wall layer. Wall={wallSortingOrder}, Creature={spriteRenderer.sortingOrder}.");

            bool replacesAbility = creature.ReplacesAbilityOnHit();
            bool isHazard = creature.IsHazard();
            hasRewardCreature |= replacesAbility;
            hasHazardCreature |= isHazard;

            diagnostics.AppendLine(
                $"{creature.name} codex={creature.CodexEntryId} reward={replacesAbility} hazard={isHazard} sprite={spriteRenderer.sprite.name} sortingOrder={spriteRenderer.sortingOrder}");
        }

        string diagnosticText = diagnostics.ToString();
        Assert.IsTrue(hasRewardCreature,
            $"Expected at least one reward creature to exist during gameplay.\nActive creatures:\n{diagnosticText}\nSegment sources:\n{segmentDiagnostics}");
        Assert.IsTrue(hasHazardCreature,
            $"Expected at least one hazard creature to exist during gameplay.\nActive creatures:\n{diagnosticText}\nSegment sources:\n{segmentDiagnostics}");
    }

    private static Transform GetFirstSegmentInGroup(Transform backgroundRoot, string groupName)
    {
        Transform group = backgroundRoot.Find(groupName);
        if (group == null)
        {
            return null;
        }

        for (int i = 0; i < group.childCount; i++)
        {
            Transform segment = group.GetChild(i);
            if (segment.GetComponentInChildren<Renderer>(true) != null)
            {
                return segment;
            }
        }

        return null;
    }

    private static Transform[] GetBackgroundSegments(Transform backgroundRoot)
    {
        List<Transform> segments = new List<Transform>();
        CollectBackgroundSegments(backgroundRoot, segments);
        return segments.ToArray();
    }

    private static float[] CaptureSegmentYPositions(Transform[] segments)
    {
        float[] positions = new float[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            positions[i] = segments[i].position.y;
        }

        return positions;
    }

    private static List<BoxCollider2D> GetWallColliders(Transform root)
    {
        BoxCollider2D[] colliders = root.GetComponentsInChildren<BoxCollider2D>(true);
        List<BoxCollider2D> walls = new List<BoxCollider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            string colliderName = colliders[i].name;
            if (colliderName == "LeftWall" || colliderName == "RightWall")
            {
                walls.Add(colliders[i]);
            }
        }

        return walls;
    }

    private static int GetMaxWallSortingOrder(Transform root)
    {
        Transform wallRoot = root.Find("WallRoot");
        Assert.IsNotNull(wallRoot, "InfiniteVerticalTilemap should contain WallRoot.");

        SpriteRenderer[] renderers = wallRoot.GetComponentsInChildren<SpriteRenderer>(true);
        Assert.Greater(renderers.Length, 0, "WallRoot should contain SpriteRenderer components.");

        int maxSortingOrder = int.MinValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            maxSortingOrder = Mathf.Max(maxSortingOrder, renderers[i].sortingOrder);
        }

        return maxSortingOrder;
    }

    private static List<SpecialCreature> GetVisibleCreatures(Camera camera, IEnumerable<SpecialCreature> creatures)
    {
        List<SpecialCreature> visible = new List<SpecialCreature>();
        if (camera == null || creatures == null)
        {
            return visible;
        }

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        foreach (SpecialCreature creature in creatures)
        {
            if (creature == null)
            {
                continue;
            }

            SpriteRenderer spriteRenderer = creature.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null)
            {
                continue;
            }

            if (GeometryUtility.TestPlanesAABB(frustumPlanes, spriteRenderer.bounds))
            {
                visible.Add(creature);
            }
        }

        return visible;
    }

    private static string DescribeCreatures(IEnumerable<SpecialCreature> creatures)
    {
        if (creatures == null)
        {
            return "No creatures.";
        }

        StringBuilder diagnostics = new StringBuilder();
        foreach (SpecialCreature creature in creatures)
        {
            if (creature == null)
            {
                diagnostics.AppendLine("<null>");
                continue;
            }

            SpriteRenderer spriteRenderer = creature.GetComponent<SpriteRenderer>();
            string spriteName = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.name
                : "<missing>";

            diagnostics.AppendLine(
                $"{creature.name} pos={creature.transform.position} codex={creature.CodexEntryId} reward={creature.ReplacesAbilityOnHit()} hazard={creature.IsHazard()} sprite={spriteName}");
        }

        return diagnostics.Length > 0 ? diagnostics.ToString() : "No creatures.";
    }

    private static string DescribeSegmentPrefabSources()
    {
        SegmentContent[] segmentContents =
            Object.FindObjectsByType<SegmentContent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (segmentContents.Length == 0)
        {
            return "No active SegmentContent instances found.";
        }

        StringBuilder diagnostics = new StringBuilder();
        for (int i = 0; i < segmentContents.Length; i++)
        {
            SegmentContent segmentContent = segmentContents[i];
            diagnostics.AppendLine($"SegmentContent[{i}] {segmentContent.name}");
            AppendPrefabDiagnostics(diagnostics, "creaturePrefabs", ReadPrefabArray(segmentContent, "creaturePrefabs"));
        }

        return diagnostics.ToString();
    }

    private static GameObject[] ReadPrefabArray(SegmentContent segmentContent, string fieldName)
    {
        FieldInfo field = typeof(SegmentContent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected SegmentContent private field '{fieldName}'.");
        return field.GetValue(segmentContent) as GameObject[];
    }

    private static void AppendPrefabDiagnostics(StringBuilder diagnostics, string label, GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            diagnostics.AppendLine($"  {label}: <empty>");
            return;
        }

        diagnostics.AppendLine($"  {label}:");
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
            {
                diagnostics.AppendLine($"    [{i}] <null>");
                continue;
            }

            SpecialCreature creature = prefab.GetComponent<SpecialCreature>();
            if (creature != null)
            {
                diagnostics.AppendLine(
                    $"    [{i}] {prefab.name} codex={creature.CodexEntryId} reward={creature.ReplacesAbilityOnHit()} hazard={creature.IsHazard()}");
                continue;
            }

            diagnostics.AppendLine($"    [{i}] {prefab.name} (non-creature)");
        }
    }

    private static void CollectBackgroundSegments(Transform root, List<Transform> segments)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("Seg_") || child.name == "Wall_1_Grid_BG")
            {
                if (child.GetComponentInChildren<Renderer>(true) != null)
                {
                    segments.Add(child);
                }

                continue;
            }

            CollectBackgroundSegments(child, segments);
        }
    }

    private static BoxCollider2D GetClosestWallCollider(List<BoxCollider2D> walls, float targetY, string wallName)
    {
        BoxCollider2D best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < walls.Count; i++)
        {
            BoxCollider2D wall = walls[i];
            if (wall == null || wall.name != wallName)
            {
                continue;
            }

            float distance = Mathf.Abs(wall.bounds.center.y - targetY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = wall;
            }
        }

        return best;
    }
}
