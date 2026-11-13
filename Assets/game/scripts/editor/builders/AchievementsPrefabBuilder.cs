using Game.Foundation.Localization;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The achievements popup (a variant of <c>Popup_Base</c>): the unlocked count, a scroll list kept in view of the
    /// selection (rows are built at runtime from the achievement catalog), an inactive row template outside the list, an
    /// empty notice, Close and the Back prompt.
    /// </summary>
    internal static class AchievementsPrefabBuilder
    {
        private const float ProgressSize = 32f;
        private const float NameSize = 34f;
        private const float DescriptionSize = 26f;
        private const float StatusSize = 26f;
        private const float RowHeight = 132f;
        private const float IconSize = 96f;
        private const float LockSize = 40f;
        private const float StatusWidth = 220f;
        private const float RowPadding = 18f;
        private const float PromptRowHeight = 56f;
        private const float ScrollSensitivity = 40f;
        private static readonly Vector2 PanelSize = new Vector2(1300f, 940f);

        internal static void BuildPopup()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_Achievements", "achievements.title", PanelSize);
            GameObject root = parts.Root;
            RectTransform content = parts.Body;
            Component popup = root.AddComponent(GameTypes.AchievementsPopup);

            TextMeshProUGUI progressText = UiFactory.AddText(UiFactory.Rect("Progress", content).TopStretch(0f, 50f), string.Empty,
                ProgressSize, Palette.TextMuted);
            progressText.raycastTarget = false;
            var progress = progressText.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(progress, ("_label", progressText));

            RectTransform scroll = UiFactory.Rect("Scroll", content).Stretch(0f, 180f, 0f, 70f);
            RectTransform viewport = UiFactory.Rect("Viewport", scroll).Stretch();
            // Catches the mouse wheel over gaps between rows.
            UiFactory.AddRaycastTarget(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform list = UiFactory.Rect("List", viewport).TopStretch(0f, 0f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = list;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = ScrollSensitivity;
            scroll.gameObject.AddComponent<ScrollToSelected>();

            TextMeshProUGUI empty = UiFactory.AddText(UiFactory.Rect("Empty", scroll).Stretch(), string.Empty, NameSize, Palette.TextMuted)
                .Localize("achievements.empty");
            empty.raycastTarget = false;

            // Template: inactive, outside the list so its layout ignores it.
            RectTransform templates = parts.Templates;
            Component row = BuildRow(templates);

            Button close = PopupPrefabBuilder.DialogButton(parts.Footer, "CloseButton", 0f, "ui-common/button-secondary", "common.close", out _);
            ((RectTransform)close.transform).anchoredPosition = new Vector2(0f, PromptRowHeight + 10f);

            RectTransform promptRow = PrefabBuilder.PromptRow(parts.Footer, "Prompts", 0f, PromptRowHeight);
            PrefabBuilder.AddPrompt(promptRow, PrefabBuilder.Prompts.Back, new[] { "UI/Cancel" }, "prompt.back");

            PopupPrefabBuilder.SetFirstSelected(root, close);
            Fields.Set(popup, ("_viewId", "popup.achievements"), ("_scroll", scrollRect), ("_list", list), ("_rowTemplate", row),
                ("_progress", progress), ("_emptyLabel", empty.gameObject), ("_closeButton", close));
            BuildUtility.SavePrefab(root, BuildPaths.PopupAchievements);
        }

        // A button (the navigation target, no click action) with the icon, lock, texts and status inside a dimmable group.
        private static Component BuildRow(RectTransform parent)
        {
            RectTransform rect = UiFactory.Rect("RowTemplate", parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            Button button = UiFactory.AddButton(rect, BuildUtility.Sprite("ui-common/row-background"));

            RectTransform body = UiFactory.Rect("Content", rect).Stretch();
            var group = body.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Image icon = UiFactory.AddImage(UiFactory.Rect("Icon", body)
                .Place(UiFactory.MiddleLeft, new Vector2(RowPadding, 0f), new Vector2(IconSize, IconSize), UiFactory.MiddleLeft), null);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            float textLeft = RowPadding * 2f + IconSize;
            TextMeshProUGUI nameText = UiFactory.AddText(UiFactory.Rect("Name", body).Stretch(textLeft, RowHeight / 2f, StatusWidth, RowPadding),
                string.Empty, NameSize, Palette.Text, TextAlignmentOptions.BottomLeft);
            nameText.raycastTarget = false;
            var nameLabel = nameText.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(nameLabel, ("_label", nameText));
            TextMeshProUGUI descriptionText = UiFactory.AddText(
                UiFactory.Rect("Description", body).Stretch(textLeft, RowPadding, StatusWidth, RowHeight / 2f + 4f),
                string.Empty, DescriptionSize, Palette.TextMuted, TextAlignmentOptions.TopLeft);
            descriptionText.raycastTarget = false;
            var descriptionLabel = descriptionText.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(descriptionLabel, ("_label", descriptionText));

            RectTransform status = UiFactory.Rect("Status", body).RightFill(StatusWidth, RowPadding);
            TextMeshProUGUI statusText = UiFactory.AddText(UiFactory.Rect("Text", status).Stretch(0f, 0f, LockSize + 8f, 0f),
                string.Empty, StatusSize, Palette.TextMuted, TextAlignmentOptions.MidlineRight);
            statusText.raycastTarget = false;
            var statusLabel = statusText.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(statusLabel, ("_label", statusText));
            Image lockIcon = UiFactory.AddImage(UiFactory.Rect("Lock", status)
                .Place(UiFactory.MiddleRight, Vector2.zero, new Vector2(LockSize, LockSize), UiFactory.MiddleRight),
                BuildUtility.Sprite("screens/achievements/lock-icon"));
            lockIcon.raycastTarget = false;

            Component view = rect.gameObject.AddComponent(GameTypes.AchievementRowView);
            Fields.Set(view, ("_selectable", button), ("_content", group), ("_icon", icon), ("_lockIcon", lockIcon.gameObject),
                ("_name", nameLabel), ("_description", descriptionLabel), ("_status", statusLabel));
            return view;
        }
    }
}
