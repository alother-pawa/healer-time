using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Foundation.InputPrompts
{
    /// <summary>
    /// One glyph slot of a prompt: an icon with an optional text over it. A sprite with a 9-slice border is drawn Sliced and
    /// grows with its text (a "Space" key cap is wider than a "Q" one); any other sprite keeps its aspect at the slot height. The slot deactivates itself when
    /// hidden, so a layout group closes the gap.
    /// REFERENCES: <c>_icon</c> draws the glyph sprite (or <c>_keyFrame</c> for a text-only glyph); <c>_text</c> is the
    /// label over it; <c>_layout</c> receives the slot's preferred size.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputGlyphView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private LayoutElement _layout;
        [Tooltip("Frame drawn behind the text when the glyph set has no sprite for the control (any unlisted key).")]
        [SerializeField] private Sprite _keyFrame;
        [Min(1f)] [SerializeField] private float _height = 48f;
        [Tooltip("Space between the text and the sides of a frame glyph, in canvas units.")]
        [Min(0f)] [SerializeField] private float _textPadding = 14f;

        /// <summary>Shows <paramref name="glyph"/>, or hides the slot.</summary>
        public void Show(PromptGlyph glyph)
        {
            if (!glyph.IsVisible)
            {
                gameObject.SetActive(false);
                return;
            }

            Sprite sprite = glyph.Sprite != null ? glyph.Sprite : _keyFrame;
            bool sliced = sprite != null && sprite.border != Vector4.zero;
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
            _icon.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            _icon.preserveAspect = !sliced;

            _text.gameObject.SetActive(glyph.ShowText);
            _text.text = glyph.ShowText ? glyph.Text : string.Empty;

            float width = _height;
            if (glyph.ShowText && sliced)
            {
                width = Mathf.Max(_height, _text.GetPreferredValues(glyph.Text).x + _textPadding * 2f);
            }
            else if (!sliced && sprite != null && sprite.rect.height > 0f)
            {
                width = _height * sprite.rect.width / sprite.rect.height;
            }

            _layout.preferredWidth = width;
            _layout.preferredHeight = _height;
            gameObject.SetActive(true);
        }
    }
}
