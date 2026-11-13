using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Colors the builders still set themselves: text and camera clear colors. Sprites carry their own color
    /// (Tools/ShapeGen), so images are drawn untinted (<see cref="White"/>).
    /// </summary>
    internal static class Palette
    {
        /// <summary>Camera clear color of menu scenes (#2B2236).</summary>
        internal static readonly Color Night = BuildUtility.Hex("#2B2236");
        /// <summary>Camera clear color of the battlefield, the sky behind its art (#9AD3E6).</summary>
        internal static readonly Color Sky = BuildUtility.Hex("#9AD3E6");
        /// <summary>Titles and body text on the cream panels of the UI pack (#4A2F27).</summary>
        internal static readonly Color Text = BuildUtility.Hex("#4A2F27");
        /// <summary>Captions and hints on cream panels (#8A6A58).</summary>
        internal static readonly Color TextMuted = BuildUtility.Hex("#8A6A58");
        /// <summary>Text drawn on wood and dark art (#FFF4D6).</summary>
        internal static readonly Color TextLight = BuildUtility.Hex("#FFF4D6");
        /// <summary>Untinted sprites and plain text.</summary>
        internal static readonly Color White = Color.white;
        /// <summary>Invisible raycast targets.</summary>
        internal static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
    }
}
