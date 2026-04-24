using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EndlessRunner;
using Object = UnityEngine.Object;

public class CreatureRuntimeDiagnosticsTests
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
    public IEnumerator SegmentContents_ReportRuntimePrefabArrays()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(gameManager, "SampleScene should contain GameManager.");

        gameManager.BeginRun();
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;

        SegmentContent[] segmentContents =
            Object.FindObjectsByType<SegmentContent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        Assert.Greater(segmentContents.Length, 0, "Expected active SegmentContent instances after BeginRun.");

        StringBuilder diagnostics = new StringBuilder();
        for (int i = 0; i < segmentContents.Length; i++)
        {
            SegmentContent segmentContent = segmentContents[i];
            diagnostics.AppendLine($"SegmentContent[{i}] {segmentContent.name}");
            AppendPrefabDiagnostics(diagnostics, "creaturePrefabs", ReadPrefabArray(segmentContent, "creaturePrefabs"));
        }

        Debug.Log(diagnostics.ToString());
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

            diagnostics.AppendLine($"    [{i}] {prefab.name} <unknown>");
        }
    }
}
