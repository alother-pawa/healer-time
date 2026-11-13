using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Colors the builders still set themselves: text and camera clear colors. Sprites carry their own color
    /// (Tools/ShapeGen), so images are drawn untinted (<see cref="White"/>).
    /// </summary>
    internal static class Palette
    {
        /// <summary>Camera clear color (#14121F), the same as the loading screen sprite.</summary>
        internal static readonly Color Night = BuildUtility.Hex("#14121F");
        /// <summary>Titles and body text (#E8E4FF).</summary>
        internal static readonly Color Text = BuildUtility.Hex("#E8E4FF");
        /// <summary>Captions and hints on dark panels (#B8B2D9).</summary>
        internal static readonly Color TextMuted = BuildUtility.Hex("#B8B2D9");
        /// <summary>Untinted sprites and plain text.</summary>
        internal static readonly Color White = Color.white;
        /// <summary>Invisible raycast targets.</summary>
        internal static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
    }
}
