using Game.Foundation.UI;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// <c>FirstLaunchCanvas</c>: title, three labelled choice rows (language, interface size, brightness; the screen fills
    /// them with buttons at runtime from an inactive choice template), the photosensitivity notice, Continue, the prompt bar
    /// (Confirm, Skip) and its own selection highlight. The screen is the focus scope; Continue is the builder's first
    /// selection, replaced at runtime by the language in use.
    /// </summary>
    internal static class FirstLaunchPrefabBuilder
    {
        private const int ContentSortOrder = 0;
        private const float TitleSize = 72f;
        private const float HeaderSize = 34f;
        private const float ChoiceLabelSize = 32f;
        private const float NoticeSize = 26f;
        private const float ContinueLabelSize = 44f;
        private static readonly Vector2 ChoiceSize = new Vector2(280f, 84f);
        private static readonly Vector2 ContinueSize = new Vector2(520f, 100f);

        /// <summary>Choice rows, top to bottom: child name, header key, y position.</summary>
        private static readonly (string name, string headerKey, float y)[] Rows =
        {
            ("LanguageRow", "first-launch.language", 270f),
            ("UIScaleRow", "first-launch.ui-scale", 140f),
            ("BrightnessRow", "first-launch.brightness", 10f),
        };

        internal static void BuildCanvas()
        {
            ScreenParts parts = ScreenParts.Create("FirstLaunchCanvas", ContentSortOrder, true, "backgrounds/first-launch-background");
            RectTransform canvas = parts.Canvas;
            canvas.gameObject.AddComponent<CanvasUIScale>();
            RectTransform screen = parts.Screen;
            var scope = screen.gameObject.AddComponent<FocusScope>();
            Component view = screen.gameObject.AddComponent(GameTypes.FirstLaunchScreen);

            UiFactory.AddText(UiFactory.Rect("TitleLabel", parts.Header).Place(UiFactory.TopCenter, new Vector2(0f, -90f), new Vector2(1400f, 110f)),
                string.Empty, TitleSize, Palette.Text).Localize("first-launch.title").raycastTarget = false;

            var rows = new RectTransform[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                RectTransform holder = UiFactory.Rect(Rows[i].name, parts.Body).Place(UiFactory.Center, new Vector2(0f, Rows[i].y), new Vector2(1500f, 100f));
                UiFactory.AddText(UiFactory.Rect("Header", holder).LeftFill(380f), string.Empty, HeaderSize, Palette.TextMuted,
                    TextAlignmentOptions.MidlineRight).Localize(Rows[i].headerKey).raycastTarget = false;
                rows[i] = UiFactory.Rect("Choices", holder).Stretch(420f, 0f, 0f, 0f);
                UiFactory.AddRow(rows[i].gameObject, 24f, TextAnchor.MiddleLeft);
            }

            // Inactive template the screen clones into the rows.
            RectTransform choice = UiFactory.Rect("ChoiceTemplate", parts.Templates).Place(UiFactory.Center, Vector2.zero, ChoiceSize);
            var choiceLayout = choice.gameObject.AddComponent<LayoutElement>();
            choiceLayout.preferredWidth = ChoiceSize.x;
            choiceLayout.preferredHeight = ChoiceSize.y;
            Button choiceButton = UiFactory.AddButton(choice, BuildUtility.Sprite("ui-common/button-secondary"));
            TextMeshProUGUI choiceLabel = UiFactory.Label(choice, string.Empty, ChoiceLabelSize);
            RectTransform marker = UiFactory.Rect("Chosen", choice).Stretch();
            UiFactory.AddImage(marker, BuildUtility.Sprite("screens/first-launch/choice-chosen")).raycastTarget = false;
            marker.gameObject.SetActive(false);
            Fields.Set(choice.gameObject.AddComponent(GameTypes.FirstLaunchChoice), ("_button", choiceButton), ("_label", choiceLabel),
                ("_marker", marker.gameObject));

            RectTransform notice = UiFactory.Rect("Notice", parts.Body).Place(UiFactory.Center, new Vector2(0f, -170f), new Vector2(1500f, 190f));
            Image icon = UiFactory.AddImage(UiFactory.Rect("Icon", notice).Place(UiFactory.MiddleLeft, new Vector2(40f, 0f), new Vector2(80f, 80f)),
                BuildUtility.Sprite("screens/first-launch/photosensitivity-icon"));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UiFactory.AddText(UiFactory.Rect("Text", notice).Stretch(160f, 0f, 20f, 0f), string.Empty, NoticeSize, Palette.Text,
                TextAlignmentOptions.MidlineLeft).Localize("first-launch.photosensitivity").raycastTarget = false;

            RectTransform continueRect = UiFactory.Rect("ContinueButton", parts.Footer).Place(UiFactory.Center, new Vector2(0f, -360f), ContinueSize);
            Button continueButton = UiFactory.AddButton(continueRect, BuildUtility.Sprite("ui-common/button-primary"));
            UiFactory.Label(continueRect, string.Empty, ContinueLabelSize).Localize("first-launch.continue");

            GameObject bar = PrefabBuilder.PromptBarInstance(parts.Footer, 24f, 64f, PrefabBuilder.Prompts.SwitchTab);
            PrefabBuilder.SetPromptLabel(bar, PrefabBuilder.Prompts.Back, "prompt.skip");

            ShellPrefabBuilder.AddSelectionHighlight(parts.Overlay);

            Fields.Set(scope, ("_firstSelected", continueButton), ("_trapFocus", true),
                ("_cancelMode", new EnumName(nameof(FocusCancelMode.PassThrough))));
            Fields.Set(view, ("_config", BuildUtility.Load<ScriptableObject>(BuildPaths.FirstLaunchConfig)), ("_scope", scope),
                ("_languageRow", rows[0]), ("_uiScaleRow", rows[1]), ("_brightnessRow", rows[2]),
                ("_choiceTemplate", choice.GetComponent(GameTypes.FirstLaunchChoice)), ("_continueButton", continueButton));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.FirstLaunchCanvas);
        }
    }
}
