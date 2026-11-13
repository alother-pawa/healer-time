using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Imports every PNG under <c>art/sprites</c> from <see cref="ArtSpec"/>: single UI or world sprites with the listed
    /// 9-slice border and pivot, animation sheets sliced into one sprite per frame (named <c>&lt;sheet&gt;_&lt;tag&gt;_&lt;index&gt;</c>,
    /// ids kept across rebuilds so clips keep their references). Every texture: one world unit per spec pixels-per-unit, no
    /// mipmaps (UI draws near 1:1), clamped edges, colour dilated under transparency; packed sprites stay uncompressed
    /// (their atlas page is compressed), full-screen backgrounds are compressed. A PNG missing from the spec, or a spec
    /// entry without its PNG, stops the build; an image whose size differs from the spec is reported (art drawn at the
    /// wrong size), and still imported.
    /// </summary>
    internal static class ArtImport
    {
        private const string LogPrefix = "[Game] ";

        internal static void Import(ArtSpec spec)
        {
            string prefix = BuildPaths.Sprites + "/";
            var files = AssetDatabase.FindAssets("t:Texture2D", new[] { BuildPaths.Sprites })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(path => path.Substring(prefix.Length, path.Length - prefix.Length - ".png".Length), path => path);
            var sprites = spec.sprites.ToDictionary(sprite => sprite.path);
            var sheets = spec.sheets.ToDictionary(sheet => sheet.path);

            string[] unlisted = files.Keys.Where(name => !sprites.ContainsKey(name) && !sheets.ContainsKey(name)).ToArray();
            string[] missing = sprites.Keys.Concat(sheets.Keys).Where(name => !files.ContainsKey(name)).ToArray();
            if (unlisted.Length > 0 || missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "art/sprites and art/art-spec.json differ. Add the image to Tools/ShapeGen/generate_shapes.py and run it. "
                    + $"Not in the spec: {string.Join(", ", unlisted)}. Missing files: {string.Join(", ", missing)}.");
            }

            // One batch: the reimports run together when editing stops, also after a throw.
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (SpriteSpec sprite in spec.sprites)
                {
                    ConfigureSingle(files[sprite.path], sprite, spec.pixelsPerUnit);
                }

                foreach (SheetSpec sheet in spec.sheets)
                {
                    ConfigureTexture((TextureImporter)AssetImporter.GetAtPath(files[sheet.path]), SpriteImportMode.Multiple,
                        spec.pixelsPerUnit, TextureImporterCompression.Uncompressed);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (SheetSpec sheet in spec.sheets)
            {
                Slice(files[sheet.path], sheet);
            }

            ReportSizes(spec, files);
        }

        private static void ConfigureSingle(string path, SpriteSpec sprite, int pixelsPerUnit)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            TextureImporterCompression compression = sprite.path.StartsWith("backgrounds/", StringComparison.Ordinal)
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.Uncompressed;
            var border = new Vector4(sprite.border[0], sprite.border[1], sprite.border[2], sprite.border[3]);
            var pivot = new Vector2(sprite.pivot[0], sprite.pivot[1]);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool unchanged = IsConfigured(importer, SpriteImportMode.Single, pixelsPerUnit, compression)
                             && settings.spriteMeshType == SpriteMeshType.FullRect && settings.spriteBorder == border
                             && settings.spriteAlignment == (int)SpriteAlignment.Custom && settings.spritePivot == pivot;
            if (unchanged)
            {
                return;
            }

            ConfigureTexture(importer, SpriteImportMode.Single, pixelsPerUnit, compression);
            // Read again: the texture settings also carry the properties above; a stale copy would undo them.
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static bool IsConfigured(TextureImporter importer, SpriteImportMode mode, int pixelsPerUnit,
            TextureImporterCompression compression)
        {
            return importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == mode
                   && Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) && importer.filterMode == FilterMode.Bilinear
                   && importer.wrapMode == TextureWrapMode.Clamp && !importer.mipmapEnabled && importer.alphaIsTransparency
                   && importer.textureCompression == compression;
        }

        private static void ConfigureTexture(TextureImporter importer, SpriteImportMode mode, int pixelsPerUnit,
            TextureImporterCompression compression)
        {
            if (IsConfigured(importer, mode, pixelsPerUnit, compression))
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = compression;
            importer.SaveAndReimport();
        }

        // One rect per frame, rows from the top of the texture (Unity's rects start at the bottom-left).
        private static void Slice(string path, SheetSpec sheet)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);

            int height = sheet.frameHeight * sheet.tags.Length;
            var pivot = new Vector2(sheet.pivot[0], sheet.pivot[1]);
            var rects = new List<SpriteRect>();
            for (int row = 0; row < sheet.tags.Length; row++)
            {
                TagSpec tag = sheet.tags[row];
                for (int frame = 0; frame < tag.frames; frame++)
                {
                    string name = FrameName(sheet, tag, frame);
                    rects.Add(new SpriteRect
                    {
                        name = name,
                        spriteID = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                        rect = new Rect(frame * sheet.frameWidth, height - (row + 1) * sheet.frameHeight, sheet.frameWidth, sheet.frameHeight),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        border = Vector4.zero,
                    });
                }
            }

            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>Name of one frame's sprite: <c>hero_run_3</c>.</summary>
        internal static string FrameName(SheetSpec sheet, TagSpec tag, int frame)
        {
            return $"{sheet.Name}_{tag.name}_{frame}";
        }

        private static void ReportSizes(ArtSpec spec, Dictionary<string, string> files)
        {
            var wrong = new List<string>();
            foreach (SpriteSpec sprite in spec.sprites)
            {
                Check(files[sprite.path], sprite.width, sprite.height, wrong);
            }

            foreach (SheetSpec sheet in spec.sheets)
            {
                Check(files[sheet.path], sheet.frameWidth * sheet.Columns, sheet.frameHeight * sheet.tags.Length, wrong);
            }

            if (wrong.Count > 0)
            {
                Debug.LogWarning(LogPrefix + "Images not at their spec size (Docs/ART_SPEC.md):\n" + string.Join("\n", wrong));
            }
        }

        private static void Check(string path, int width, int height, List<string> wrong)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int actualWidth, out int actualHeight);
            if (actualWidth != width || actualHeight != height)
            {
                wrong.Add($"  {path}: {actualWidth}x{actualHeight}, spec {width}x{height}");
            }
        }
    }
}
