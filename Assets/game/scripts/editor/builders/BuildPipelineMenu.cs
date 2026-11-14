using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Paowa.Core.Foundation.Bootstrap;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Player builds for Windows (Steam) and the web (itch.io): menu <c>Game › Build › Windows | Web (Development | Release)</c>
    /// and the command line entry <see cref="BuildFromCommandLine"/> (platform = the active build target, set by
    /// <c>-buildTarget</c>). The project must already be built (<c>Game › Build All</c>): the enabled Build
    /// Settings scenes must exist and start with Boot. Before <c>BuildPipeline.BuildPlayer</c> the build stamp
    /// <c>Resources/Paowa/BuildInfo</c> is written (build number from <c>BUILD_NUMBER</c> or a local counter in
    /// <c>Library/</c>, commit from <c>git rev-parse --short HEAD</c>, UTC date); it is removed (or its earlier text put
    /// back) after the build, success or not. The version is <c>PlayerSettings.bundleVersion</c>. Output, unless
    /// <c>-output</c> says otherwise: <c>Builds/Windows/&lt;Development|Release&gt;/&lt;Product&gt;.exe</c>, or the folder
    /// <c>Builds/Web/&lt;Development|Release&gt;</c> holding <c>index.html</c> (its loader syncs <c>persistentDataPath</c> to
    /// the browser's storage after every write, so saves survive a reload). In batch mode the editor exits with 0 on
    /// success and 1 on any failure.
    /// </summary>
    public static class BuildPipelineMenu
    {
        private const string Menu = "Game/Build/";
        private const string LogPrefix = "[Game] ";
        private const string CounterPath = "Library/Game/BuildNumber.txt";
        private const int GitTimeoutMilliseconds = 5000;

        private const string WebConfigStart = "var config = {";
        private const string WebAutoSync = "autoSyncPersistentDataPath: true,";

        [MenuItem(Menu + "Windows (Development)", priority = 40)]
        private static void BuildDevelopmentMenu()
        {
            BuildFromMenu(true, false);
        }

        [MenuItem(Menu + "Windows (Release)", priority = 41)]
        private static void BuildReleaseMenu()
        {
            BuildFromMenu(false, false);
        }

        [MenuItem(Menu + "Web (Development)", priority = 42)]
        private static void BuildWebDevelopmentMenu()
        {
            BuildFromMenu(true, true);
        }

        [MenuItem(Menu + "Web (Release)", priority = 43)]
        private static void BuildWebReleaseMenu()
        {
            BuildFromMenu(false, true);
        }

        /// <summary>
        /// Command line entry:
        /// <c>Unity -batchmode -nographics -projectPath . -buildTarget Win64|WebGL -executeMethod
        /// Game.Editor.Builders.BuildPipelineMenu.BuildFromCommandLine -release|-development [-output &lt;dir&gt;] -logFile build.log</c>.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            bool succeeded = false;
            try
            {
                BuildCommandLine options = BuildCommandLine.Parse(Environment.GetCommandLineArgs());
                if (options.Error != null)
                {
                    Debug.LogError(LogPrefix + "Build: " + options.Error);
                }
                else
                {
                    bool web = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                    succeeded = Build(options.Development, web, options.OutputDirectory, out _);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(succeeded ? 0 : 1);
            }
        }

        private static void BuildFromMenu(bool development, bool web)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Game build", "Stop Play Mode and wait for compilation first.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (Build(development, web, null, out string executable))
            {
                EditorUtility.RevealInFinder(executable);
            }
            else
            {
                EditorUtility.DisplayDialog("Game build", "The build failed. See the Console.", "OK");
            }
        }

        /// <summary>Builds the Windows or web player; returns false (logged) on any failure.</summary>
        private static bool Build(bool development, bool web, string outputDirectory, out string executable)
        {
            string config = development ? "Development" : "Release";
            string folder = string.IsNullOrEmpty(outputDirectory)
                ? Path.Combine("Builds", web ? "Web" : "Windows", config)
                : outputDirectory;
            executable = web ? folder : Path.Combine(folder, PlayerSettings.productName + ".exe");

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            string invalid = ValidateScenes(scenes);
            if (invalid != null)
            {
                Debug.LogError(LogPrefix + "Build: " + invalid + " Run Game › Build All first.");
                return false;
            }

            string buildNumber = BuildInfoFile.ResolveBuildNumber(Environment.GetEnvironmentVariable("BUILD_NUMBER"),
                File.Exists(CounterPath) ? File.ReadAllText(CounterPath) : null, out string nextCounter);
            if (nextCounter != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CounterPath));
                File.WriteAllText(CounterPath, nextCounter);
            }

            string text = BuildInfoFile.Compose(buildNumber, GitCommit(), DateTime.UtcNow);
            string logLine = BuildInfo.Parse(PlayerSettings.productName, PlayerSettings.bundleVersion, text).LogLine;
            BuildInfoStamp stamp = BuildInfoStamp.Write(BuildPaths.BuildInfoFile, text);
            try
            {
                AssetDatabase.ImportAsset(BuildPaths.BuildInfoFile, ImportAssetOptions.ForceSynchronousImport);
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = executable,
                    target = web ? BuildTarget.WebGL : BuildTarget.StandaloneWindows64,
                    targetGroup = web ? BuildTargetGroup.WebGL : BuildTargetGroup.Standalone,
                    options = development ? BuildOptions.Development : BuildOptions.None,
                };
                Debug.Log(LogPrefix + $"Building {logLine} ({config}) to {executable}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError(LogPrefix + $"Build {summary.result}: {summary.totalErrors} error(s). {logLine}");
                    return false;
                }

                if (web && !EnableWebAutoSync(Path.Combine(folder, "index.html")))
                {
                    return false;
                }

                Debug.Log(LogPrefix + $"Built {logLine} ({config}): {executable}, {summary.totalSize / (1024 * 1024)} MB, "
                    + $"{summary.totalTime.TotalSeconds:0} s");
                return true;
            }
            finally
            {
                stamp.Restore();
                AssetDatabase.Refresh();
            }
        }

        // The web loader keeps files written under persistentDataPath only in memory unless it syncs them to the browser's
        // storage; the default template does not ask for that, so the setting goes into its loader config.
        private static bool EnableWebAutoSync(string indexPath)
        {
            string html = File.Exists(indexPath) ? File.ReadAllText(indexPath) : string.Empty;
            if (html.Contains(WebAutoSync))
            {
                return true;
            }

            int at = html.IndexOf(WebConfigStart, StringComparison.Ordinal);
            if (at < 0)
            {
                Debug.LogError(LogPrefix + $"Build: no '{WebConfigStart}' in {indexPath}; saves would not survive a reload.");
                return false;
            }

            at += WebConfigStart.Length;
            File.WriteAllText(indexPath, html.Substring(0, at) + "\n        " + WebAutoSync + html.Substring(at));
            return true;
        }

        // Enabled scenes exist and Boot comes first (it loads the UI scene, then the first screen).
        private static string ValidateScenes(string[] scenes)
        {
            if (scenes.Length == 0)
            {
                return "Build Settings has no enabled scene.";
            }

            string missing = scenes.FirstOrDefault(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null);
            if (missing != null)
            {
                return $"Scene {missing} does not exist.";
            }

            return scenes[0] == BuildPaths.SceneBoot ? null : $"The first scene is {scenes[0]}, expected {BuildPaths.SceneBoot}.";
        }

        // Short hash of HEAD; empty (no commit line) when git is missing, the folder is no repository or git hangs.
        private static string GitCommit()
        {
            try
            {
                var start = new ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                };
                using (Process git = Process.Start(start))
                {
                    if (git == null)
                    {
                        return string.Empty;
                    }

                    // The output is one short line, so waiting before reading cannot fill the pipe.
                    if (!git.WaitForExit(GitTimeoutMilliseconds))
                    {
                        git.Kill();
                        return string.Empty;
                    }

                    return git.ExitCode == 0 ? git.StandardOutput.ReadToEnd().Trim() : string.Empty;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "git rev-parse failed; the build has no commit stamp. " + exception.Message);
                return string.Empty;
            }
        }
    }
}
