using Game.Foundation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The credits popup (a variant of <c>Popup_Base</c>): a clipped viewport with the list (top-anchored, sized by a
    /// vertical layout), inactive section and entry templates outside the list (the popup builds rows from the credits
    /// data at runtime), Close, and prompts (scroll keys on keyboard, scroll stick on a gamepad, Back).
    /// </summary>
    internal static class CreditsPrefabBuilder
    {
        private const float SectionSize = 44f;
        private const float NameSize = 36f;
        private const float RoleSize = 28f;
        private const float NoteSize = 22f;
        private const float PromptRowHeight = 56f;
        private static readonly Vector2 PanelSize = new Vector2(1300f, 940f);
        private static readonly Color Accent = BuildUtility.Hex("#FFD166");

        internal static void BuildPopup()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_Credits", "credits.title", PanelSize);
            GameObject root = parts.Root;
            RectTransform content = parts.Body;
            Component popup = root.AddComponent(GameTypes.CreditsPopup);

            RectTransform viewport = UiFactory.Rect("Viewport", content).Stretch(0f, 170f, 0f, 0f);
            UiFactory.AddImage(viewport, BuildUtility.Sprite("screens/credits/credits-viewport")).raycastTarget = false;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform list = UiFactory.Rect("List", viewport).TopStretch(0f, 0f, 40f, 40f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 40, 60);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Templates: inactive, outside the list so its layout ignores them.
            RectTransform templates = parts.Templates;
            TextMeshProUGUI section = UiFactory.AddText(UiFactory.Rect("SectionTemplate", templates), string.Empty, SectionSize, Accent);
            section.raycastTarget = false;
            var sectionLabel = section.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(sectionLabel, ("_label", section));

            RectTransform entry = UiFactory.Rect("EntryTemplate", templates);
            var entryLayout = entry.gameObject.AddComponent<VerticalLayoutGroup>();
            entryLayout.spacing = 2f;
            entryLayout.childControlWidth = true;
            entryLayout.childControlHeight = true;
            entryLayout.childForceExpandWidth = true;
            entryLayout.childForceExpandHeight = false;
            TextMeshProUGUI entryName = UiFactory.AddText(UiFactory.Rect("Name", entry), string.Empty, NameSize, Palette.Text);
            entryName.raycastTarget = false;
            TextMeshProUGUI role = UiFactory.AddText(UiFactory.Rect("Role", entry), string.Empty, RoleSize, Palette.TextMuted);
            role.raycastTarget = false;
            var roleLabel = role.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(roleLabel, ("_label", role));
            TextMeshProUGUI note = UiFactory.AddText(UiFactory.Rect("Note", entry), string.Empty, NoteSize, Palette.TextMuted);
            note.raycastTarget = false;
            Component entryView = entry.gameObject.AddComponent(GameTypes.CreditsEntryView);
            Fields.Set(entryView, ("_nameText", entryName), ("_role", roleLabel), ("_noteText", note));

            Button close = PopupPrefabBuilder.DialogButton(parts.Footer, "CloseButton", 0f, "ui-common/button-secondary", "common.close", out _);
            ((RectTransform)close.transform).anchoredPosition = new Vector2(0f, PromptRowHeight + 10f);

            RectTransform promptRow = PrefabBuilder.PromptRow(parts.Footer, "Prompts", 0f, PromptRowHeight);
            GameObject keys = PrefabBuilder.AddPrompt(promptRow, "ScrollKeysPrompt", new[] { "UI/Navigate/up", "UI/Navigate/down" }, "prompt.scroll");
            GameObject stick = PrefabBuilder.AddPrompt(promptRow, "ScrollStickPrompt", new[] { "UI/Navigate" }, "prompt.scroll");
            PrefabBuilder.AddPrompt(promptRow, PrefabBuilder.Prompts.Back, new[] { "UI/Cancel" }, "prompt.back");

            PopupPrefabBuilder.SetFirstSelected(root, close);
            Fields.Set(popup, ("_viewId", "popup.credits"), ("_data", BuildUtility.Load<ScriptableObject>(BuildPaths.CreditsData)),
                ("_viewport", viewport), ("_list", list), ("_sectionTemplate", sectionLabel), ("_entryTemplate", entryView),
                ("_closeButton", close), ("_keysPrompt", keys), ("_stickPrompt", stick), ("_scrollAction", "UI/Navigate"),
                ("_wheelAction", "UI/ScrollWheel"));
            BuildUtility.SavePrefab(root, BuildPaths.PopupCredits);
        }
    }
}
