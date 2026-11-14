using System;
using System.IO;

namespace Game.Editor.Builders
{
    /// <summary>
    /// <c>art/art-spec.json</c>, written by <c>Tools/ShapeGen/generate_shapes.py</c> with the placeholders: the size,
    /// 9-slice border and pivot of every image, and the frames and tags of every animation sheet. The builders import and
    /// animate the art from it, so final art only has to keep the names and sizes listed in <c>Docs/ART_SPEC.md</c>.
    /// Field names match the JSON.
    /// </summary>
    [Serializable]
    internal sealed class ArtSpec
    {
        public int pixelsPerUnit = 128;
        public SpriteSpec[] sprites = Array.Empty<SpriteSpec>();
        public SheetSpec[] sheets = Array.Empty<SheetSpec>();

        /// <summary>Reads the spec; throws when the file is missing (run the generator).</summary>
        internal static ArtSpec Load()
        {
            if (!File.Exists(BuildPaths.ArtSpec))
            {
                throw new InvalidOperationException($"{BuildPaths.ArtSpec} is missing. Run python Tools/ShapeGen/generate_shapes.py.");
            }

            return UnityEngine.JsonUtility.FromJson<ArtSpec>(File.ReadAllText(BuildPaths.ArtSpec));
        }
    }

    /// <summary>One image: path under art/sprites without extension, size in pixels, border (left, bottom, right, top), pivot.</summary>
    [Serializable]
    internal sealed class SpriteSpec
    {
        public string path;
        public int width;
        public int height;
        public int[] border = new int[4];
        public float[] pivot = { 0.5f, 0.5f };
    }

    /// <summary>An animation sheet: one row per tag (first tag on top), frames left to right, every cell one frame.</summary>
    [Serializable]
    internal sealed class SheetSpec
    {
        public string path;
        public int frameWidth;
        public int frameHeight;
        public float[] pivot = { 0.5f, 0f };
        public TagSpec[] tags = Array.Empty<TagSpec>();

        /// <summary>Texture pixels per world unit of this sheet; 0 uses the spec's. Pixel art drawn small uses fewer.</summary>
        public float pixelsPerUnit;

        /// <summary>Point (nearest) filtering, for pixel art shown larger than its texture.</summary>
        public bool point;

        /// <summary>Widest row in frames.</summary>
        internal int Columns
        {
            get
            {
                int columns = 0;
                foreach (TagSpec tag in tags)
                {
                    columns = Math.Max(columns, tag.frames);
                }

                return columns;
            }
        }

        /// <summary>The sheet's file name, used to name its sprites, clips and controller.</summary>
        internal string Name => path.Substring(path.LastIndexOf('/') + 1);
    }

    /// <summary>One animation: its row's frame count, the time of each frame and whether it loops.</summary>
    [Serializable]
    internal sealed class TagSpec
    {
        public string name;
        public int frames;
        public int frameMs;
        public bool loop = true;
    }
}
