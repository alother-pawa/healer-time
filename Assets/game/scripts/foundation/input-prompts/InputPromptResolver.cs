using Paowa.Gamepad;

namespace Game.Foundation.InputPrompts
{
    /// <summary>
    /// Turns an action into what a prompt slot shows, from <see cref="IInputGlyphs"/> (device in use, its scheme, the glyph
    /// set). An action is written <c>"Context/Action"</c>, or <c>"Context/Action/part"</c> for one part of a composite
    /// (<c>"Gameplay/Move/up"</c>). Display rule:
    /// <list type="bullet">
    /// <item>no binding for the device in use: the slot is hidden;</item>
    /// <item>no sprite: the text (glyph table label or the binding's display string) is drawn on the view's key frame;</item>
    /// <item>a sprite with a label in the glyph table (<see cref="InputGlyph.IsTableText"/>: key cap, face button,
    /// shoulder): the label is drawn over it;</item>
    /// <item>a sprite without a table label (mouse, arrow, PlayStation symbol): a complete picture, no text.</item>
    /// </list>
    /// </summary>
    public static class InputPromptResolver
    {
        private const char Separator = '/';

        /// <summary>Glyph of <paramref name="actionSpec"/> for the device in use; hidden when nothing is bound.</summary>
        public static PromptGlyph Resolve(IInputGlyphs glyphs, string actionSpec)
        {
            if (glyphs == null || !TrySplit(actionSpec, out string action, out string part))
            {
                return PromptGlyph.Hidden;
            }

            return FromGlyph(glyphs.GetActionGlyph(action, part));
        }

        /// <summary>
        /// What a slot shows for <paramref name="glyph"/> (e.g. one binding's glyph from <see cref="IInputGlyphs.GetGlyph"/>),
        /// by the display rule above; hidden when the glyph has neither sprite nor text (an unbound control).
        /// </summary>
        public static PromptGlyph FromGlyph(InputGlyph glyph)
        {
            string text = glyph.Text;
            if (!glyph.HasSprite)
            {
                return text.Length > 0 ? new PromptGlyph(null, text, true) : PromptGlyph.Hidden;
            }

            return new PromptGlyph(glyph.Sprite, text, glyph.IsTableText && text.Length > 0);
        }

        /// <summary>
        /// Splits <c>"Context/Action"</c> (no part) or <c>"Context/Action/part"</c>; false for anything else (empty, one
        /// segment, empty segments, more than three).
        /// </summary>
        public static bool TrySplit(string actionSpec, out string action, out string part)
        {
            action = null;
            part = null;
            if (string.IsNullOrEmpty(actionSpec))
            {
                return false;
            }

            string[] segments = actionSpec.Split(Separator);
            if (segments.Length < 2 || segments.Length > 3)
            {
                return false;
            }

            foreach (string segment in segments)
            {
                if (segment.Length == 0)
                {
                    return false;
                }
            }

            action = segments[0] + Separator + segments[1];
            part = segments.Length == 3 ? segments[2] : null;
            return true;
        }
    }
}
