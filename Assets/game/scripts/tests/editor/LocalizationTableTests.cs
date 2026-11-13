using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Checks the game's localization table read by Core's CSV backend: English is the first (default and fallback)
    /// language, keys are unique, every language has every text with the same placeholders (<c>{0}</c>, <c>{0:N0}</c>…),
    /// every language has the <c>language.&lt;code&gt;</c> name the settings show, the table loads from Resources where
    /// the backend looks, and every key the built data and prefabs hold is in the table.
    /// </summary>
    public sealed class LocalizationTableTests
    {
        private const string TablePath = "Assets/game/localization/Resources/Paowa/Localization/healer-time.csv";
        private const string DefaultLanguage = "en";
        private const string LanguageNamePrefix = "language.";
        private const string ResourcesFolder = "Paowa/Localization";
        private const string TableName = "healer-time";
        private const string ResourcePathHint = ResourcesFolder + "/" + TableName;
        private static readonly Regex Placeholder = new Regex(@"\{\d+(?:[,:][^}]*)?\}");
        private static readonly Regex KeyShape = new Regex(@"^[a-z][a-z0-9-]*(?:\.[a-z0-9-]+)+$");
        private static readonly string[] BuiltFolders = { "Assets/game/data", "Assets/game/prefabs" };

        // Serialized string fields that hold a localization key (names, labels, texts, formats).
        private static readonly HashSet<string> KeyFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "_displayName", "_label", "_text", "_format",
        };

        [Test]
        public void EnglishIsTheFirstLanguage()
        {
            List<string> header = ReadRows()[0];
            Assert.That(header[0], Is.EqualTo("key"));
            Assert.That(header.Count, Is.GreaterThanOrEqualTo(3), "The table needs English and at least one more language.");
            Assert.That(header[1], Is.EqualTo(DefaultLanguage));
        }

        [Test]
        public void KeysAreUnique()
        {
            List<string> duplicates = Entries().GroupBy(row => row[0]).Where(group => group.Count() > 1)
                .Select(group => group.Key).ToList();
            Assert.That(duplicates, Is.Empty, string.Join(", ", duplicates));
        }

        [Test]
        public void EveryLanguageHasEveryTextWithTheSamePlaceholders()
        {
            List<string> header = ReadRows()[0];
            var problems = new List<string>();
            foreach (List<string> row in Entries())
            {
                string english = Cell(row, 1);
                for (int column = 1; column < header.Count; column++)
                {
                    string text = Cell(row, column);
                    if (text.Length == 0)
                    {
                        problems.Add($"{row[0]}: no {header[column]} text");
                    }
                    else if (!Placeholders(text).SetEquals(Placeholders(english)))
                    {
                        problems.Add($"{row[0]}: {header[column]} placeholders differ from English");
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EveryLanguageHasItsName()
        {
            List<string> header = ReadRows()[0];
            var keys = new HashSet<string>(Entries().Select(row => row[0]));
            List<string> missing = header.Skip(1).Where(code => !keys.Contains(LanguageNamePrefix + code)).ToList();
            Assert.That(missing, Is.Empty, string.Join(", ", missing));
        }

        [Test]
        public void TheTableLoadsFromResources()
        {
            TextAsset[] tables = Resources.LoadAll<TextAsset>(ResourcesFolder);
            Assert.That(tables.Select(table => table.name), Does.Contain(TableName),
                $"Core's CSV backend reads Resources/{ResourcePathHint}; the table must be a TextAsset there.");
        }

        [Test]
        public void EveryKeyOfTheBuiltDataAndPrefabsIsInTheTable()
        {
            // No valid folder must not widen the search to the whole project (an empty folder list searches everywhere).
            string[] folders = BuiltFolders.Where(AssetDatabase.IsValidFolder).ToArray();
            string[] guids = folders.Length > 0
                ? AssetDatabase.FindAssets("t:ScriptableObject t:GameObject", folders)
                : Array.Empty<string>();
            if (guids.Length == 0)
            {
                Assert.Ignore("Nothing built yet: run Game › Build All first.");
            }

            var keys = new HashSet<string>(Entries().Select(row => row[0]));
            var missing = new SortedSet<string>();
            foreach (string path in guids.Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                foreach (UnityEngine.Object asset in Targets(path))
                {
                    SerializedProperty property = new SerializedObject(asset).GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.String && IsKeyField(property.name)
                            && KeyShape.IsMatch(property.stringValue) && !keys.Contains(property.stringValue))
                        {
                            missing.Add($"{property.stringValue} ({path} › {asset.GetType().Name}.{property.propertyPath})");
                        }
                    }
                }
            }

            Assert.That(missing, Is.Empty, string.Join("\n", missing));
        }

        // A key field: listed by name, or any field whose name ends with "Key" (_textKey, _effectFormatKey, _key...).
        private static bool IsKeyField(string name)
        {
            return KeyFields.Contains(name) || name.EndsWith("Key", StringComparison.Ordinal) || name == "_key";
        }

        // The asset itself, or every component of a prefab (LocalizedLabel keys).
        private static IEnumerable<UnityEngine.Object> Targets(string path)
        {
            if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                return prefab != null
                    ? prefab.GetComponentsInChildren<Component>(true).Where(component => component != null).Cast<UnityEngine.Object>()
                    : Enumerable.Empty<UnityEngine.Object>();
            }

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            return asset != null ? new UnityEngine.Object[] { asset } : Enumerable.Empty<UnityEngine.Object>();
        }

        // Rows with a key, without comment rows (a key starting with '#').
        private static IEnumerable<List<string>> Entries()
        {
            return ReadRows().Skip(1).Where(row => row[0].Trim().Length > 0 && !row[0].Trim().StartsWith("#", StringComparison.Ordinal));
        }

        private static string Cell(List<string> row, int column)
        {
            return column < row.Count ? row[column] : string.Empty;
        }

        private static HashSet<string> Placeholders(string text)
        {
            return new HashSet<string>(Placeholder.Matches(text).Cast<Match>().Select(match => match.Value));
        }

        private static List<List<string>> ReadRows()
        {
            Assert.That(File.Exists(TablePath), Is.True, $"Missing localization table {TablePath}.");
            return ParseCsv(File.ReadAllText(TablePath, Encoding.UTF8));
        }

        // RFC 4180, as Core's CSV backend reads it: quoted cells may hold commas, newlines and doubled quotes.
        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = text.Length > 0 && text[0] == '\uFEFF' ? 1 : 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c != '"')
                    {
                        cell.Append(c);
                    }
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else if (c == '"' && cell.Length == 0)
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\n')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else if (c != '\r')
                {
                    cell.Append(c);
                }
            }

            Assert.That(quoted, Is.False, "A quoted cell is not closed.");
            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
