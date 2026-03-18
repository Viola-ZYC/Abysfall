using UnityEditor;
using UnityEngine;

namespace EndlessRunner.EditorTools
{
    public static class CodexTableImporter
    {
        private const string AssetPath = "Assets/Game/Resources/Codex/CodexDatabase.asset";

        [MenuItem("Tools/Codex/Build Codex Database")]
        public static void BuildCodexDatabase()
        {
            CodexDatabase database = AssetDatabase.LoadAssetAtPath<CodexDatabase>(AssetPath);
            if (database == null)
            {
                EnsureFolder("Assets/Game/Resources");
                EnsureFolder("Assets/Game/Resources/Codex");
                database = ScriptableObject.CreateInstance<CodexDatabase>();
                AssetDatabase.CreateAsset(database, AssetPath);
            }

            CodexTableParser.PopulateFromDocs(database);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Codex database updated from Docs/图鉴.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
