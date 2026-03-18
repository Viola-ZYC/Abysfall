using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    [CreateAssetMenu(menuName = "EndlessRunner/Codex Database")]
    public class CodexDatabase : ScriptableObject
    {
        public List<CodexEntry> creatures = new();
        public List<CodexEntry> obstacles = new();
        public List<CodexEntry> collections = new();

        public static CodexDatabase Load()
        {
            CodexDatabase database = Resources.Load<CodexDatabase>("Codex/CodexDatabase");
#if UNITY_EDITOR
            if (database == null)
            {
                database = CodexTableParser.BuildRuntimeDatabaseFromDocs();
            }
#endif
            return database;
        }

        public IReadOnlyList<CodexEntry> GetEntries(CodexCategory category)
        {
            return category switch
            {
                CodexCategory.Creature => creatures,
                CodexCategory.Obstacle => obstacles,
                CodexCategory.Collection => collections,
                _ => creatures
            };
        }

        public int GetEntryCount(CodexCategory category)
        {
            return GetEntries(category)?.Count ?? 0;
        }

        public CodexEntry FindEntry(CodexCategory category, string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                return null;
            }

            IReadOnlyList<CodexEntry> list = GetEntries(category);
            if (list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                CodexEntry entry = list[i];
                if (entry != null && string.Equals(entry.id, entryId, System.StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public string GetEntryId(CodexCategory category, int index)
        {
            IReadOnlyList<CodexEntry> list = GetEntries(category);
            if (list == null || index < 0 || index >= list.Count)
            {
                return string.Empty;
            }

            CodexEntry entry = list[index];
            return entry != null ? entry.id : string.Empty;
        }
    }
}
