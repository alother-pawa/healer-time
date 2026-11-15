using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Creates uGUI objects. Layout convention: for a point anchor the pivot equals the
    /// anchor (an element "anchored top-center, Pos Y −80" hangs 80 units below the top edge), unless a pivot is passed;
    /// edge-stretched rects use the pivot of that edge. All objects use the UI layer.
    /// </summary>
    internal static class UiFactory
    {
        private const float MinAutoSize = 12f;

        private const string UiLayerName = "UI";
        private const float Half = 0.5f;
        /// <summary>Canvas scaler match of every canvas: width and height weighted equally.</summary>
        internal const float MatchWidthAndHeight = 0.5f;

        /// <summary>Canvas scaler reference resolution of every canvas (landscape PC).</summary>
        internal static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        internal static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        internal static readonly Vector2 TopCenter = new Vector2(Half, 1f);
        internal static readonly Vector2 TopRight = new Vector2(1f, 1f);
        internal static readonly Vector2 MiddleLeft = new Vector2(0f, Half);
        internal static readonly Vector2 Center = new Vector2(Half, Half);
        internal static readonly Vector2 MiddleRight = new Vector2(1f, Half);
        internal static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        internal static readonly Vector2 BottomCenter = new Vector2(Half, 0f);
        internal static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        /// <summary>A new RectTransform object under <paramref name="parent"/> (scene root when null).</summary>
        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer(UiLayerName) };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Stretches on both axes with the given edge offsets.</summary>
        internal static RectTransform Stretch(this RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Point anchor with the pivot on the anchor (or <paramref name="pivot"/>), a position and a size.</summary>
        internal static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>Stretched along the top edge, hanging <paramref name="y"/> below it (negative values go down).</summary>
        internal static RectTransform TopStretch(this RectTransform rect, float y, float height, float left = 0f, float right = 0f)
        {
            return EdgeStretch(rect, 1f, y, height, left, right);
        }

        /// <summary>Stretched along the bottom edge, <paramref name="y"/> above it.</summary>
        internal static RectTransform BottomStretch(this RectTransform rect, float y, float height, float left = 0f, float right = 0f)
        {
            return EdgeStretch(rect, 0f, y, height, left, right);
        }

        /// <summary>Full height along the left edge, <paramref name="inset"/> from it.</summary>
        internal static RectTransform LeftFill(this RectTransform rect, float width, float inset = 0f)
        {
            return SideFill(rect, 0f, width, inset);
        }

        /// <summary>Full height along the right edge, <paramref name="inset"/> from it.</summary>
        internal static RectTransform RightFill(this RectTransform rect, float width, float inset = 0f)
        {
            return SideFill(rect, 1f, width, -inset);
        }

        /// <summary>Adds an untinted Image (the sprite carries its color); sprites with a 9-slice border are drawn Sliced.</summary>
        internal static Image AddImage(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Palette.White;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        /// <summary>Adds an invisible Image that only catches pointer events.</summary>
        internal static Image AddRaycastTarget(RectTransform rect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Palette.Clear;
            return image;
        }

        /// <summary>
        /// Adds a Button drawn with <paramref name="sprite"/> and a <see cref="SelectOnHover"/>, so mouse hover and the
        /// keyboard/gamepad selection are one state.
        /// </summary>
        internal static Button AddButton(RectTransform rect, Sprite sprite)
        {
            Image image = AddImage(rect, sprite);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            rect.gameObject.AddComponent<SelectOnHover>();
            return button;
        }

        /// <summary>
        /// Explicit up/down navigation through <paramref name="items"/> in order (no wrap, nothing left or right):
        /// Automatic navigation can jump into hidden objects or the screen under a popup.
        /// </summary>
        internal static void LinkVertical(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                Navigation navigation = Explicit(items[i]);
                navigation.selectOnUp = i > 0 ? items[i - 1] : null;
                navigation.selectOnDown = i < items.Length - 1 ? items[i + 1] : null;
                items[i].navigation = navigation;
            }
        }

        /// <summary>Explicit left/right navigation through <paramref name="items"/> in order (no wrap); up/down kept.</summary>
        internal static void LinkHorizontal(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                Navigation navigation = Explicit(items[i]);
                navigation.selectOnLeft = i > 0 ? items[i - 1] : null;
                navigation.selectOnRight = i < items.Length - 1 ? items[i + 1] : null;
                items[i].navigation = navigation;
            }
        }

        /// <summary>Sets the down neighbour of every item of <paramref name="items"/> to <paramref name="down"/>.</summary>
        internal static void LinkDown(Selectable down, params Selectable[] items)
        {
            foreach (Selectable item in items)
            {
                Navigation navigation = Explicit(item);
                navigation.selectOnDown = down;
                item.navigation = navigation;
            }
        }

        /// <summary>Adds a horizontal layout that sizes its children from their preferred sizes (no stretching).</summary>
        internal static HorizontalLayoutGroup AddRow(GameObject go, float spacing, TextAnchor alignment)
        {
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = alignment;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return row;
        }

        /// <summary>
        /// Adds a TextMeshPro UI text in the game's font (<see cref="GameFont"/>), with a <see cref="ScalableText"/> so it follows
        /// the text size setting. One line by default, auto-sized between half the size and the size: a large text size
        /// setting shrinks a long label to fit its box instead of spilling out or breaking the line. Texts meant to run
        /// over several lines set <see cref="Paragraph"/>.
        /// </summary>
        internal static TextMeshProUGUI AddText(RectTransform rect, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = GameFont.Asset;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(MinAutoSize, size * 0.5f);
            label.fontSizeMax = size;
            rect.gameObject.AddComponent<ScalableText>();
            return label;
        }

        /// <summary>Lets <paramref name="text"/> wrap over several lines (it still shrinks when the lines overflow its box).</summary>
        internal static TextMeshProUGUI Paragraph(this TextMeshProUGUI text)
        {
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        /// <summary>
        /// Light text with the game font's dark outline, readable on wood panels and dark art (<paramref name="muted"/>: a
        /// dimmer light for captions and hints). The weight is kept, so paragraphs stay regular.
        /// </summary>
        internal static TextMeshProUGUI OnWood(this TextMeshProUGUI text, bool muted = false)
        {
            text.color = muted ? Palette.TextLightMuted : Palette.TextLight;
            Material outline = GameFont.Outline;
            if (outline != null)
            {
                text.fontSharedMaterial = outline;
            }

            return text;
        }

        /// <summary>Bold, with the game font's dark outline (or <paramref name="material"/>): text drawn straight on art.</summary>
        internal static void Outlined(TextMeshProUGUI text, Material material = null)
        {
            Material outline = material != null ? material : GameFont.Outline;
            if (outline != null)
            {
                text.fontSharedMaterial = outline;
            }

            text.fontStyle = FontStyles.Bold;
        }

        /// <summary>A stretched, non-raycast text child (button labels, captions filling their parent).</summary>
        internal static TextMeshProUGUI Label(Transform parent, string text, float size, string name = "Label")
        {
            TextMeshProUGUI label = AddText(Rect(name, parent).Stretch(), text, size, Palette.White);
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Makes <paramref name="label"/> show the localization key <paramref name="key"/> through a
        /// <see cref="LocalizedLabel"/>; the saved text is the key itself, replaced by the localized text at runtime.
        /// </summary>
        internal static TextMeshProUGUI Localize(this TextMeshProUGUI label, string key)
        {
            label.text = key;
            Fields.Set(label.gameObject.AddComponent<LocalizedLabel>(), ("_key", key), ("_label", label));
            return label;
        }

        /// <summary>
        /// Screen Space - Overlay canvas with the shared landscape scaler (1920×1080, width and height weighted equally, so
        /// other aspect ratios scale between fitting the width and fitting the height).
        /// </summary>
        internal static Canvas AddCanvas(GameObject go, int sortOrder, bool raycaster)
        {
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthAndHeight;
            if (raycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        // Keeps the neighbours already set and switches the mode to Explicit.
        private static Navigation Explicit(Selectable item)
        {
            Navigation navigation = item.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            return navigation;
        }

        private static RectTransform EdgeStretch(RectTransform rect, float edge, float y, float height, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, edge);
            rect.anchorMax = new Vector2(1f, edge);
            rect.pivot = new Vector2(Half, edge);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.anchoredPosition = new Vector2((left - right) * Half, y);
            return rect;
        }

        private static RectTransform SideFill(RectTransform rect, float edge, float width, float x)
        {
            rect.anchorMin = new Vector2(edge, 0f);
            rect.anchorMax = new Vector2(edge, 1f);
            rect.pivot = new Vector2(edge, Half);
            rect.sizeDelta = new Vector2(width, 0f);
            rect.anchoredPosition = new Vector2(x, 0f);
            return rect;
        }
    }
}
