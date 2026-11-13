using System.Collections.Generic;
using System.Linq;
using Game.Editor.Builders;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Editor
{
    /// <summary>
    /// The built scenes and prefabs follow the standard trees of Docs/STRUCTURE.md: scene root groups, the sections of
    /// every popup and of every screen canvas, templates kept inactive. Skipped until the project is built.
    /// </summary>
    public sealed class HierarchyConventionTests
    {
        private static readonly string[] PopupChildren = { "Backdrop", "Panel" };
        private static readonly string[] PanelChildren = { "Frame", "Header", "Body", "Footer", "Templates" };
        private static readonly string[] CanvasChildren = { "Background", "Screen", "Overlay" };
        private static readonly string[] ScreenSections = { "Header", "Body", "Footer", "Templates" };
        private static readonly string[] SceneGroups = { "Systems", "Cameras", "World", "UI", "Audio" };

        private static readonly string[] ScreenCanvases =
        {
            BuildPaths.SplashCanvas, BuildPaths.MainMenuCanvas, BuildPaths.GameplayCanvas, BuildPaths.FirstLaunchCanvas,
        };

        private static readonly string[] Scenes =
        {
            BuildPaths.SceneBoot, BuildPaths.SceneUI, BuildPaths.SceneSplash, BuildPaths.SceneMainMenu, BuildPaths.SceneGameplay,
            BuildPaths.SceneFirstLaunch, BuildPaths.SceneArtPreview,
        };

        [Test]
        public void EveryPopupHasTheStandardSections()
        {
            string[] popups = AssetDatabase.FindAssets("t:Prefab Popup_", new[] { BuildPaths.PrefabsPopups })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (popups.Length == 0)
            {
                Assert.Ignore("Nothing built yet: run Game › Build All first.");
            }

            var problems = new List<string>();
            foreach (string path in popups)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                ExpectChildren(path, root.transform, PopupChildren, problems);
                Transform panel = root.transform.Find("Panel");
                if (panel != null)
                {
                    ExpectChildren(path + " Panel", panel, PanelChildren, problems);
                    ExpectInactive(path, panel.Find("Templates"), problems);
                    if (panel.Find("Header/TitleLabel") == null)
                    {
                        problems.Add($"{path}: Panel/Header/TitleLabel missing");
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EveryScreenCanvasHasTheStandardSections()
        {
            var problems = new List<string>();
            int found = 0;
            foreach (string path in ScreenCanvases)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                {
                    continue;
                }

                found++;
                ExpectChildren(path, root.transform, CanvasChildren, problems);
                Transform screen = root.transform.Find("Screen");
                if (screen != null)
                {
                    ExpectChildren(path + " Screen", screen, ScreenSections, problems);
                    ExpectInactive(path, screen.Find("Templates"), problems);
                }
            }

            if (found == 0)
            {
                Assert.Ignore("Nothing built yet: run Game › Build All first.");
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EverySceneUsesTheRootGroups()
        {
            var problems = new List<string>();
            int found = 0;
            foreach (string path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    continue;
                }

                found++;
                // A scene already open stays open (closing the last loaded scene is not allowed).
                bool wasOpen = SceneManager.GetSceneByPath(path).isLoaded;
                Scene scene = wasOpen ? SceneManager.GetSceneByPath(path) : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    string[] roots = scene.GetRootGameObjects().Select(go => go.name).ToArray();
                    foreach (string name in roots.Where(name => !SceneGroups.Contains(name)))
                    {
                        problems.Add($"{path}: '{name}' at the scene root (put it in one of {string.Join(", ", SceneGroups)})");
                    }

                    foreach (string required in SceneGroups.Where(group => group != "World" && !roots.Contains(group)))
                    {
                        problems.Add($"{path}: group '{required}' missing");
                    }
                }
                finally
                {
                    if (!wasOpen)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            if (found == 0)
            {
                Assert.Ignore("Nothing built yet: run Game › Build All first.");
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // The listed children come first, in this order (more may follow, e.g. a popup's own extras).
        private static void ExpectChildren(string where, Transform parent, string[] expected, List<string> problems)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                string actual = i < parent.childCount ? parent.GetChild(i).name : "(none)";
                if (actual != expected[i])
                {
                    problems.Add($"{where}: child {i} is '{actual}', expected '{expected[i]}'");
                    return;
                }
            }
        }

        private static void ExpectInactive(string where, Transform templates, List<string> problems)
        {
            if (templates != null && templates.gameObject.activeSelf)
            {
                problems.Add($"{where}: Templates must be inactive");
            }
        }
    }
}
