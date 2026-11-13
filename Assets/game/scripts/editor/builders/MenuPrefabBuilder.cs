using Game.Contracts.Navigation;
using Game.Foundation.UI;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Canvases of the content scenes, one prefab each: the splash (logo, studio name, loading indicator, driven by
    /// <c>SplashScreen</c>), the main menu (title, Play/Settings/Quit, version, prompt bar, its own selection highlight) and,
    /// through <see cref="GameplayPrefabBuilder"/>, the gameplay canvas. Content canvases sort at 0, under the persistent UI
    /// scene's canvases.
    /// </summary>
    internal static class MenuPrefabBuilder
    {
        private const int ContentSortOrder = 0;
        private const float StudioNameSize = 48f;
        private const float IndicatorSize = 32f;
        private const float TitleSize = 104f;
        private const float MenuButtonLabelSize = 44f;
        private const float VersionSize = 26f;
        private const float MenuButtonSpacing = 120f;
        private const float MenuTopButtonY = 140f;
        private static readonly Vector2 LogoSize = new Vector2(320f, 320f);
        private static readonly Vector2 MenuButtonSize = new Vector2(520f, 100f);

        /// <summary>Main menu buttons, top to bottom: child name, sprite, label key.</summary>
        private static readonly (string name, string sprite, string key)[] MenuButtons =
        {
            ("PlayButton", "ui-common/button-primary", "menu.play"),
            ("SettingsButton", "ui-common/button-secondary", "menu.settings"),
            ("QuitButton", "ui-common/button-secondary", "menu.quit"),
        };

        internal static void Build()
        {
            BuildSplash();
            BuildMainMenu();
            GameplayPrefabBuilder.BuildCanvas();
            GameplayPrefabBuilder.BuildBattlefield();
        }

        // The logo group (logo and studio name) starts transparent: SplashScreen draws its alpha from the sequence.
        private static void BuildSplash()
        {
            ScreenParts parts = ScreenParts.Create("SplashCanvas", ContentSortOrder, false, "backgrounds/splash-background");
            RectTransform canvas = parts.Canvas;

            RectTransform logo = UiFactory.Rect("Logo", parts.Body).Place(UiFactory.Center, new Vector2(0f, 40f), new Vector2(800f, 460f));
            var logoGroup = logo.gameObject.AddComponent<CanvasGroup>();
            logoGroup.alpha = 0f;
            logoGroup.interactable = false;
            logoGroup.blocksRaycasts = false;
            Image logoImage = UiFactory.AddImage(UiFactory.Rect("LogoImage", logo).Place(UiFactory.TopCenter, Vector2.zero, LogoSize),
                BuildUtility.Sprite("screens/splash/splash-logo"));
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            UiFactory.AddText(UiFactory.Rect("StudioNameLabel", logo).BottomStretch(0f, 90f), string.Empty, StudioNameSize, Palette.Text)
                .Localize("studio.name").raycastTarget = false;

            RectTransform indicator = UiFactory.Rect("LoadingLabel", parts.Footer)
                .Place(UiFactory.BottomCenter, new Vector2(0f, 80f), new Vector2(600f, 60f));
            UiFactory.AddText(indicator, string.Empty, IndicatorSize, Palette.TextMuted).Localize("loading.label").raycastTarget = false;
            indicator.gameObject.SetActive(false);

            Fields.Set(parts.Screen.gameObject.AddComponent(GameTypes.SplashScreen),
                ("_config", BuildUtility.Load<ScriptableObject>(BuildPaths.SplashConfig)), ("_logoGroup", logoGroup),
                ("_loadingIndicator", indicator.gameObject));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.SplashCanvas);
        }

        // The screen is the focus scope (Play first), keeps the Menu input context on and owns the Quit-on-Cancel handler.
        private static void BuildMainMenu()
        {
            ScreenParts parts = ScreenParts.Create("MainMenuCanvas", ContentSortOrder, true, "backgrounds/menu-background");
            RectTransform canvas = parts.Canvas;
            canvas.gameObject.AddComponent<CanvasUIScale>();
            RectTransform screen = parts.Screen;
            var scope = screen.gameObject.AddComponent<FocusScope>();
            screen.gameObject.AddComponent<InputContextRequest>();
            Component menu = screen.gameObject.AddComponent(GameTypes.MainMenuScreen);

            UiFactory.AddText(UiFactory.Rect("TitleLabel", parts.Header).Place(UiFactory.TopCenter, new Vector2(0f, -150f), new Vector2(1400f, 160f)),
                string.Empty, TitleSize, Palette.Text).Localize("game.title").raycastTarget = false;

            var buttons = new Button[MenuButtons.Length];
            for (int i = 0; i < MenuButtons.Length; i++)
            {
                RectTransform rect = UiFactory.Rect(MenuButtons[i].name, parts.Body)
                    .Place(UiFactory.Center, new Vector2(0f, MenuTopButtonY - i * MenuButtonSpacing), MenuButtonSize);
                buttons[i] = UiFactory.AddButton(rect, BuildUtility.Sprite(MenuButtons[i].sprite));
                UiFactory.Label(rect, string.Empty, MenuButtonLabelSize).Localize(MenuButtons[i].key);
            }

            UiFactory.LinkVertical(buttons);

            // The version stamp is written at runtime (Core's BuildInfo); it is not translated.
            TextMeshProUGUI version = UiFactory.AddText(UiFactory.Rect("VersionLabel", parts.Footer)
                .Place(UiFactory.BottomLeft, new Vector2(48f, 100f), new Vector2(900f, 40f)), string.Empty, VersionSize, Palette.TextMuted,
                TextAlignmentOptions.MidlineLeft);
            version.raycastTarget = false;

            GameObject bar = PrefabBuilder.PromptBarInstance(parts.Footer, 24f, 64f, PrefabBuilder.Prompts.SwitchTab);
            PrefabBuilder.SetPromptLabel(bar, PrefabBuilder.Prompts.Back, "prompt.quit");

            ShellPrefabBuilder.AddSelectionHighlight(parts.Overlay);

            Fields.Set(scope, ("_firstSelected", buttons[0]), ("_trapFocus", true),
                ("_cancelMode", new EnumName(nameof(FocusCancelMode.PassThrough))));
            Fields.Set(menu, ("_playButton", buttons[0]), ("_settingsButton", buttons[1]),
                ("_quitButton", buttons[2]), ("_versionText", version),
                ("_gameplayScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyGameplay)),
                ("_quitTitleKey", "quit.title"), ("_quitMessageKey", "quit.message"), ("_quitConfirmKey", "quit.confirm"),
                ("_quitCancelKey", "common.cancel"));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.MainMenuCanvas);
        }
    }
}
