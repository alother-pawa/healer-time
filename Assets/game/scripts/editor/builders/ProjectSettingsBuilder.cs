using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Project-level setup: sprite import from the art spec (<see cref="ArtImport"/>), one Sprite Atlas per sprite pack
    /// (<see cref="AtlasBuilder"/>), animation clips of the sprite sheets (<see cref="AnimationBuilder"/>), sound import from
    /// the sound spec (<see cref="AudioImport"/>),
    /// the Japanese fallback font, Player Settings of a desktop
    /// game (identity, borderless fullscreen, resizable window, single instance, no Unity splash screen; for the web build
    /// gzip with decompression fallback and a 16:9 canvas) and the Build
    /// Settings scene list. Active Input Handling and the render pipeline are not scriptable; this step only reports
    /// when Active Input Handling is not "Input System Package (New)".
    /// </summary>
    internal static class ProjectSettingsBuilder
    {
        private const string LogPrefix = "[Game] ";

        /// <summary>Scenes in build order (index 0 first).</summary>
        private static readonly string[] BuildScenes =
        {
            BuildPaths.SceneBoot, BuildPaths.SceneUI, BuildPaths.SceneSplash, BuildPaths.SceneMainMenu, BuildPaths.SceneGameplay,
        };

        /// <summary>Sprite import, fallback font and player settings.</summary>
        internal static void Build()
        {
            ArtSpec art = ArtSpec.Load();
            ArtImport.Import(art);
            AtlasBuilder.Build();
            AnimationBuilder.Build(art);
            AudioImport.Import();
            WireJapaneseFont();
            ApplyPlayerSettings();
            ReportInputHandling();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Replaces the Build Settings scene list with the game scenes.</summary>
        internal static void BuildSceneList()
        {
            string missing = BuildScenes.FirstOrDefault(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null);
            if (missing != null)
            {
                throw new InvalidOperationException($"Scene {missing} does not exist. Run 'Scenes' first.");
            }

            EditorBuildSettings.scenes = BuildScenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        // Desktop defaults: borderless fullscreen at the monitor's resolution, a resizable window when windowed, one
        // running copy, the player loop paused while unfocused, and no Unity splash (the game shows its own logos).
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = ProjectIdentity.CompanyName;
            PlayerSettings.productName = ProjectIdentity.ProductName;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.defaultScreenWidth = ProjectIdentity.DefaultScreenWidth;
            PlayerSettings.defaultScreenHeight = ProjectIdentity.DefaultScreenHeight;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.forceSingleInstance = true;

            // Web (itch.io): gzip files that the loader can unpack itself, so a host that sends no Content-Encoding header
            // still serves them; a 16:9 canvas.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;

            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            if (PlayerSettings.SplashScreen.show)
            {
                // Reads back true when the editor's license does not allow turning the splash off.
                Debug.LogWarning(LogPrefix + "The Unity splash screen is still on: this license does not allow disabling it. "
                    + "See Docs/SETUP_CHECKLIST.md §4.");
            }
        }

        // Active Input Handling needs an editor restart, so it is a manual setting; the scripting defines tell which
        // backends are compiled in.
        private static void ReportInputHandling()
        {
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError(LogPrefix + "Active Input Handling does not include the Input System package; the game reads no "
                + "input. Set it to 'Input System Package (New)' (Docs/SETUP_CHECKLIST.md §4).");
#elif ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogWarning(LogPrefix + "Active Input Handling is 'Both'; the base expects 'Input System Package (New)' "
                + "(Docs/SETUP_CHECKLIST.md §4).");
#endif
        }

        /// <summary>
        /// Adds the Japanese font asset to the fallbacks of TMP's default font (when both exist), so every label shows
        /// Japanese glyphs without its own font.
        /// </summary>
        internal static void WireJapaneseFont()
        {
            TMP_FontAsset defaultFont = TMP_Settings.instance != null ? TMP_Settings.defaultFontAsset : null;
            if (defaultFont == null)
            {
                Debug.LogWarning(LogPrefix + "TextMeshPro has no default font yet; the Japanese fallback font is not wired.");
                return;
            }

            var japanese = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.JapaneseFont);
            if (japanese == null)
            {
                Debug.LogWarning(LogPrefix + $"No font asset at {BuildPaths.JapaneseFont}: Japanese text shows missing glyphs. "
                    + "See Docs/SETUP_CHECKLIST.md §2.");
                return;
            }

            defaultFont.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!defaultFont.fallbackFontAssetTable.Contains(japanese))
            {
                defaultFont.fallbackFontAssetTable.Add(japanese);
                EditorUtility.SetDirty(defaultFont);
            }
        }
    }
}
