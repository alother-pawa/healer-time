using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Enforces the folder rule under <c>Assets/game</c>: folders are lowercase kebab-case, except Unity's special folders
    /// (<c>Editor</c>, <c>Resources</c>, <c>Plugins</c>, <c>StreamingAssets</c>). Paths inside a <c>Resources</c> folder are
    /// not checked, because loaders fix them (Core reads <c>Resources/Paowa/...</c>).
    /// </summary>
    public sealed class ProjectLayoutTests
    {
        private const string GameRoot = "Assets/game";
        private const string ResourcesFolder = "Resources";
        private static readonly Regex KebabCase = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

        private static readonly HashSet<string> SpecialFolders = new HashSet<string>(StringComparer.Ordinal)
        {
            "Editor", ResourcesFolder, "Plugins", "StreamingAssets",
        };

        [Test]
        public void FoldersAreKebabCase()
        {
            var violations = new List<string>();
            Check(GameRoot, violations);
            Assert.That(violations, Is.Empty, string.Join("\n", violations));
        }

        private static void Check(string folder, List<string> violations)
        {
            foreach (string child in Directory.GetDirectories(folder).OrderBy(path => path, StringComparer.Ordinal))
            {
                string name = Path.GetFileName(child);
                if (!SpecialFolders.Contains(name) && !KebabCase.IsMatch(name))
                {
                    violations.Add($"{child.Replace('\\', '/')}: use lowercase kebab-case");
                }

                if (name != ResourcesFolder)
                {
                    Check(child, violations);
                }
            }
        }
    }
}
