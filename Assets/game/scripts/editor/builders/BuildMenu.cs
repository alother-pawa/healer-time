using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Menu <c>Game</c>: builds the whole project from the values in the builders (project settings → assets →
    /// prefabs → scenes → Build Settings) or one step at a time. The builders are the only description of the scenes,
    /// prefabs and assets. Every step overwrites what it built before, so it can be re-run safely.
    /// Steps that create objects first offer to save modified scenes, because they replace the open scene.
    /// </summary>
    internal static class BuildMenu
    {
        private const string Menu = "Game/";
        private const string Steps = Menu + "Steps/";
        private const string Title = "Game builders";
        private const string LogPrefix = "[Game] ";

        [MenuItem(Menu + "Build All", priority = 0)]
        private static void BuildAll()
        {
            Run("Build All", true, () =>
            {
                Progress("Project settings", 0f);
                ProjectSettingsBuilder.Build();
                Progress("Assets", 0.2f);
                AssetBuilder.Build();
                Progress("Prefabs", 0.4f);
                PrefabBuilder.Build();
                Progress("Scenes", 0.7f);
                SceneBuilder.Build();
                Progress("Build Settings", 0.95f);
                ProjectSettingsBuilder.BuildSceneList();
            });
        }

        [MenuItem(Steps + "1. Project Settings (Player, Sprites)", priority = 20)]
        private static void BuildProjectSettings()
        {
            Run("Project Settings", false, ProjectSettingsBuilder.Build);
        }

        [MenuItem(Steps + "2. Assets", priority = 21)]
        private static void BuildAssets()
        {
            Run("Assets", false, AssetBuilder.Build);
        }

        // Scenes hold prefab instances with scene-only references; rebuilding them right after the prefabs keeps those
        // references valid whatever the prefab overwrite did to object ids.
        [MenuItem(Steps + "3. Prefabs (and Scenes)", priority = 22)]
        private static void BuildPrefabs()
        {
            // One run: a second save check would ask about the prefab step's unsaved scratch scene.
            Run("Prefabs and Scenes", true, () =>
            {
                PrefabBuilder.Build();
                SceneBuilder.Build();
            });
        }

        [MenuItem(Steps + "4. Scenes", priority = 23)]
        private static void BuildScenes()
        {
            Run("Scenes", true, SceneBuilder.Build);
        }

        [MenuItem(Steps + "5. Build Settings", priority = 24)]
        private static void BuildSceneList()
        {
            Run("Build Settings", false, ProjectSettingsBuilder.BuildSceneList);
        }

        // createsObjects: the step replaces the open scene and creates UI text, so it needs saved scenes and TMP resources.
        private static void Run(string name, bool createsObjects, Action build)
        {
            if (createsObjects && (!TmpReady() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()))
            {
                return;
            }

            try
            {
                build();
                AssetDatabase.SaveAssets();
                Debug.Log(LogPrefix + name + " finished.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(Title, $"{name} failed:\n{exception.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool TmpReady()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
            {
                return true;
            }

            Debug.LogError(LogPrefix + "TextMeshPro has no default font asset. Run Window › TextMeshPro › Import TMP Essential "
                + "Resources, then build again.");
            return false;
        }

        private static void Progress(string step, float fraction)
        {
            EditorUtility.DisplayProgressBar(Title, step, fraction);
        }
    }
}
