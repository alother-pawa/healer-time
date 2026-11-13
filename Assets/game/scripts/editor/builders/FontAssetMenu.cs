using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Menu <c>Game › Fonts › Create Japanese Font Asset</c>: makes <c>fonts/NotoSansJP-SDF.asset</c> from
    /// <c>fonts/NotoSansJP-Regular.ttf</c> as a Dynamic SDF font (glyphs are added at runtime, multi-atlas, 2048² atlas,
    /// padding 9, SDFAA) and adds it to the fallbacks of TMP's default font. An existing font asset is left alone (delete
    /// it to make a new one). Without the .ttf it only explains where to put it.
    /// </summary>
    internal static class FontAssetMenu
    {
        private const string LogPrefix = "[Game] ";
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 2048;

        [MenuItem("Game/Fonts/Create Japanese Font Asset", priority = 60)]
        private static void CreateJapaneseFont()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.JapaneseFont) != null)
            {
                Debug.Log(LogPrefix + $"{BuildPaths.JapaneseFont} already exists; wiring it as the fallback font.");
                ProjectSettingsBuilder.WireJapaneseFont();
                AssetDatabase.SaveAssets();
                return;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(BuildPaths.JapaneseFontSource);
            if (source == null)
            {
                Debug.LogWarning(LogPrefix + $"No font at {BuildPaths.JapaneseFontSource}. Download Noto Sans JP (Google Fonts, OFL) and copy "
                    + "NotoSansJP-Regular.ttf there, then run this menu again (Docs/SETUP_CHECKLIST.md §2).");
                return;
            }

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            if (font == null)
            {
                Debug.LogError(LogPrefix + "TextMesh Pro could not create the font asset; use the manual steps of Docs/SETUP_CHECKLIST.md §2.");
                return;
            }

            // The atlas texture and material live inside the font asset, as TMP's own Create menu stores them.
            font.name = "NotoSansJP-SDF";
            AssetDatabase.CreateAsset(font, BuildPaths.JapaneseFont);
            if (font.atlasTextures != null && font.atlasTextures.Length > 0 && font.atlasTextures[0] != null)
            {
                font.atlasTextures[0].name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            }

            if (font.material != null)
            {
                font.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
            }

            AssetDatabase.SaveAssets();
            ProjectSettingsBuilder.WireJapaneseFont();
            AssetDatabase.SaveAssets();
            Debug.Log(LogPrefix + $"Created {BuildPaths.JapaneseFont} (Dynamic SDF) and added it to the default font's fallbacks.");
        }
    }
}
