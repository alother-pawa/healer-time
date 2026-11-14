using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The game's text font: Fibberish (a pixel font, CC BY 3.0) as a Dynamic SDF font asset made from
    /// <c>fonts/Fibberish.ttf</c>, its printable ASCII already in the atlas, with TMP's default font then the Japanese font
    /// as fallbacks for the glyphs it lacks (accents, symbols, Japanese). Its outline material draws the dark edge of HUD
    /// and world texts. Both are generated under <c>data/fonts</c> by the project step; without the source font every text
    /// keeps TMP's default font.
    /// </summary>
    internal static class GameFont
    {
        private const string LogPrefix = "[Game] ";
        private const int SamplingPointSize = 64;
        private const int AtlasPadding = 8;
        private const int AtlasSize = 1024;
        private const float OutlineWidth = 0.2f;
        private const float FaceDilate = 0.15f;

        /// <summary>The font every builder text uses (TMP's default until the project step has made it).</summary>
        internal static TMP_FontAsset Asset =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.GameFont) ?? TMP_Settings.defaultFontAsset;

        /// <summary>The font's material with a dark outline; null until the project step has made it.</summary>
        internal static Material Outline => AssetDatabase.LoadAssetAtPath<Material>(BuildPaths.GameFontOutline);

        /// <summary>Makes (or refreshes) the font asset, its fallbacks and its outline material.</summary>
        internal static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(BuildPaths.GameFontSource);
            if (source == null)
            {
                Debug.LogWarning(LogPrefix + $"No font at {BuildPaths.GameFontSource}; texts keep TMP's default font.");
                return;
            }

            BuildUtility.EnsureFolder(BuildUtility.FolderOf(BuildPaths.GameFont));
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.GameFont) ?? Create(source);
            if (font == null)
            {
                return;
            }

            var fallbacks = new List<TMP_FontAsset>();
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset != font)
            {
                fallbacks.Add(TMP_Settings.defaultFontAsset);
            }

            var japanese = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.JapaneseFont);
            if (japanese != null)
            {
                fallbacks.Add(japanese);
            }

            font.fallbackFontAssetTable = fallbacks;
            EditorUtility.SetDirty(font);

            Material outline = AssetDatabase.LoadAssetAtPath<Material>(BuildPaths.GameFontOutline);
            if (outline == null)
            {
                outline = new Material(font.material);
                AssetDatabase.CreateAsset(outline, BuildPaths.GameFontOutline);
            }
            else
            {
                outline.CopyPropertiesFromMaterial(font.material);
            }

            outline.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outline.SetFloat(ShaderUtilities.ID_OutlineWidth, OutlineWidth);
            outline.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            outline.SetFloat(ShaderUtilities.ID_FaceDilate, FaceDilate);
            EditorUtility.SetDirty(outline);
            AssetDatabase.SaveAssets();
        }

        // A Dynamic SDF font asset whose atlas texture and material live inside it (as TMP's own Create menu stores them).
        private static TMP_FontAsset Create(Font source)
        {
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
            if (font == null)
            {
                Debug.LogError(LogPrefix + $"TextMesh Pro could not make a font asset from {BuildPaths.GameFontSource}.");
                return null;
            }

            font.name = System.IO.Path.GetFileNameWithoutExtension(BuildPaths.GameFont);
            AssetDatabase.CreateAsset(font, BuildPaths.GameFont);
            font.atlasTextures[0].name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);

            var ascii = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++)
            {
                ascii.Append(c);
            }

            font.TryAddCharacters(ascii.ToString());
            EditorUtility.SetDirty(font);
            return font;
        }
    }
}
