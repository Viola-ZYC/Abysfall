using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EndlessRunner
{
    public static class CodexTableParser
    {
        private const string DocsFolderName = "Docs";
        private const string CodexFolderName = "图鉴";

        public static void PopulateFromDocs(CodexDatabase database)
        {
            if (database == null)
            {
                return;
            }

            string root = GetDocsRoot();
            database.creatures = ParseTable(Path.Combine(root, "生物表.md"));
            database.obstacles = ParseTable(Path.Combine(root, "障碍表.md"));
            List<CodexEntry> pads = ParseTable(Path.Combine(root, "跳板表.md"));
            if (pads.Count > 0)
            {
                database.creatures.AddRange(pads);
            }
            database.pads = new List<CodexEntry>();
            database.collections = ParseTable(Path.Combine(root, "收藏品表.md"));
        }

#if UNITY_EDITOR
        public static CodexDatabase BuildRuntimeDatabaseFromDocs()
        {
            CodexDatabase database = ScriptableObject.CreateInstance<CodexDatabase>();
            PopulateFromDocs(database);
            return database;
        }
#else
        public static CodexDatabase BuildRuntimeDatabaseFromDocs()
        {
            return null;
        }
#endif

        private static string GetDocsRoot()
        {
            string root = Application.dataPath;
            string projectRoot = Directory.GetParent(root)?.FullName ?? root;
            return Path.Combine(projectRoot, DocsFolderName, CodexFolderName);
        }

        private static List<CodexEntry> ParseTable(string path)
        {
            List<CodexEntry> entries = new List<CodexEntry>();
            if (!File.Exists(path))
            {
                return entries;
            }

            string[] lines = File.ReadAllLines(path);
            int headerIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (IsTableRow(lines[i]))
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex < 0 || headerIndex + 1 >= lines.Length)
            {
                return entries;
            }

            List<string> headers = SplitRow(lines[headerIndex]);
            if (headers == null || headers.Count == 0)
            {
                return entries;
            }

            Dictionary<string, int> columnIndex = BuildHeaderIndex(headers);

            for (int i = headerIndex + 2; i < lines.Length; i++)
            {
                if (!IsTableRow(lines[i]))
                {
                    break;
                }

                List<string> cells = SplitRow(lines[i]);
                if (cells == null || cells.Count == 0)
                {
                    continue;
                }

                CodexEntry entry = BuildEntry(columnIndex, cells);
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private static bool IsTableRow(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            return trimmed.StartsWith("|", StringComparison.Ordinal) && trimmed.Contains("|");
        }

        private static List<string> SplitRow(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            string trimmed = line.Trim();
            if (!trimmed.StartsWith("|", StringComparison.Ordinal))
            {
                return null;
            }

            string[] parts = trimmed.Split('|');
            List<string> cells = new List<string>();
            for (int i = 1; i < parts.Length - 1; i++)
            {
                cells.Add(parts[i].Trim());
            }

            return cells;
        }

        private static Dictionary<string, int> BuildHeaderIndex(List<string> headers)
        {
            Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                if (header.Contains("ID", StringComparison.OrdinalIgnoreCase))
                {
                    index["id"] = i;
                }
                else if (header.Contains("名称", StringComparison.OrdinalIgnoreCase))
                {
                    index["title"] = i;
                }
                else if (header.Contains("解锁", StringComparison.OrdinalIgnoreCase))
                {
                    index["unlock"] = i;
                }
                else if (header.Contains("描述", StringComparison.OrdinalIgnoreCase))
                {
                    index["description"] = i;
                }
                else if (header.Contains("能力", StringComparison.OrdinalIgnoreCase))
                {
                    index["ability"] = i;
                }
                else if (header.Contains("主/被动", StringComparison.OrdinalIgnoreCase))
                {
                    index["passive"] = i;
                }
                else if (header.Contains("图标", StringComparison.OrdinalIgnoreCase))
                {
                    index["icon"] = i;
                }
                else if (header.Contains("备注", StringComparison.OrdinalIgnoreCase))
                {
                    index["note"] = i;
                }
            }

            return index;
        }

        private static CodexEntry BuildEntry(Dictionary<string, int> index, List<string> cells)
        {
            CodexEntry entry = new CodexEntry
            {
                id = GetCell(index, "id", cells),
                title = GetCell(index, "title", cells),
                unlockHint = GetCell(index, "unlock", cells),
                description = GetCell(index, "description", cells),
                abilityId = GetCell(index, "ability", cells),
                icon = GetCell(index, "icon", cells),
                note = GetCell(index, "note", cells),
                isPassive = ParsePassive(GetCell(index, "passive", cells))
            };

            return entry;
        }

        private static string GetCell(Dictionary<string, int> index, string key, List<string> cells)
        {
            if (!index.TryGetValue(key, out int column) || column < 0 || column >= cells.Count)
            {
                return string.Empty;
            }

            return cells[column];
        }

        private static bool ParsePassive(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Contains("被动", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.Contains("主动", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return value.Contains("passive", StringComparison.OrdinalIgnoreCase);
        }
    }
}
