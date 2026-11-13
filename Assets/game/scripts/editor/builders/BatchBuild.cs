using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Command-line entry of <c>Game › Build All</c>:
    /// <c>Unity -batchmode -nographics -quit -projectPath . -executeMethod Game.Editor.Builders.BatchBuild.BuildAll</c>.
    /// Runs the same steps without dialogs and exits with code 1 when a step throws, so a script can tell a failed build.
    /// </summary>
    public static class BatchBuild
    {
        public static void BuildAll()
        {
            try
            {
                ProjectSettingsBuilder.Build();
                AssetBuilder.Build();
                PrefabBuilder.Build();
                SceneBuilder.Build();
                ProjectSettingsBuilder.BuildSceneList();
                AssetDatabase.SaveAssets();
                Debug.Log("[Game] Batch Build All finished.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
