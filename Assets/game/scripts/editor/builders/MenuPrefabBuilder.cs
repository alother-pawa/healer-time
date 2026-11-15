using Game.Contracts.Navigation;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Canvases of the content scenes, one prefab each: the splash (logo, studio name, loading indicator, driven by
    /// <c>SplashScreen</c>), the main menu (title, Play, Characters/How to play/Settings/Quit, prompt bar, its own selection
    /// highlight) and, through <see cref="GameplayPrefabBuilder"/>, the gameplay canvas. Content canvases sort at 0, under the persistent UI
    /// scene's canvases.
    /// </summary>
    internal static class MenuPrefabBuilder
    {
        private const int ContentSortOrder = 0;
        private const float StudioNameSize = 48f;
        private const float IndicatorSize = 32f;
        private const float TitleSize = 104f;
        private const float MenuButtonLabelSize = 44f;
        private const float MenuButtonGap = 40f;
        private const float CardGap = 20f;
        private static readonly Vector2 CardSize = new Vector2(168f, 236f);
        private static readonly Vector2 LogoSize = new Vector2(320f, 320f);
        private static readonly Vector2 MenuButtonSize = new Vector2(340f, 96f);

        /// <summary>Main menu buttons under Play, left to right: child name, sprite, label key.</summary>
        private static readonly (string name, string sprite, string key)[] MenuButtons =
        {
            ("CharactersButton", "ui-common/button-secondary", "menu.characters"),
            ("HowToPlayButton", "ui-common/button-secondary", "menu.how-to-play"),
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

        // Two pages in Body (home at 0, characters one page to the right; the screen slides them). The screen is the focus
        // scope (Play first), keeps the Menu input context on and owns the Cancel handler (back to home, or Quit).
        private static void BuildMainMenu()
        {
            ScreenParts parts = ScreenParts.Create("MainMenuCanvas", ContentSortOrder, true, "backgrounds/menu-background");
            RectTransform canvas = parts.Canvas;
            canvas.gameObject.AddComponent<CanvasUIScale>();
            RectTransform screen = parts.Screen;
            var scope = screen.gameObject.AddComponent<FocusScope>();
            screen.gameObject.AddComponent<InputContextRequest>();
            Component menu = screen.gameObject.AddComponent(GameTypes.MainMenuScreen);

            RectTransform pages = UiFactory.Rect("Pages", parts.Body).Stretch();
            RectTransform home = UiFactory.Rect("HomePage", pages).Stretch();
            TextMeshProUGUI title = UiFactory.AddText(UiFactory.Rect("TitleLabel", home).Place(UiFactory.TopCenter, new Vector2(0f, -90f),
                new Vector2(1400f, 150f)), string.Empty, TitleSize, Palette.TextLight).Localize("game.title");
            title.raycastTarget = false;
            UiFactory.Outlined(title);

            RectTransform wallet = UiFactory.Rect("Wallet", home).Place(UiFactory.TopLeft, new Vector2(40f, -36f), new Vector2(260f, 80f));
            UiFactory.AddImage(wallet, BuildUtility.Sprite("ui-common/panel-wood")).raycastTarget = false;
            UiFactory.AddImage(UiFactory.Rect("CoinIcon", wallet).Place(UiFactory.MiddleLeft, new Vector2(16f, 0f), new Vector2(52f, 52f)),
                BuildUtility.Sprite("ui-common/coin-icon")).raycastTarget = false;
            TextMeshProUGUI coins = UiFactory.AddText(UiFactory.Rect("CoinsLabel", wallet).Stretch(76f, 0f, 16f, 0f), "0", 40f,
                Palette.TextLight, TextAlignmentOptions.MidlineLeft);
            coins.raycastTarget = false;

            UiFactory.AddImage(UiFactory.Rect("Stage", home).Place(UiFactory.Center, new Vector2(0f, 40f), new Vector2(760f, 150f)),
                BuildUtility.Sprite("screens/main-menu/stage")).raycastTarget = false;
            // Feet on the stage: the hero facing right, the healer facing the hero.
            Component heroPreview = Preview(home, "HeroPreview", new Vector2(-190f, -10f), false, 1f);
            Component healerPreview = Preview(home, "HealerPreview", new Vector2(210f, -10f), true, 1f);

            RectTransform playRect = UiFactory.Rect("PlayButton", home).Place(UiFactory.Center, new Vector2(0f, -210f), new Vector2(560f, 130f));
            Button play = UiFactory.AddButton(playRect, BuildUtility.Sprite("ui-common/button-primary"));
            UiFactory.Label(playRect, string.Empty, 64f).Localize("menu.play");

            // A centred row: a button hidden at runtime (Quit on the web) closes up instead of leaving a gap.
            RectTransform buttonRow = UiFactory.Rect("MenuButtons", home).Place(UiFactory.Center, new Vector2(0f, -370f),
                new Vector2(1840f, MenuButtonSize.y));
            HorizontalLayoutGroup rowLayout = UiFactory.AddRow(buttonRow.gameObject, MenuButtonGap, TextAnchor.MiddleCenter);
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            var row = new Button[MenuButtons.Length];
            for (int i = 0; i < MenuButtons.Length; i++)
            {
                RectTransform rect = UiFactory.Rect(MenuButtons[i].name, buttonRow).Place(UiFactory.Center, Vector2.zero, MenuButtonSize);
                row[i] = UiFactory.AddButton(rect, BuildUtility.Sprite(MenuButtons[i].sprite));
                UiFactory.Label(rect, string.Empty, MenuButtonLabelSize).Localize(MenuButtons[i].key);
            }

            UiFactory.LinkHorizontal(row);
            UiFactory.LinkDown(row[1], play);
            foreach (Button button in row)
            {
                Navigation navigation = button.navigation;
                navigation.selectOnUp = play;
                button.navigation = navigation;
            }

            Component characters = BuildCharactersPage(parts, pages, out Button back);

            GameObject bar = PrefabBuilder.PromptBarInstance(parts.Footer, 24f, 64f, PrefabBuilder.Prompts.SwitchTab);
            PrefabBuilder.SetPromptLabel(bar, PrefabBuilder.Prompts.Back, "prompt.back");

            ShellPrefabBuilder.AddSelectionHighlight(parts.Overlay);

            Fields.Set(scope, ("_firstSelected", play), ("_trapFocus", true),
                ("_cancelMode", new EnumName(nameof(FocusCancelMode.PassThrough))));
            Fields.Set(menu, ("_pages", pages), ("_characters", characters),
                ("_playButton", play), ("_charactersButton", row[0]), ("_howToPlayButton", row[1]), ("_settingsButton", row[2]),
                ("_quitButton", row[3]),
                ("_backButton", back), ("_heroPreview", heroPreview), ("_healerPreview", healerPreview), ("_coinsText", coins),
                ("_coin", BuildUtility.Load<ScriptableObject>(BuildPaths.Data + "/economy/Currency_Coin.asset")),
                ("_gameplayScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyGameplay)),
                ("_music", AssetBuilder.Clip("music/menu-theme")), ("_swipe", AssetBuilder.Clip("ui/page-swipe")),
                ("_quitTitleKey", "quit.title"), ("_quitMessageKey", "quit.message"), ("_quitConfirmKey", "quit.confirm"),
                ("_quitCancelKey", "common.cancel"));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.MainMenuCanvas);
        }

        // Back (top left), title, Hero/Healer tabs, the card list (cards cloned from a template) and the detail panel.
        private static Component BuildCharactersPage(ScreenParts parts, RectTransform pages, out Button back)
        {
            // One page width to the right of the home page.
            RectTransform page = UiFactory.Rect("CharactersPage", pages).Stretch();
            page.anchorMin = new Vector2(1f, 0f);
            page.anchorMax = new Vector2(2f, 1f);
            Component view = page.gameObject.AddComponent(GameTypes.CharacterPage);

            RectTransform backRect = UiFactory.Rect("BackButton", page).Place(UiFactory.TopLeft, new Vector2(40f, -36f), new Vector2(102f, 102f));
            back = UiFactory.AddButton(backRect, BuildUtility.Sprite("screens/main-menu/arrow-left"));
            TextMeshProUGUI title = UiFactory.AddText(UiFactory.Rect("TitleLabel", page).Place(UiFactory.TopCenter, new Vector2(0f, -50f),
                new Vector2(1200f, 110f)), string.Empty, 80f, Palette.TextLight).Localize("characters.title");
            title.raycastTarget = false;
            UiFactory.Outlined(title);

            Button heroTab = Tab(page, "HeroTab", new Vector2(-560f, 230f), "characters.tab.hero", out GameObject heroMark);
            Button healerTab = Tab(page, "HealerTab", new Vector2(-220f, 230f), "characters.tab.healer", out GameObject healerMark);
            UiFactory.LinkHorizontal(heroTab, healerTab);

            RectTransform list = UiFactory.Rect("CardList", page).Place(UiFactory.Center, new Vector2(-390f, -40f), new Vector2(760f, 300f));
            // Four heroes fit side by side in the list, clear of the detail panel.
            HorizontalLayoutGroup layout = UiFactory.AddRow(list.gameObject, CardGap, TextAnchor.MiddleCenter);
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            RectTransform card = UiFactory.Rect("CardTemplate", parts.Templates).Place(UiFactory.Center, Vector2.zero, CardSize);
            Button cardButton = UiFactory.AddButton(card, BuildUtility.Sprite("screens/main-menu/character-card"));
            Component cardPreview = Preview(card, "Preview", new Vector2(0f, -40f), false, 0.4f);
            TextMeshProUGUI cardName = UiFactory.AddText(UiFactory.Rect("NameLabel", card).BottomStretch(14f, 56f, 8f, 8f), string.Empty, 30f,
                Palette.Text).Localize("hero.martial-hero.name");
            cardName.raycastTarget = false;
            Component cardView = card.gameObject.AddComponent(GameTypes.CharacterCard);
            Fields.Set(cardView, ("_button", cardButton), ("_background", card.GetComponent<Image>()),
                ("_normal", BuildUtility.Sprite("screens/main-menu/character-card")),
                ("_chosen", BuildUtility.Sprite("screens/main-menu/character-card-selected")), ("_preview", cardPreview),
                ("_name", cardName.GetComponent<LocalizedLabel>()));

            RectTransform detail = UiFactory.Rect("Detail", page).Place(UiFactory.Center, new Vector2(430f, -40f), new Vector2(720f, 640f));
            UiFactory.AddImage(detail, BuildUtility.Sprite("ui-common/popup-background")).raycastTarget = false;
            Component preview = Preview(detail, "Preview", new Vector2(0f, -10f), true, 0.8f);
            TextMeshProUGUI name = UiFactory.AddText(UiFactory.Rect("NameLabel", detail).Place(UiFactory.Center, new Vector2(0f, -70f),
                new Vector2(640f, 70f)), string.Empty, 54f, Palette.Text).Localize("hero.martial-hero.name").OnWood();
            name.raycastTarget = false;
            name.fontStyle = FontStyles.Bold;
            TextMeshProUGUI description = UiFactory.AddText(UiFactory.Rect("DescriptionLabel", detail).Place(UiFactory.Center, new Vector2(0f, -150f),
                new Vector2(640f, 90f)), string.Empty, 32f, Palette.Text).Localize("hero.martial-hero.description").OnWood();
            description.raycastTarget = false;
            description.Paragraph();
            TextMeshProUGUI stats = UiFactory.AddText(UiFactory.Rect("StatsLabel", detail).Place(UiFactory.Center, new Vector2(0f, -240f),
                new Vector2(640f, 60f)), string.Empty, 30f, Palette.TextMuted).Localize("characters.stats.hero").OnWood(true);
            stats.raycastTarget = false;

            Fields.Set(view, ("_heroTab", heroTab), ("_healerTab", healerTab), ("_heroTabMark", heroMark), ("_healerTabMark", healerMark),
                ("_list", list), ("_cardTemplate", cardView), ("_preview", preview), ("_name", name.GetComponent<LocalizedLabel>()),
                ("_description", description.GetComponent<LocalizedLabel>()), ("_stats", stats.GetComponent<LocalizedLabel>()),
                ("_heroStatsKey", "characters.stats.hero"), ("_healerStatsKey", "characters.stats.healer"));
            return view;
        }

        private static Button Tab(RectTransform parent, string name, Vector2 position, string key, out GameObject mark)
        {
            RectTransform rect = UiFactory.Rect(name, parent).Place(UiFactory.Center, position, new Vector2(320f, 90f));
            Button button = UiFactory.AddButton(rect, BuildUtility.Sprite("ui-common/button-secondary"));
            UiFactory.Label(rect, string.Empty, 40f).Localize(key);
            RectTransform markRect = UiFactory.Rect("OpenMark", rect).Stretch(-10f, -10f, -10f, -10f);
            UiFactory.AddImage(markRect, BuildUtility.Sprite("ui-common/focus-frame")).raycastTarget = false;
            mark = markRect.gameObject;
            return button;
        }

        // A character preview playing idle frames (set at runtime): the rect's position is where the feet stand, its size and
        // pivot come from the sprite (in-game size times sizeScale); mirrored when it faces left.
        private static Component Preview(RectTransform parent, string name, Vector2 feet, bool faceLeft, float sizeScale)
        {
            RectTransform rect = UiFactory.Rect(name, parent).Place(UiFactory.Center, feet, new Vector2(100f, 100f));
            Image image = UiFactory.AddImage(rect, null);
            image.raycastTarget = false;
            image.enabled = false;
            if (faceLeft)
            {
                rect.localScale = new Vector3(-1f, 1f, 1f);
            }

            var flipbook = rect.gameObject.AddComponent<SpriteFlipbook>();
            Fields.Set(flipbook, ("_image", image), ("_sizeScale", sizeScale));
            return flipbook;
        }
    }
}
