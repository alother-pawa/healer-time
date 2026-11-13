using UnityEngine;

namespace Game.Foundation.InputPrompts
{
    /// <summary>What one glyph slot of a prompt shows, resolved by <see cref="InputPromptResolver"/>.</summary>
    public readonly struct PromptGlyph
    {
        private readonly string _text;

        /// <summary>Creates a visible glyph.</summary>
        public PromptGlyph(Sprite sprite, string text, bool showText)
        {
            Sprite = sprite;
            _text = text;
            ShowText = showText;
            IsVisible = true;
        }

        /// <summary>A slot that shows nothing (the action has no binding for the device in use).</summary>
        public static PromptGlyph Hidden => default;

        /// <summary>Icon from the glyph set; null when the slot draws its text on the view's own key frame.</summary>
        public Sprite Sprite { get; }

        /// <summary>Text of the control (glyph set text, else the binding's display string).</summary>
        public string Text => _text ?? string.Empty;

        /// <summary>True when <see cref="Text"/> is drawn over the icon (a frame glyph or no icon at all).</summary>
        public bool ShowText { get; }

        /// <summary>False when the slot is hidden.</summary>
        public bool IsVisible { get; }
    }
}
