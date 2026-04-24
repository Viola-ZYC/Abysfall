using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class CreatureVisualAssetReferenceTests
{
    private static readonly string[] CreaturePrefabPaths =
    {
        "Assets/Game/Prefabs/Creatures/SpecialCreature_AbyssEye.prefab",
        "Assets/Game/Prefabs/Creatures/SpecialCreature_Basic.prefab",
        "Assets/Game/Prefabs/Creatures/SpecialCreature_Echo.prefab",
        "Assets/Game/Prefabs/Creatures/SpecialCreature_Lightweight.prefab",
        "Assets/Game/Prefabs/Creatures/SpecialCreature_Normal.prefab",
        "Assets/Game/Prefabs/Creatures/SpecialCreature_SpeedBoost.prefab",
        "Assets/Game/Prefabs/Creatures/SpecialCreature_ToughSkin.prefab"
    };

    private static readonly string[] CreatureAnimationPaths =
    {
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_AbyssEye.anim",
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_Basic.anim",
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_Echo.anim",
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_Golen_Idle.anim",
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_Lightweight.anim",
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_Normal.anim",
        "Assets/Game/Resources/Art/Animations/Creatures/SpecialCreature_SpeedBoost.anim"
    };

    private static readonly Regex SpriteReferenceRegex =
        new Regex(@"m_Sprite:\s*\{fileID:\s*(-?\d+), guid:\s*([0-9a-f]+), type:\s*3\}",
            RegexOptions.Compiled);

    private static readonly Regex AnimationReferenceRegex =
        new Regex(@"value:\s*\{fileID:\s*(-?\d+), guid:\s*([0-9a-f]+), type:\s*3\}",
            RegexOptions.Compiled);

    private static readonly Regex InternalIdRegex =
        new Regex(@"internalID:\s*(-?\d+)", RegexOptions.Compiled);

    private static readonly Regex SortingOrderRegex =
        new Regex(@"m_SortingOrder:\s*(-?\d+)", RegexOptions.Compiled);

    [Test]
    public void CreaturePrefabs_ReferenceImportedSprites()
    {
        for (int i = 0; i < CreaturePrefabPaths.Length; i++)
        {
            AssertPrefabSpriteReference(CreaturePrefabPaths[i]);
        }
    }

    [Test]
    public void CreatureAnimations_ReferenceImportedSprites()
    {
        for (int i = 0; i < CreatureAnimationPaths.Length; i++)
        {
            AssertAnimationSpriteReferences(CreatureAnimationPaths[i]);
        }
    }

    [Test]
    public void CreatureSortingOrder_IsAboveForegroundWall()
    {
        int wallSortingOrder = ReadSortingOrder("Assets/Game/Prefabs/Backgrounds/Wall_1_Grid_BG.prefab");
        for (int i = 0; i < CreaturePrefabPaths.Length; i++)
        {
            int creatureSortingOrder = ReadSortingOrder(CreaturePrefabPaths[i]);
            Assert.Greater(creatureSortingOrder, wallSortingOrder,
                $"{Path.GetFileName(CreaturePrefabPaths[i])} sorting order should be above wall foreground. Wall={wallSortingOrder}, Creature={creatureSortingOrder}.");
        }
    }

    private static void AssertPrefabSpriteReference(string relativePath)
    {
        string fullPath = GetProjectPath(relativePath);
        string content = File.ReadAllText(fullPath);
        Match match = SpriteReferenceRegex.Match(content);
        Assert.IsTrue(match.Success, $"Expected sprite reference in prefab: {relativePath}");

        long fileId = long.Parse(match.Groups[1].Value);
        string guid = match.Groups[2].Value;
        AssertSpriteReferenceExists(relativePath, guid, fileId);
    }

    private static void AssertAnimationSpriteReferences(string relativePath)
    {
        string fullPath = GetProjectPath(relativePath);
        string content = File.ReadAllText(fullPath);
        MatchCollection matches = AnimationReferenceRegex.Matches(content);
        Assert.Greater(matches.Count, 0, $"Expected animation sprite references in clip: {relativePath}");

        foreach (Match match in matches)
        {
            long fileId = long.Parse(match.Groups[1].Value);
            string guid = match.Groups[2].Value;
            AssertSpriteReferenceExists(relativePath, guid, fileId);
        }
    }

    private static void AssertSpriteReferenceExists(string assetPath, string guid, long fileId)
    {
        string metaPath = FindMetaPathByGuid(guid);
        Assert.IsNotNull(metaPath, $"Could not resolve .meta file for guid {guid} referenced by {assetPath}");

        HashSet<long> internalIds = GetInternalIds(metaPath);
        Assert.IsTrue(internalIds.Contains(fileId),
            $"Asset {assetPath} references missing sprite fileID {fileId} from guid {guid} ({metaPath}).");
    }

    private static string FindMetaPathByGuid(string guid)
    {
        string searchRoot = Path.Combine(Application.dataPath, "Game", "Resources", "Art", "Sprite");
        if (!Directory.Exists(searchRoot))
        {
            return null;
        }

        foreach (string metaPath in Directory.EnumerateFiles(searchRoot, "*.meta", SearchOption.AllDirectories))
        {
            using StreamReader reader = new StreamReader(metaPath);
            string firstLine;
            while ((firstLine = reader.ReadLine()) != null)
            {
                if (!firstLine.StartsWith("guid: "))
                {
                    continue;
                }

                string value = firstLine.Substring("guid: ".Length).Trim();
                if (value == guid)
                {
                    return metaPath;
                }

                break;
            }
        }

        return null;
    }

    private static HashSet<long> GetInternalIds(string metaPath)
    {
        string content = File.ReadAllText(metaPath);
        MatchCollection matches = InternalIdRegex.Matches(content);
        HashSet<long> ids = new HashSet<long>();
        foreach (Match match in matches)
        {
            ids.Add(long.Parse(match.Groups[1].Value));
        }

        return ids;
    }

    private static int ReadSortingOrder(string relativePath)
    {
        string content = File.ReadAllText(GetProjectPath(relativePath));
        Match match = SortingOrderRegex.Match(content);
        Assert.IsTrue(match.Success, $"Expected sorting order in {relativePath}");
        return int.Parse(match.Groups[1].Value);
    }

    private static string GetProjectPath(string relativePath)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
