using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The standard tree of a screen canvas (Docs/STRUCTURE.md), made by <see cref="Create"/>:
    /// <code>
    /// XCanvas          Canvas, CanvasScaler (+ GraphicRaycaster)
    /// ├── Background   full-bleed art (Image)
    /// ├── Screen       the view component and its FocusScope
    /// │   ├── Header      title, top bar
    /// │   ├── Body        main content
    /// │   ├── Footer      buttons, version, prompt bar
    /// │   └── Templates   inactive templates cloned at runtime
    /// └── Overlay      drawn above everything: the selection highlight, effects
    /// </code>
    /// Every section stretches over the whole canvas, so a position given relative to the screen is the same in any section.
    /// </summary>
    internal readonly struct ScreenParts
    {
        private ScreenParts(RectTransform canvas, RectTransform screen, RectTransform header, RectTransform body,
            RectTransform footer, RectTransform templates, RectTransform overlay)
        {
            Canvas = canvas;
            Screen = screen;
            Header = header;
            Body = body;
            Footer = footer;
            Templates = templates;
            Overlay = overlay;
        }

        /// <summary>Canvas root, saved as the prefab.</summary>
        internal RectTransform Canvas { get; }

        /// <summary>Holds the view component and the FocusScope.</summary>
        internal RectTransform Screen { get; }

        internal RectTransform Header { get; }

        internal RectTransform Body { get; }

        internal RectTransform Footer { get; }

        /// <summary>Inactive: templates under it never draw, lay out or take focus.</summary>
        internal RectTransform Templates { get; }

        internal RectTransform Overlay { get; }

        /// <summary>
        /// A canvas named <paramref name="name"/> with the standard sections; <paramref name="background"/> is the sprite path
        /// of the full-bleed art (none when null).
        /// </summary>
        internal static ScreenParts Create(string name, int sortOrder, bool raycaster, string background)
        {
            RectTransform canvas = UiFactory.Rect(name, null);
            UiFactory.AddCanvas(canvas.gameObject, sortOrder, raycaster);
            RectTransform backgroundRect = UiFactory.Rect("Background", canvas).Stretch();
            if (background != null)
            {
                UiFactory.AddImage(backgroundRect, BuildUtility.Sprite(background)).raycastTarget = false;
            }

            RectTransform screen = UiFactory.Rect("Screen", canvas).Stretch();
            RectTransform header = UiFactory.Rect("Header", screen).Stretch();
            RectTransform body = UiFactory.Rect("Body", screen).Stretch();
            RectTransform footer = UiFactory.Rect("Footer", screen).Stretch();
            RectTransform templates = UiFactory.Rect("Templates", screen).Stretch();
            templates.gameObject.SetActive(false);
            RectTransform overlay = UiFactory.Rect("Overlay", canvas).Stretch();
            return new ScreenParts(canvas, screen, header, body, footer, templates, overlay);
        }
    }
}
