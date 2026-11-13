using Game.Foundation.Localization;
using Game.Foundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The save slot panel: the card prefab (<c>prefabs/save-slots/SaveSlotCard</c>: title, 16:9 thumbnail with placeholder and
    /// state icon, details or status line, a vertical column of Continue / New game / Delete) and the panel popup (a
    /// variant of <c>Popup_Base</c>: a row the cards are created in at runtime, one per slot id of the save data, a Close
    /// button and a prompt row with Confirm, Delete and Back). Sizes fit three cards and the 1920×1080 reference at the
    /// largest UI scale.
    /// </summary>
    internal static class SaveSlotsPrefabBuilder
    {
        private static readonly Vector2 PanelSize = new Vector2(1500f, 880f);
        private static readonly Vector2 CardSize = new Vector2(440f, 550f);
        private static readonly Vector2 ThumbnailSize = new Vector2(320f, 180f);
        private static readonly Vector2 StateIconSize = new Vector2(96f, 96f);
        private static readonly Vector2 CloseButtonSize = new Vector2(320f, 64f);

        private const float CardInset = 20f;
        private const float TitleSize = 34f;
        private const float LocationSize = 30f;
        private const float DetailSize = 26f;
        private const float ActionLabelSize = 30f;
        private const float ActionHeight = 52f;
        private const float ActionSpacing = 8f;
        private const int ActionCount = 3;
        private const float CardSpacing = 30f;
        private const float PromptRowHeight = 48f;
        private const float CloseY = 56f;
        private const float CardsBottom = 136f;

        private static readonly (string name, string[] actions, string labelKey)[] Prompts =
        {
            (PrefabBuilder.Prompts.Confirm, new[] { "UI/Submit" }, "prompt.confirm"),
            ("DeletePrompt", new[] { "Menu/Delete" }, "prompt.delete"),
            (PrefabBuilder.Prompts.Back, new[] { "UI/Cancel" }, "prompt.back"),
        };

        internal static void Build()
        {
            BuildCard();
            BuildPanel();
        }

        // Title, thumbnail, details/status, then the action column at the bottom (at most three actions show at once).
        private static void BuildCard()
        {
            RectTransform root = UiFactory.Rect("SaveSlotCard", null).Place(UiFactory.Center, Vector2.zero, CardSize);
            UiFactory.AddImage(root, BuildUtility.Sprite("screens/save-slots/slot-card-frame")).raycastTarget = false;
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = CardSize.x;
            layout.preferredHeight = CardSize.y;
            Component card = root.gameObject.AddComponent(GameTypes.SaveSlotCardView);

            LocalizedLabel title = Text(UiFactory.Rect("Title", root).TopStretch(-12f, 44f, CardInset, CardInset), TitleSize, Palette.Text,
                TextAlignmentOptions.Center);

            RectTransform thumbnailArea = UiFactory.Rect("Thumbnail", root).Place(UiFactory.TopCenter, new Vector2(0f, -62f), ThumbnailSize);
            Image placeholder = UiFactory.AddImage(UiFactory.Rect("Placeholder", thumbnailArea).Stretch(),
                BuildUtility.Sprite("screens/save-slots/thumbnail-placeholder"));
            placeholder.raycastTarget = false;
            RectTransform pictureRect = UiFactory.Rect("Picture", thumbnailArea).Stretch();
            var picture = pictureRect.gameObject.AddComponent<RawImage>();
            picture.raycastTarget = false;
            pictureRect.gameObject.SetActive(false);
            Image stateIcon = UiFactory.AddImage(UiFactory.Rect("StateIcon", thumbnailArea).Place(UiFactory.Center, Vector2.zero, StateIconSize),
                BuildUtility.Sprite("screens/save-slots/slot-empty-icon"));
            stateIcon.raycastTarget = false;
            stateIcon.preserveAspect = true;

            RectTransform details = UiFactory.Rect("Details", root).TopStretch(-250f, 100f, CardInset, CardInset);
            LocalizedLabel location = Text(UiFactory.Rect("Location", details).TopStretch(0f, 36f), LocationSize, Palette.Text,
                TextAlignmentOptions.MidlineLeft);
            LocalizedLabel playtime = Text(UiFactory.Rect("Playtime", details).TopStretch(-38f, 30f), DetailSize, Palette.TextMuted,
                TextAlignmentOptions.MidlineLeft);
            LocalizedLabel savedAt = Text(UiFactory.Rect("SavedAt", details).TopStretch(-70f, 30f), DetailSize, Palette.TextMuted,
                TextAlignmentOptions.MidlineLeft);
            LocalizedLabel status = Text(UiFactory.Rect("Status", root).TopStretch(-250f, 100f, CardInset, CardInset), DetailSize,
                Palette.TextMuted, TextAlignmentOptions.Center);

            RectTransform actions = UiFactory.Rect("Actions", root)
                .BottomStretch(12f, ActionCount * ActionHeight + (ActionCount - 1) * ActionSpacing, CardInset + 4f, CardInset + 4f);
            var column = actions.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = ActionSpacing;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            // Order of the column: the panel walks the visible buttons top to bottom.
            Button continueButton = Action(actions, "ContinueButton", "ui-common/button-primary", "save-slots.action.continue");
            Button newGameButton = Action(actions, "NewGameButton", "ui-common/button-secondary", "save-slots.action.new-game");
            Button deleteButton = Action(actions, "DeleteButton", "ui-common/button-secondary", "save-slots.action.delete");

            Fields.Set(card, ("_title", title), ("_titleKey", "save-slots.card.title"), ("_thumbnail", picture),
                ("_thumbnailPlaceholder", placeholder), ("_stateIcon", stateIcon),
                ("_emptyIcon", BuildUtility.Sprite("screens/save-slots/slot-empty-icon")),
                ("_problemIcon", BuildUtility.Sprite("screens/save-slots/slot-corrupted-icon")), ("_details", details.gameObject),
                ("_location", location), ("_playtime", playtime), ("_playtimeKey", "save-slots.card.playtime"), ("_savedAt", savedAt),
                ("_savedAtKey", "save-slots.card.saved-at"), ("_status", status), ("_emptyKey", "save-slots.card.empty"),
                ("_unlabeledKey", "save-slots.card.unlabeled"), ("_damagedKey", "save-slots.card.damaged"),
                ("_newerVersionKey", "save-slots.card.newer"), ("_unreadableKey", "save-slots.card.unreadable"),
                ("_migrationFailedKey", "save-slots.card.migration-failed"), ("_continueButton", continueButton),
                ("_newGameButton", newGameButton), ("_deleteButton", deleteButton));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.SaveSlotCard);
        }

        // Cards row on top (filled at runtime), Close below it, prompts at the bottom; the panel picks the first selection.
        private static void BuildPanel()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_SaveSlots", "save-slots.title", PanelSize);
            GameObject root = parts.Root;
            RectTransform content = parts.Body;
            Component panel = root.AddComponent(GameTypes.SaveSlotPanel);
            root.AddComponent<InputContextRequest>();

            RectTransform cards = UiFactory.Rect("Cards", content).Stretch(0f, CardsBottom, 0f, 0f);
            UiFactory.AddRow(cards.gameObject, CardSpacing, TextAnchor.UpperCenter);

            RectTransform closeRect = UiFactory.Rect("CloseButton", parts.Footer).Place(UiFactory.BottomCenter, new Vector2(0f, CloseY), CloseButtonSize);
            Button close = UiFactory.AddButton(closeRect, BuildUtility.Sprite("ui-common/button-secondary"));
            UiFactory.Label(closeRect, string.Empty, PopupPrefabBuilder.ButtonLabelSize).Localize("common.close");

            RectTransform promptRow = PrefabBuilder.PromptRow(parts.Footer, "Prompts", 0f, PromptRowHeight);
            GameObject deletePrompt = null;
            foreach ((string name, string[] actions, string labelKey) in Prompts)
            {
                GameObject prompt = PrefabBuilder.AddPrompt(promptRow, name, actions, labelKey);
                if (name == "DeletePrompt")
                {
                    deletePrompt = prompt;
                }
            }

            PopupPrefabBuilder.SetFirstSelected(root, close);
            Fields.Set(panel, ("_viewId", "popup.save-slots"),
                ("_cardPrefab", BuildUtility.PrefabComponent(BuildPaths.SaveSlotCard, GameTypes.SaveSlotCardView)),
                ("_cardRow", cards), ("_closeButton", close), ("_deletePrompt", deletePrompt), ("_cancelKey", "common.cancel"),
                ("_overwriteTitleKey", "save-slots.overwrite.title"), ("_overwriteMessageKey", "save-slots.overwrite.message"),
                ("_overwriteConfirmKey", "save-slots.overwrite.confirm"), ("_deleteTitleKey", "save-slots.delete.title"),
                ("_deleteMessageKey", "save-slots.delete.message"), ("_deleteConfirmKey", "save-slots.action.delete"),
                ("_deleteNewerMessageKey", "save-slots.delete.newer-message"), ("_errorTitleKey", "save-slots.error.title"),
                ("_errorConfirmKey", "common.ok"), ("_unreadableMessageKey", "save-slots.error.unreadable"),
                ("_migrationFailedMessageKey", "save-slots.error.migration-failed"), ("_newerVersionMessageKey", "save-slots.error.newer"),
                ("_createFailedMessageKey", "save-slots.error.create"), ("_deleteFailedMessageKey", "save-slots.error.delete"));
            BuildUtility.SavePrefab(root, BuildPaths.PopupSaveSlots);
        }

        // A text whose key the card writes at runtime.
        private static LocalizedLabel Text(RectTransform rect, float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = UiFactory.AddText(rect, string.Empty, size, color, alignment);
            text.raycastTarget = false;
            var label = text.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(label, ("_label", text));
            return label;
        }

        private static Button Action(RectTransform column, string name, string sprite, string key)
        {
            RectTransform rect = UiFactory.Rect(name, column);
            Button button = UiFactory.AddButton(rect, BuildUtility.Sprite(sprite));
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = ActionHeight;
            UiFactory.Label(rect, string.Empty, ActionLabelSize).Localize(key);
            return button;
        }
    }
}
