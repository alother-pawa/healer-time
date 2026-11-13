using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Enforces the module rule: an assembly under scripts/systems/ or scripts/objects/ may reference Core, Unity,
    /// third-party assemblies, Game.Contracts and Game.Foundation, but never another game assembly.
    /// </summary>
    public sealed class ModuleIsolationTests
    {
        private const string GamePrefix = "Game.";
        private const string GuidPrefix = "GUID:";
        private const string ScriptsRoot = "Assets/game/scripts";

        private static readonly string[] ModuleRoots = { ScriptsRoot + "/systems", ScriptsRoot + "/objects" };
        private static readonly HashSet<string> SharedAssemblies = new HashSet<string>(StringComparer.Ordinal)
        {
            "Game.Contracts",
            "Game.Foundation",
        };

        [Test]
        public void ModulesReferenceOnlySharedGameAssemblies()
        {
            var violations = new List<string>();
            foreach (string path in ModuleAsmdefPaths())
            {
                AsmdefData asmdef = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(path));
                foreach (string reference in asmdef.references ?? Array.Empty<string>())
                {
                    if (reference.StartsWith(GuidPrefix, StringComparison.Ordinal))
                    {
                        violations.Add($"{asmdef.name} references an assembly by GUID ({reference}); use its name so this check can read it.");
                    }
                    else if (reference.StartsWith(GamePrefix, StringComparison.Ordinal) && !SharedAssemblies.Contains(reference))
                    {
                        violations.Add($"{asmdef.name} references {reference}; talk through Contracts interfaces or events instead.");
                    }
                }
            }

            Assert.That(violations, Is.Empty, string.Join("\n", violations));
        }

        [Test]
        public void SharedAssembliesDoNotReferenceModules()
        {
            var violations = new List<string>();
            foreach (string path in Directory.GetFiles(ScriptsRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                AsmdefData asmdef = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(path));
                if (!SharedAssemblies.Contains(asmdef.name))
                {
                    continue;
                }

                violations.AddRange((asmdef.references ?? Array.Empty<string>())
                    .Where(reference => reference.StartsWith(GamePrefix, StringComparison.Ordinal) && !SharedAssemblies.Contains(reference))
                    .Select(reference => $"{asmdef.name} references module {reference}."));
            }

            Assert.That(violations, Is.Empty, string.Join("\n", violations));
        }

        private static IEnumerable<string> ModuleAsmdefPaths()
        {
            return ModuleRoots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.GetFiles(root, "*.asmdef", SearchOption.AllDirectories));
        }

        [Serializable]
        private sealed class AsmdefData
        {
            public string name;
            public string[] references;
        }
    }
}
