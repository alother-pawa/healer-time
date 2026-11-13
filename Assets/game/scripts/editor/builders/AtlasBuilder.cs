using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.Builders
{
    /// <summary>
    /// One Sprite Atlas (V2) per sprite pack. A pack is a folder under <c>art/sprites</c> that holds PNG files directly
    /// (<c>ui-common</c>, <c>ui-glyphs</c>, <c>screens/settings</c>, <c>game/props</c>...); the atlas packs the folder, so
    /// a sprite added there joins its pack on the next build (animation sheets too: their frames are packed). Not packed:
    /// <c>backgrounds</c> (full-screen art fills a page on its own). Atlases are written to
    /// <c>art/atlases/&lt;pack path with dashes&gt;.spriteatlasv2</c> on every run (GUIDs kept); an atlas whose folder is gone is
    /// deleted. Settings for UI: no rotation and no tight packing (UI images draw the sprite rect, so neighbours packed
    /// into it would show), 4 px padding, no mipmaps, max 2048 per page, compressed, included in the build.
    /// </summary>
    internal static class AtlasBuilder
    {
        private const string Extension = ".spriteatlasv2";
        private const string UnpackedFolder = "backgrounds";
        private const int Padding = 4;
        private const int MaxTextureSize = 2048;

        /// <summary>Writes the atlas of every pack and removes atlases of packs that no longer exist.</summary>
        internal static void Build()
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            BuildUtility.EnsureFolder(BuildPaths.Atlases);
            var written = new HashSet<string>(StringComparer.Ordinal);
            foreach (string pack in Packs())
            {
                string path = $"{BuildPaths.Atlases}/{AtlasName(pack)}{Extension}";
                WriteAtlas(path, $"{BuildPaths.Sprites}/{pack}");
                written.Add(path);
            }

            foreach (string stale in Directory.GetFiles(BuildPaths.Atlases, "*" + Extension)
                         .Select(file => file.Replace('\\', '/'))
                         .Where(file => !written.Contains(file)))
            {
                AssetDatabase.DeleteAsset(stale);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Pack folders relative to <c>art/sprites</c>, sorted: every folder with a PNG directly inside it.</summary>
        internal static List<string> Packs()
        {
            string root = BuildPaths.Sprites + "/";
            return Directory.GetDirectories(BuildPaths.Sprites, "*", SearchOption.AllDirectories)
                .Select(folder => folder.Replace('\\', '/'))
                .Where(folder => Directory.GetFiles(folder, "*.png").Length > 0)
                .Select(folder => folder.Substring(root.Length))
                .Where(pack => pack != UnpackedFolder && !pack.StartsWith(UnpackedFolder + "/", StringComparison.Ordinal))
                .OrderBy(pack => pack, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>"screens/settings" → "screens-settings".</summary>
        internal static string AtlasName(string pack)
        {
            return pack.Replace('/', '-');
        }

        // A new atlas object every run (packables cannot be listed to diff them); saving over the file keeps its GUID.
        private static void WriteAtlas(string path, string folder)
        {
            var source = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            if (source == null)
            {
                throw new InvalidOperationException($"Sprite pack folder {folder} is not imported.");
            }

            var atlas = new SpriteAtlasAsset();
            atlas.Add(new Object[] { source });
            SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"{path} did not import as a Sprite Atlas V2.");
            }

            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
                padding = Padding,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            TextureImporterPlatformSettings platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = MaxTextureSize;
            platform.textureCompression = TextureImporterCompression.Compressed;
            platform.format = TextureImporterFormat.Automatic;
            importer.SetPlatformSettings(platform);
            importer.SaveAndReimport();
        }
    }
}
