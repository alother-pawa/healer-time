using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The base popup and its Prefab Variants. <c>Popup_Base</c> carries the shared look and motion (<c>PopupFrame</c>), a
    /// modal backdrop and a trapping <c>FocusScope</c>, so every modal popup owns keyboard/gamepad focus; each variant
    /// resizes the panel, sets the title key, adds its behaviour and content to the standard sections
    /// (<see cref="PopupParts"/>), and points the scope at its safe default control. Fixed texts are localization keys shown
    /// through <c>LocalizedLabel</c>. Standard tree (Docs/STRUCTURE.md):
    /// <code>
    /// Popup_X        root: the popup component, FocusScope, CanvasGroup, PopupFrame
    /// ├── Backdrop   modal dimmer
    /// └── Panel      moved by PopupFrame
    ///     ├── Frame          panel art
    ///     ├── Header         header art, TitleLabel
    ///     ├── Body           content area
    ///     ├── Footer         bottom strip of the body: buttons, prompts
    ///     └── Templates      inactive row and item templates
    /// </code>
    /// </summary>
    internal static class PopupPrefabBuilder
    {
        private const float TitleSize = 52f;
        /// <summary>Text size of dialog messages.</summary>
        internal const float MessageSize = 36f;
        /// <summary>Text size of button labels.</summary>
        internal const float ButtonLabelSize = 38f;
        private static readonly Vector2 DialogButtonSize = new Vector2(320f, 96f);
        private const string BodyName = "Body";
        private const string FooterName = "Footer";
        private const string TemplatesName = "Templates";
        private const string TitlePath = "Panel/Header/TitleLabel";
        private const float BodyInset = 40f;
        private const float BodyTop = 150f;
        private const float FooterHeight = 140f;

        internal static void Build()
        {
            BuildBase();
            BuildConfirm();
            SettingsPrefabBuilder.Build();
            GameplayPrefabBuilder.BuildPopups();
        }

        private static void BuildBase()
        {
            RectTransform root = UiFactory.Rect("Popup_Base", null).Stretch();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            var frame = root.gameObject.AddComponent<PopupFrame>();
            Fields.Set(root.gameObject.AddComponent<FocusScope>(), ("_trapFocus", true),
                ("_cancelMode", new EnumName(nameof(FocusCancelMode.PassThrough))));

            Image backdrop = UiFactory.AddImage(UiFactory.Rect("Backdrop", root).Stretch(), BuildUtility.Sprite("ui-common/popup-backdrop"));
            backdrop.raycastTarget = true;

            RectTransform panel = UiFactory.Rect("Panel", root).Place(UiFactory.Center, Vector2.zero, new Vector2(900f, 560f));
            UiFactory.AddImage(UiFactory.Rect("Frame", panel).Stretch(), BuildUtility.Sprite("ui-common/popup-background"));
            RectTransform header = UiFactory.Rect("Header", panel).TopStretch(-24f, 96f, 24f, 24f);
            UiFactory.AddImage(header, BuildUtility.Sprite("ui-common/popup-header")).raycastTarget = false;
            // Variants (or the popup's code) set the title's localization key.
            TextMeshProUGUI title = UiFactory.AddText(UiFactory.Rect("TitleLabel", header).Stretch(), string.Empty, TitleSize, Palette.Text);
            title.raycastTarget = false;
            Fields.Set(title.gameObject.AddComponent<LocalizedLabel>(), ("_label", title));
            UiFactory.Rect(BodyName, panel).Stretch(BodyInset, BodyInset, BodyInset, BodyTop);
            // Same bottom edge and sides as the body: what is anchored to the bottom lands where it did inside the body.
            UiFactory.Rect(FooterName, panel).BottomStretch(BodyInset, FooterHeight, BodyInset, BodyInset);
            UiFactory.Rect(TemplatesName, panel).Stretch().gameObject.SetActive(false);

            Fields.Set(frame, ("_canvasGroup", group), ("_panel", panel),
                ("_animation", BuildUtility.Load<PopupAnimationConfig>(BuildPaths.PopupAnimation)));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.PopupBase);
        }

        // Title, message, then Confirm (left) and Cancel (right); the scope starts on Cancel, the safe choice.
        private static void BuildConfirm()
        {
            PopupParts parts = Variant("Popup_Confirm", null, new Vector2(960f, 540f));
            GameObject root = parts.Root;
            Component popup = root.AddComponent(GameTypes.ConfirmDialogPopup);

            TextMeshProUGUI message = UiFactory.AddText(UiFactory.Rect("MessageLabel", parts.Body).Stretch(20f, 130f, 20f, 0f), string.Empty,
                MessageSize, Palette.Text);
            message.raycastTarget = false;
            message.Paragraph();
            var messageLabel = message.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(messageLabel, ("_label", message));

            Button confirm = DialogButton(parts.Footer, "ConfirmButton", -180f, "ui-common/button-primary", "common.ok", out LocalizedLabel confirmLabel);
            Button cancel = DialogButton(parts.Footer, "CancelButton", 180f, "ui-common/button-secondary", "common.cancel", out LocalizedLabel cancelLabel);
            UiFactory.LinkHorizontal(confirm, cancel);

            SetFirstSelected(root, cancel);
            Fields.Set(popup, ("_viewId", "popup.confirm"),
                ("_title", BuildUtility.Child(root.transform, TitlePath).GetComponent<LocalizedLabel>()),
                ("_message", messageLabel), ("_confirmLabel", confirmLabel), ("_cancelLabel", cancelLabel),
                ("_confirmButton", confirm), ("_cancelButton", cancel));
            BuildUtility.SavePrefab(root, BuildPaths.PopupConfirm);
        }

        // Instance of Popup_Base renamed, with panel size and title key overrides (none when the popup sets its title);
        // saving its root yields a Prefab Variant.
        internal static PopupParts Variant(string name, string titleKey, Vector2 panelSize)
        {
            GameObject root = BuildUtility.Instantiate(BuildPaths.PopupBase, null, name);
            var panel = (RectTransform)BuildUtility.Child(root.transform, "Panel");
            panel.sizeDelta = panelSize;
            BuildUtility.RecordOverrides(panel);
            if (!string.IsNullOrEmpty(titleKey))
            {
                Transform title = BuildUtility.Child(root.transform, TitlePath);
                var titleText = title.GetComponent<TextMeshProUGUI>();
                titleText.text = titleKey;
                var titleLabel = title.GetComponent<LocalizedLabel>();
                Fields.Set(titleLabel, ("_key", titleKey));
                BuildUtility.RecordOverrides(titleText, titleLabel);
            }

            return new PopupParts(root, panel, (RectTransform)BuildUtility.Child(panel, BodyName),
                (RectTransform)BuildUtility.Child(panel, FooterName), (RectTransform)BuildUtility.Child(panel, TemplatesName));
        }

        internal static void SetFirstSelected(GameObject root, Selectable first)
        {
            var scope = root.GetComponent<FocusScope>();
            Fields.Set(scope, ("_firstSelected", first));
            BuildUtility.RecordOverrides(scope);
        }

        // Button anchored bottom-center of the content, shifted by x, with a localized label (the popup may change its key).
        internal static Button DialogButton(RectTransform parent, string name, float x, string sprite, string key, out LocalizedLabel label)
        {
            RectTransform rect = UiFactory.Rect(name, parent).Place(UiFactory.BottomCenter, new Vector2(x, 10f), DialogButtonSize);
            Button button = UiFactory.AddButton(rect, BuildUtility.Sprite(sprite));
            TextMeshProUGUI text = UiFactory.Label(rect, string.Empty, ButtonLabelSize).Localize(key);
            label = text.GetComponent<LocalizedLabel>();
            return button;
        }
    }

    /// <summary>The standard sections of a popup variant (see <see cref="PopupPrefabBuilder"/>).</summary>
    internal readonly struct PopupParts
    {
        internal PopupParts(GameObject root, RectTransform panel, RectTransform body, RectTransform footer, RectTransform templates)
        {
            Root = root;
            Panel = panel;
            Body = body;
            Footer = footer;
            Templates = templates;
        }

        /// <summary>The variant root, saved as the prefab.</summary>
        internal GameObject Root { get; }

        /// <summary>The panel PopupFrame animates.</summary>
        internal RectTransform Panel { get; }

        /// <summary>Content area: rows, lists, messages.</summary>
        internal RectTransform Body { get; }

        /// <summary>Bottom strip of the body: dialog buttons and the prompt row.</summary>
        internal RectTransform Footer { get; }

        /// <summary>Inactive parent of row and item templates (layouts and focus ignore it).</summary>
        internal RectTransform Templates { get; }
    }
}
