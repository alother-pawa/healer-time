using System.Collections.Generic;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.Desktop;
using Paowa.Settings;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The settings screen: one row prefab per setting kind (<c>prefabs/settings/SettingRow_*</c>), the settings popup (a
    /// variant of <c>Popup_Base</c>: tab row, one page per tab of the catalog assigned in <c>DesktopSettings</c>, a description
    /// panel, footer buttons and prompts) and the "Keep these display settings?" popup. Pages hold no rows: the popup builds
    /// them at runtime from the settings model, so a setting added to the catalog needs no rebuild; a new tab does (its page
    /// and tab button are made here). A page whose tab has key binding rows gets a scheme bar (one button per control scheme).
    /// </summary>
    internal static class SettingsPrefabBuilder
    {
        // Fits the 1920×1080 reference at the largest UI scale (1.2, settings catalog).
        private static readonly Vector2 PanelSize = new Vector2(1580f, 880f);
        private static readonly Vector2 DialogSize = new Vector2(960f, 540f);
        private static readonly Vector2 RowSize = new Vector2(1100f, RowHeight);
        private static readonly Vector2 ArrowSize = new Vector2(56f, 56f);
        private static readonly Vector2 StepperValueSize = new Vector2(300f, 60f);
        private static readonly Vector2 SlotSize = new Vector2(280f, 56f);
        private static readonly Vector2 SliderSize = new Vector2(420f, 40f);
        private static readonly Vector2 FooterButtonSize = new Vector2(280f, 72f);
        private static readonly Vector2 SchemeButtonSize = new Vector2(300f, 56f);

        private const float RowHeight = 72f;
        private const float RowSpacing = 8f;
        private const float RowInset = 24f;
        private const float CaptionWidth = 420f;
        private const float CaptionSize = 32f;
        private const float ValueSize = 32f;
        private const float SlotTextSize = 22f;
        private const float SlotTextMinSize = 14f;
        private const float GlyphGap = 16f;
        private const float TabHeight = 72f;
        private const float TabLabelSize = 30f;
        private const float MaxTabWidth = 300f;
        private const float TabGap = 20f;
        private const float DescriptionWidth = 420f;
        private const float BodyGap = 24f;
        private const float DescriptionTitleSize = 34f;
        private const float DescriptionBodySize = 28f;
        private const float FooterButtonSpacing = 300f;
        private const float PromptRowHeight = 48f;
        private const float FooterGap = 8f;
        private const float FooterHeight = PromptRowHeight + FooterGap + 72f;
        private const float SchemeBarHeight = 72f;
        private const float ScrollSensitivity = 40f;

        private static readonly (string name, string[] actions, string labelKey)[] FooterPrompts =
        {
            (PrefabBuilder.Prompts.Confirm, new[] { "UI/Submit" }, "prompt.confirm"),
            (PrefabBuilder.Prompts.Back, new[] { "UI/Cancel" }, "prompt.back"),
            (PrefabBuilder.Prompts.SwitchTab, new[] { "Menu/TabPrevious", "Menu/TabNext" }, "prompt.switch-tab"),
            ("ApplyPrompt", new[] { "Menu/Apply" }, "prompt.apply"),
            ("RevertPrompt", new[] { "Menu/Revert" }, "prompt.revert"),
            ("DefaultsPrompt", new[] { "Menu/Defaults" }, "prompt.defaults"),
        };

        internal static void Build()
        {
            BuildToggleRow();
            BuildSliderRow();
            BuildChoiceRow();
            BuildKeyBindingRow();
            BuildButtonRow();
            BuildSettingsPopup();
            BuildDisplayConfirm();
        }

        // Root button drawn as the row ground; the value key on the right.
        private static void BuildToggleRow()
        {
            RectTransform root = RowRoot("SettingRow_Toggle", out LocalizedLabel caption);
            Button button = UiFactory.AddButton(root, BuildUtility.Sprite("ui-common/row-background"));
            TextMeshProUGUI value = UiFactory.AddText(UiFactory.Rect("Value", root).RightFill(320f, RowInset), string.Empty, ValueSize,
                Palette.Text, TextAlignmentOptions.MidlineRight).Localize("settings.value.on");
            value.raycastTarget = false;
            Component row = root.gameObject.AddComponent(GameTypes.ToggleSettingRow);
            Fields.Set(row, ("_caption", caption), ("_button", button), ("_value", value.GetComponent<LocalizedLabel>()),
                ("_onKey", "settings.value.on"), ("_offKey", "settings.value.off"));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.SettingRowToggle);
        }

        // Track, fill and handle like uGUI's default slider; the formatted value at the right edge.
        private static void BuildSliderRow()
        {
            RectTransform root = RowRoot("SettingRow_Slider", out LocalizedLabel caption);
            UiFactory.AddImage(root, BuildUtility.Sprite("ui-common/row-background"));

            RectTransform sliderRect = UiFactory.Rect("Slider", root).Place(UiFactory.MiddleRight, new Vector2(-150f, 0f), SliderSize);
            UiFactory.AddImage(UiFactory.Rect("Track", sliderRect).Stretch(0f, 12f, 0f, 12f), BuildUtility.Sprite("screens/settings/slider-track"));
            RectTransform fillArea = UiFactory.Rect("FillArea", sliderRect).Stretch(4f, 12f, 16f, 12f);
            RectTransform fill = UiFactory.Rect("Fill", fillArea).Stretch();
            fill.sizeDelta = new Vector2(12f, 0f);
            UiFactory.AddImage(fill, BuildUtility.Sprite("screens/settings/slider-fill")).raycastTarget = false;
            RectTransform handleArea = UiFactory.Rect("HandleArea", sliderRect).Stretch(20f, 0f, 20f, 0f);
            RectTransform handle = UiFactory.Rect("Handle", handleArea).Stretch();
            handle.sizeDelta = new Vector2(SliderSize.y, 0f);
            Image handleImage = UiFactory.AddImage(handle, BuildUtility.Sprite("screens/settings/slider-handle"));
            handleImage.preserveAspect = true;

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            slider.navigation = navigation;
            sliderRect.gameObject.AddComponent<SelectOnHover>();

            TextMeshProUGUI value = UiFactory.AddText(UiFactory.Rect("Value", root).RightFill(110f, RowInset), string.Empty, ValueSize,
                Palette.Text, TextAlignmentOptions.MidlineRight);
            value.raycastTarget = false;
            Component row = root.gameObject.AddComponent(GameTypes.SliderSettingRow);
            Fields.Set(row, ("_caption", caption), ("_slider", slider), ("_value", value));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.SettingRowSlider);
        }

        // Root button (keeps the selection); arrows for the mouse, not navigable, without hover selection.
        private static void BuildChoiceRow()
        {
            RectTransform root = RowRoot("SettingRow_Choice", out LocalizedLabel caption);
            Button button = UiFactory.AddButton(root, BuildUtility.Sprite("ui-common/row-background"));
            float nextX = -RowInset;
            float valueX = nextX - ArrowSize.x - 8f;
            float previousX = valueX - StepperValueSize.x - 8f;
            Button previous = Arrow(root, "Previous", previousX, "screens/settings/stepper-left");
            TextMeshProUGUI value = UiFactory.AddText(
                UiFactory.Rect("Value", root).Place(UiFactory.MiddleRight, new Vector2(valueX, 0f), StepperValueSize), string.Empty,
                ValueSize, Palette.Text);
            value.raycastTarget = false;
            Button next = Arrow(root, "Next", nextX, "screens/settings/stepper-right");
            Component row = root.gameObject.AddComponent(GameTypes.ChoiceSettingRow);
            Fields.Set(row, ("_caption", caption), ("_button", button), ("_previous", previous), ("_next", next), ("_value", value),
                ("_keepKey", "settings.value.keep"));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.SettingRowChoice);
        }

        // Two slot buttons (primary, secondary) at the right; each holds a centred glyph and a state text.
        private static void BuildKeyBindingRow()
        {
            RectTransform root = RowRoot("SettingRow_KeyBinding", out LocalizedLabel caption);
            UiFactory.AddImage(root, BuildUtility.Sprite("ui-common/row-background"));
            var slots = new Object[2];
            var glyphs = new Object[2];
            var texts = new Object[2];
            for (int i = 0; i < slots.Length; i++)
            {
                float x = -RowInset - (slots.Length - 1 - i) * (SlotSize.x + GlyphGap);
                RectTransform slot = UiFactory.Rect("Slot" + (i + 1), root).Place(UiFactory.MiddleRight, new Vector2(x, 0f), SlotSize);
                slots[i] = UiFactory.AddButton(slot, BuildUtility.Sprite("ui-common/button-secondary"));
                RectTransform glyphRow = UiFactory.Rect("GlyphRow", slot).Stretch();
                UiFactory.AddRow(glyphRow.gameObject, 0f, TextAnchor.MiddleCenter);
                glyphs[i] = PrefabBuilder.GlyphSlot(glyphRow, "Glyph");
                TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect("State", slot).Stretch(12f, 0f, 12f, 0f), string.Empty, SlotTextSize,
                    Palette.TextMuted);
                text.raycastTarget = false;
                text.enableAutoSizing = true;
                text.fontSizeMin = SlotTextMinSize;
                text.fontSizeMax = SlotTextSize;
                texts[i] = text;
            }

            Component row = root.gameObject.AddComponent(GameTypes.KeyBindingSettingRow);
            Fields.Set(row, ("_caption", caption), ("_slots", slots), ("_glyphs", glyphs), ("_texts", texts),
                ("_unboundKey", "settings.rebind.unbound"), ("_listeningKeyboardKey", "settings.rebind.listening.keyboard"),
                ("_listeningGamepadKey", "settings.rebind.listening.gamepad"));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.SettingRowKeyBinding);
        }

        private static void BuildButtonRow()
        {
            RectTransform root = RowRoot("SettingRow_Button", out LocalizedLabel caption);
            Button button = UiFactory.AddButton(root, BuildUtility.Sprite("ui-common/button-secondary"));
            Component row = root.gameObject.AddComponent(GameTypes.ButtonSettingRow);
            Fields.Set(row, ("_caption", caption), ("_button", button));
            BuildUtility.SavePrefab(root.gameObject, BuildPaths.SettingRowButton);
        }

        // Tab row on top; pages (left) and description (right) in the middle; footer buttons and prompts at the bottom.
        private static void BuildSettingsPopup()
        {
            SettingsCatalog catalog = BuildUtility.Load<SettingsProfile>(BuildPaths.SettingsProfile).Catalog;
            List<SettingsTab> tabs = catalog != null ? SettingsCatalogBuilder.DeclaredTabs(catalog) : new List<SettingsTab>();
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_Settings", "settings.title", PanelSize);
            GameObject root = parts.Root;
            RectTransform content = parts.Body;
            Component popup = root.AddComponent(GameTypes.SettingsPopup);
            root.AddComponent<InputContextRequest>();

            RectTransform tabRow = UiFactory.Rect("TabRow", content).TopStretch(0f, TabHeight);
            RectTransform body = UiFactory.Rect("PageArea", content).Stretch(0f, FooterHeight + 16f, 0f, TabHeight + 16f);
            RectTransform pagesArea = UiFactory.Rect("Pages", body).Stretch(0f, 0f, DescriptionWidth + BodyGap, 0f);
            float tabWidth = tabs.Count > 0
                ? Mathf.Min(MaxTabWidth, (PanelSize.x - 80f - (tabs.Count - 1) * TabGap) / tabs.Count)
                : MaxTabWidth;
            var tabButtons = new Button[tabs.Count];
            var tabImages = new Image[tabs.Count];
            var pageObjects = new Object[tabs.Count];
            var pages = new Object[tabs.Count];
            float firstX = -(tabs.Count - 1) * (tabWidth + TabGap) / 2f;
            for (int i = 0; i < tabs.Count; i++)
            {
                RectTransform tab = UiFactory.Rect(tabs[i].Id + "-tab", tabRow)
                    .Place(UiFactory.TopCenter, new Vector2(firstX + i * (tabWidth + TabGap), 0f), new Vector2(tabWidth, TabHeight));
                tabButtons[i] = UiFactory.AddButton(tab, BuildUtility.Sprite("ui-common/button-secondary"));
                tabImages[i] = (Image)tabButtons[i].targetGraphic;
                UiFactory.Label(tab, string.Empty, TabLabelSize).Localize(tabs[i].LabelKey);

                RectTransform page = UiFactory.Rect(tabs[i].Id + "-page", pagesArea).Stretch();
                pages[i] = BuildPage(page, tabs[i].Id, SettingsCatalogBuilder.BindingSchemes(catalog, tabs[i].Id));
                page.gameObject.SetActive(i == 0);
                pageObjects[i] = page.gameObject;
            }

            BuildDescription(body, out GameObject descriptionPanel, out LocalizedLabel descriptionTitle, out LocalizedLabel descriptionBody);

            Button apply = FooterButton(parts.Footer, "ApplyButton", -1.5f, "ui-common/button-primary", "settings.apply");
            Button revert = FooterButton(parts.Footer, "RevertButton", -0.5f, "ui-common/button-secondary", "settings.revert");
            Button defaults = FooterButton(parts.Footer, "DefaultsButton", 0.5f, "ui-common/button-secondary", "settings.defaults");
            Button close = FooterButton(parts.Footer, "CloseButton", 1.5f, "ui-common/button-secondary", "common.close");
            RectTransform prompts = PrefabBuilder.PromptRow(parts.Footer, "Prompts", 0f, PromptRowHeight);
            var promptObjects = new Dictionary<string, GameObject>();
            foreach ((string name, string[] actions, string labelKey) in FooterPrompts)
            {
                promptObjects[name] = PrefabBuilder.AddPrompt(prompts, name, actions, labelKey);
            }

            // Navigation is rewritten by the popup from state; tabs have no left/right neighbour (SettingsTabButton steps).
            UiFactory.LinkHorizontal(apply, revert, defaults, close);
            var tabGroup = pagesArea.gameObject.AddComponent<TabGroup>();
            Fields.Set(tabGroup, ("_pages", pageObjects), ("_tabButtons", tabButtons), ("_selectedIndex", 0), ("_wrap", true));
            foreach (Button tabButton in tabButtons)
            {
                Fields.Set(tabButton.gameObject.AddComponent(GameTypes.SettingsTabButton), ("_tabs", tabGroup));
            }

            // Last child of the popup: above the panel while a key binding slot listens.
            RectTransform blocker = UiFactory.Rect("RebindBlocker", root.transform).Stretch();
            UiFactory.AddRaycastTarget(blocker);
            // Not a Selectable: navigation is off while listening, and it must never take the selection.
            RectTransform cancelRect = UiFactory.Rect("CancelButton", blocker)
                .Place(UiFactory.Center, new Vector2(0f, -PanelSize.y / 2f + FooterHeight / 2f), FooterButtonSize);
            UiFactory.AddImage(cancelRect, BuildUtility.Sprite("ui-common/button-secondary"));
            UiFactory.Label(cancelRect, string.Empty, PopupPrefabBuilder.ButtonLabelSize).Localize("common.cancel");
            Component rebindCancel = cancelRect.gameObject.AddComponent(GameTypes.RebindCancelButton);
            blocker.gameObject.SetActive(false);

            if (tabButtons.Length > 0)
            {
                PopupPrefabBuilder.SetFirstSelected(root, tabButtons[0]);
            }

            Fields.Set(popup, ("_viewId", "popup.settings"), ("_tabs", tabGroup), ("_pages", pages), ("_tabButtons", tabButtons),
                ("_tabBackgrounds", tabImages), ("_selectedTabSprite", BuildUtility.Sprite("ui-common/button-primary")),
                ("_idleTabSprite", BuildUtility.Sprite("ui-common/button-secondary")),
                ("_toggleRow", BuildUtility.PrefabComponent(BuildPaths.SettingRowToggle, GameTypes.ToggleSettingRow)),
                ("_sliderRow", BuildUtility.PrefabComponent(BuildPaths.SettingRowSlider, GameTypes.SliderSettingRow)),
                ("_choiceRow", BuildUtility.PrefabComponent(BuildPaths.SettingRowChoice, GameTypes.ChoiceSettingRow)),
                ("_keyBindingRow", BuildUtility.PrefabComponent(BuildPaths.SettingRowKeyBinding, GameTypes.KeyBindingSettingRow)),
                ("_buttonRow", BuildUtility.PrefabComponent(BuildPaths.SettingRowButton, GameTypes.ButtonSettingRow)),
                ("_applyButton", apply), ("_revertButton", revert), ("_defaultsButton", defaults), ("_closeButton", close),
                ("_applyPrompt", promptObjects["ApplyPrompt"]), ("_revertPrompt", promptObjects["RevertPrompt"]),
                ("_descriptionPanel", descriptionPanel), ("_descriptionTitle", descriptionTitle), ("_descriptionBody", descriptionBody),
                ("_rebindBlocker", blocker.gameObject), ("_rebindCancelButton", rebindCancel), ("_cancelKey", "common.cancel"),
                ("_swapTitleKey", "settings.rebind.swap.title"), ("_swapMessageKey", "settings.rebind.swap.message"),
                ("_swapConfirmKey", "settings.rebind.swap.confirm"), ("_defaultsTitleKey", "settings.defaults.title"),
                ("_defaultsMessageKey", "settings.defaults.message"), ("_defaultsConfirmKey", "settings.defaults.confirm"),
                ("_pendingTitleKey", "settings.pending.title"), ("_pendingMessageKey", "settings.pending.message"),
                ("_pendingApplyKey", "settings.apply"), ("_pendingDiscardKey", "settings.pending.discard"),
                ("_buttonMessageKey", "settings.button.message"), ("_buttonConfirmKey", "common.ok"),
                ("_volumePreview", BuildUtility.Load<AudioClip>(BuildPaths.VolumePreview)));
            BuildUtility.SavePrefab(root, BuildPaths.PopupSettings);
        }

        // Message with the countdown, then Keep (left) and Revert (right); the scope starts on Revert, the safe choice.
        private static void BuildDisplayConfirm()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_DisplayConfirm", "settings.display.keep.title", DialogSize);
            GameObject root = parts.Root;
            RectTransform content = parts.Body;
            Component popup = root.AddComponent(GameTypes.DisplayConfirmPopup);
            TextMeshProUGUI message = UiFactory.AddText(UiFactory.Rect("MessageLabel", content).Stretch(20f, 130f, 20f, 0f), string.Empty,
                PopupPrefabBuilder.MessageSize, Palette.Text);
            message.raycastTarget = false;
            message.Paragraph();
            var messageLabel = message.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(messageLabel, ("_label", message));
            Button keep = PopupPrefabBuilder.DialogButton(parts.Footer, "KeepButton", -180f, "ui-common/button-primary",
                "settings.display.keep.confirm", out LocalizedLabel _);
            Button revert = PopupPrefabBuilder.DialogButton(parts.Footer, "RevertButton", 180f, "ui-common/button-secondary",
                "settings.display.keep.revert", out LocalizedLabel _);
            UiFactory.LinkHorizontal(keep, revert);
            PopupPrefabBuilder.SetFirstSelected(root, revert);
            Fields.Set(popup, ("_viewId", "popup.display-confirm"), ("_message", messageLabel),
                ("_messageKey", "settings.display.keep.message"), ("_keepButton", keep), ("_revertButton", revert));
            BuildUtility.SavePrefab(root, BuildPaths.PopupDisplayConfirm);
        }

        // Optional scheme bar, then a vertical scroll list the rows are created in.
        private static Component BuildPage(RectTransform page, string tabId, List<string> schemes)
        {
            var schemeButtons = new Button[schemes.Count];
            var schemeImages = new Image[schemes.Count];
            for (int i = 0; i < schemes.Count; i++)
            {
                RectTransform rect = UiFactory.Rect("Scheme-" + schemes[i], page)
                    .Place(UiFactory.TopLeft, new Vector2(i * (SchemeButtonSize.x + GlyphGap), 0f), SchemeButtonSize);
                schemeButtons[i] = UiFactory.AddButton(rect, BuildUtility.Sprite("ui-common/button-secondary"));
                schemeImages[i] = (Image)schemeButtons[i].targetGraphic;
                UiFactory.Label(rect, string.Empty, TabLabelSize).Localize("settings.controls.scheme." + schemes[i].ToLowerInvariant());
            }

            RectTransform scroll = UiFactory.Rect("Scroll", page).Stretch(0f, 0f, 0f, schemes.Count > 0 ? SchemeBarHeight : 0f);
            RectTransform viewport = UiFactory.Rect("Viewport", scroll).Stretch();
            // Catches the mouse wheel over gaps between rows.
            UiFactory.AddRaycastTarget(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform list = UiFactory.Rect("Content", viewport).TopStretch(0f, 0f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RowSpacing;
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

            Component component = page.gameObject.AddComponent(GameTypes.SettingsPage);
            Fields.Set(component, ("_tabId", tabId), ("_content", list), ("_schemes", schemes.ToArray()), ("_schemeButtons", schemeButtons),
                ("_schemeBackgrounds", schemeImages), ("_selectedSchemeSprite", BuildUtility.Sprite("ui-common/button-primary")),
                ("_idleSchemeSprite", BuildUtility.Sprite("ui-common/button-secondary")));
            return component;
        }

        private static void BuildDescription(RectTransform body, out GameObject panel, out LocalizedLabel title, out LocalizedLabel text)
        {
            RectTransform description = UiFactory.Rect("Description", body).RightFill(DescriptionWidth);
            UiFactory.AddImage(description, BuildUtility.Sprite("ui-common/row-background")).raycastTarget = false;
            RectTransform holder = UiFactory.Rect("Text", description).Stretch();
            TextMeshProUGUI titleText = UiFactory.AddText(UiFactory.Rect("Title", holder).TopStretch(-20f, 60f, RowInset, RowInset),
                string.Empty, DescriptionTitleSize, Palette.Text, TextAlignmentOptions.TopLeft);
            titleText.raycastTarget = false;
            title = titleText.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(title, ("_label", titleText));
            TextMeshProUGUI bodyText = UiFactory.AddText(UiFactory.Rect("DescriptionLabel", holder).Stretch(RowInset, RowInset, RowInset, 96f),
                string.Empty, DescriptionBodySize, Palette.TextMuted, TextAlignmentOptions.TopLeft);
            bodyText.raycastTarget = false;
            bodyText.Paragraph();
            text = bodyText.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(text, ("_label", bodyText));
            holder.gameObject.SetActive(false);
            panel = holder.gameObject;
        }

        // Row root sized by its LayoutElement, with the caption on the left.
        private static RectTransform RowRoot(string name, out LocalizedLabel caption)
        {
            RectTransform root = UiFactory.Rect(name, null).Place(UiFactory.Center, Vector2.zero, RowSize);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect("Caption", root).LeftFill(CaptionWidth, RowInset), string.Empty,
                CaptionSize, Palette.Text, TextAlignmentOptions.MidlineLeft);
            text.raycastTarget = false;
            caption = text.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(caption, ("_label", text));
            return root;
        }

        private static Button Arrow(RectTransform row, string name, float x, string sprite)
        {
            RectTransform rect = UiFactory.Rect(name, row).Place(UiFactory.MiddleRight, new Vector2(x, 0f), ArrowSize);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = UiFactory.AddImage(rect, BuildUtility.Sprite(sprite));
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        private static Button FooterButton(RectTransform parent, string name, float slot, string sprite, string key)
        {
            RectTransform rect = UiFactory.Rect(name, parent)
                .Place(UiFactory.BottomCenter, new Vector2(slot * FooterButtonSpacing, PromptRowHeight + FooterGap), FooterButtonSize);
            Button button = UiFactory.AddButton(rect, BuildUtility.Sprite(sprite));
            UiFactory.Label(rect, string.Empty, PopupPrefabBuilder.ButtonLabelSize).Localize(key);
            return button;
        }
    }
}
