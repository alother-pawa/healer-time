using Game.Foundation.Localization;
using Game.Foundation.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The gameplay scene's prefabs (landscape 1920×1080). The left half holds the board, as large as the height allows, with
    /// the score, monsters beaten and coins over it; the right half holds the monster's health bar and Pause on top, the
    /// battlefield (world) below them, and at the bottom the item row and the healer with the magic bar:
    /// <c>GameplayCanvas</c> (HUD, board, item bar, magic bar, showcase banner; the screen host and the run controller),
    /// <c>Battlefield</c> (world: backgrounds, hero, two monster views, healer, spots, number and burst templates) and the
    /// popups (pause menu, result; variants of <c>Popup_Base</c>).
    /// </summary>
    internal static class GameplayPrefabBuilder
    {
        private const int ContentSortOrder = 0;
        private const float MenuButtonLabelSize = 40f;
        private const float MenuButtonSpacing = 104f;
        private const float PromptRowHeight = 56f;
        private const float BoardSide = 864f;
        private const float FramePadding = 44f;
        private const float SlotSize = 184f;
        private const float SlotSpacing = 208f;
        /// <summary>Width of the item slot art's frame at <see cref="SlotSize"/> (12 of its 132 pixels).</summary>
        private const float SlotFrame = SlotSize * 12f / 132f;
        private static readonly Vector2 BoardCenter = new Vector2(480f, 498f);
        private static readonly Vector2 ItemRowCenter = new Vector2(1440f, 280f);
        private static readonly Vector2 PausePanelSize = new Vector2(760f, 860f);
        private static readonly Vector2 MenuButtonSize = new Vector2(520f, 88f);

        // World layout in units (camera at the origin, 128 px per unit, 1080 px = 8.4375 units tall): reference pixel (x, y)
        // from the bottom-left is ((x - 960) / 128, (y - 540) / 128).
        private const float GroundY = (600f - 540f) / 128f;
        private static readonly Vector3 HeroSpot = new Vector3((1570f - 960f) / 128f, GroundY, 0f);
        private static readonly Vector3 HealerSpot = new Vector3((1790f - 960f) / 128f, GroundY, 0f);
        private static readonly Vector3 MonsterSpot = new Vector3((1310f - 960f) / 128f, GroundY, 0f);
        private static readonly Vector3 MonsterEntry = new Vector3(0f, GroundY, 0f);

        /// <summary>Pause menu buttons, top to bottom: child name, sprite, label key.</summary>
        private static readonly (string name, string sprite, string key)[] PauseButtons =
        {
            ("ResumeButton", "ui-common/button-primary", "gameplay.pause.resume"),
            ("SettingsButton", "ui-common/button-secondary", "gameplay.pause.settings"),
            ("HowToPlayButton", "ui-common/button-secondary", "gameplay.pause.how-to-play"),
            ("QuitToMenuButton", "ui-common/button-secondary", "gameplay.pause.quit-menu"),
            ("QuitToDesktopButton", "ui-common/button-secondary", "gameplay.pause.quit-desktop"),
        };

        private static readonly (string name, string[] actions, string labelKey)[] PausePrompts =
        {
            (PrefabBuilder.Prompts.Confirm, new[] { "UI/Submit" }, "prompt.confirm"),
            ("ResumePrompt", new[] { "Menu/Pause" }, "prompt.resume"),
        };

        internal static void BuildPopups()
        {
            BuildPauseMenu();
            BuildResult();
        }

        // Resume (first selected), Settings, How to play, Quit to main menu, Quit to desktop, in a centred column over the
        // prompts (a button hidden at runtime, Quit to desktop on the web, closes up); Confirm and Resume prompts.
        private static void BuildPauseMenu()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_PauseMenu", "gameplay.pause.title", PausePanelSize);
            GameObject root = parts.Root;
            Component popup = root.AddComponent(GameTypes.PauseMenuPopup);
            root.AddComponent<InputContextRequest>();

            RectTransform column = UiFactory.Rect("Buttons", parts.Body).Stretch(0f, PromptRowHeight + 24f, 0f, 0f);
            var columnLayout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            columnLayout.spacing = MenuButtonSpacing - MenuButtonSize.y;
            columnLayout.childAlignment = TextAnchor.MiddleCenter;
            columnLayout.childControlWidth = false;
            columnLayout.childControlHeight = false;
            columnLayout.childForceExpandWidth = false;
            columnLayout.childForceExpandHeight = false;
            var buttons = new Button[PauseButtons.Length];
            for (int i = 0; i < PauseButtons.Length; i++)
            {
                RectTransform rect = UiFactory.Rect(PauseButtons[i].name, column).Place(UiFactory.Center, Vector2.zero, MenuButtonSize);
                buttons[i] = UiFactory.AddButton(rect, BuildUtility.Sprite(PauseButtons[i].sprite));
                UiFactory.Label(rect, string.Empty, MenuButtonLabelSize).Localize(PauseButtons[i].key);
            }

            UiFactory.LinkVertical(buttons);

            RectTransform promptRow = PrefabBuilder.PromptRow(parts.Footer, "Prompts", 0f, PromptRowHeight);
            foreach ((string name, string[] actions, string labelKey) in PausePrompts)
            {
                PrefabBuilder.AddPrompt(promptRow, name, actions, labelKey);
            }

            PopupPrefabBuilder.SetFirstSelected(root, buttons[0]);
            Fields.Set(popup, ("_viewId", "popup.pause-menu"), ("_resumeButton", buttons[0]), ("_settingsButton", buttons[1]),
                ("_howToPlayButton", buttons[2]), ("_quitToMenuButton", buttons[3]), ("_quitToDesktopButton", buttons[4]), ("_resumeAction", "Menu/Pause"));
            BuildUtility.SavePrefab(root, BuildPaths.PopupPauseMenu);
        }

        // Score (big), best, monsters, coins, new-best badge; Play again (first selected) and Main menu.
        private static void BuildResult()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_Result", "gameplay.result.title", new Vector2(900f, 720f));
            GameObject root = parts.Root;
            Component popup = root.AddComponent(GameTypes.ResultPopup);

            LocalizedLabel score = Text(parts.Body, "ScoreLabel", new Vector2(0f, -20f), new Vector2(780f, 100f), 72f, false,
                "gameplay.result.score");
            score.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            LocalizedLabel best = Text(parts.Body, "BestLabel", new Vector2(0f, -120f), new Vector2(780f, 56f), 34f, true,
                "gameplay.result.best");
            LocalizedLabel kills = Text(parts.Body, "KillsLabel", new Vector2(0f, -200f), new Vector2(780f, 56f), 38f, false,
                "gameplay.result.kills");
            LocalizedLabel coins = Text(parts.Body, "CoinsLabel", new Vector2(0f, -260f), new Vector2(780f, 56f), 38f, false,
                "gameplay.result.coins");

            RectTransform badge = UiFactory.Rect("NewBestBadge", parts.Body).Place(UiFactory.TopRight, new Vector2(10f, -6f), new Vector2(260f, 64f),
                new Vector2(1f, 1f));
            UiFactory.AddImage(badge, BuildUtility.Sprite("ui-common/popup-header")).raycastTarget = false;
            UiFactory.Label(badge, string.Empty, 32f, "BadgeLabel").Localize("gameplay.result.new-best").color = Palette.Text;
            badge.localRotation = Quaternion.Euler(0f, 0f, -8f);

            Button replay = PopupPrefabBuilder.DialogButton(parts.Footer, "ReplayButton", -180f, "ui-common/button-primary",
                "gameplay.result.replay", out _);
            Button menu = PopupPrefabBuilder.DialogButton(parts.Footer, "MenuButton", 180f, "ui-common/button-secondary",
                "gameplay.result.menu", out _);
            UiFactory.LinkHorizontal(replay, menu);
            PopupPrefabBuilder.SetFirstSelected(root, replay);
            Fields.Set(popup, ("_viewId", "popup.result"), ("_score", score), ("_best", best), ("_kills", kills), ("_coins", coins),
                ("_newBest", badge.gameObject), ("_replayButton", replay), ("_menuButton", menu));
            BuildUtility.SavePrefab(root, BuildPaths.PopupResult);
        }

        // Screen host and run controller on Screen; HUD in Header; board, items and banner in Body; templates.
        // No full-screen background: the battlefield and the camp are world art drawn by the camera behind the canvas.
        internal static void BuildCanvas()
        {
            ScreenParts parts = ScreenParts.Create("GameplayCanvas", ContentSortOrder, true, null);
            RectTransform canvas = parts.Canvas;
            canvas.gameObject.AddComponent<CanvasUIScale>();
            Component screen = parts.Screen.gameObject.AddComponent(GameTypes.GameplayScreen);
            Component run = parts.Screen.gameObject.AddComponent(GameTypes.RunController);
            ScriptableObject config = BuildUtility.Load<ScriptableObject>(BuildPaths.GameplayConfig);
            Fields.Set(screen, ("_config", config), ("_pauseAction", "Gameplay/Pause"),
                ("_quitToMenuTitleKey", "gameplay.quit-menu.title"), ("_quitToMenuMessageKey", "gameplay.quit-menu.message"),
                ("_quitToMenuConfirmKey", "gameplay.quit-menu.confirm"), ("_quitToDesktopTitleKey", "gameplay.quit-desktop.title"),
                ("_quitToDesktopMessageKey", "gameplay.quit-desktop.message"), ("_quitToDesktopConfirmKey", "gameplay.quit-desktop.confirm"),
                ("_cancelKey", "common.cancel"));

            Component hud = BuildHud(parts.Header);
            Component enemy = BuildEnemyBar(parts.Header);
            Component board = BuildBoard(parts);
            Component items = BuildItemBar(parts.Body);
            Component banner = BuildBanner(parts.Body);

            Fields.Set(run, ("_screen", screen), ("_config", config), ("_board", board), ("_hud", hud), ("_enemyBar", enemy), ("_items", items),
                ("_banner", banner), ("_comboKey", "gameplay.showcase.combo"),
                ("_boughtKey", "gameplay.item.bought"), ("_noCoinsKey", "gameplay.item.no-coins"),
                ("_cannotUseKey", "gameplay.item.cannot-use"), ("_pickTargetKey", "gameplay.item.pick-target"));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.GameplayCanvas);
        }

        // Score, monsters beaten and coins on wood signs over the board (top left), Pause (top right corner).
        private static Component BuildHud(RectTransform header)
        {
            RectTransform root = UiFactory.Rect("Hud", header).Stretch();
            Component hud = root.gameObject.AddComponent(GameTypes.HudView);
            LocalizedLabel score = Pill(root, "ScorePanel", new Vector2(24f, -12f), 300f, "gameplay.hud.score", null);
            LocalizedLabel kills = Pill(root, "KillsPanel", new Vector2(338f, -12f), 250f, "gameplay.hud.kills", null);
            LocalizedLabel coins = Pill(root, "CoinsPanel", new Vector2(602f, -12f), 230f, "gameplay.hud.coins", "ui-common/coin-icon");

            RectTransform pauseRect = UiFactory.Rect("PauseButton", root).Place(UiFactory.TopRight, new Vector2(-20f, -20f), new Vector2(96f, 96f));
            Button pause = UiFactory.AddButton(pauseRect, BuildUtility.Sprite("screens/gameplay/button-pause"));

            Fields.Set(hud, ("_scoreLabel", score), ("_killsLabel", kills), ("_coinsLabel", coins),
                ("_pauseButton", pause));
            return hud;
        }

        // The boss's dragon bar over the battlefield (hidden for other monsters): the empty bar, the full bar cut at the health
        // left, the boss's name on it.
        private static Component BuildEnemyBar(RectTransform header)
        {
            RectTransform root = UiFactory.Rect("EnemyBar", header).Place(UiFactory.TopLeft, new Vector2(1216f, -16f), new Vector2(448f, 128f));
            UiFactory.AddImage(root, BuildUtility.Sprite("screens/gameplay/enemy-bar-empty")).raycastTarget = false;
            Image full = UiFactory.AddImage(UiFactory.Rect("Full", root).Stretch(), BuildUtility.Sprite("screens/gameplay/enemy-bar-full"));
            full.raycastTarget = false;
            full.type = Image.Type.Filled;
            full.fillMethod = Image.FillMethod.Horizontal;
            full.fillOrigin = (int)Image.OriginHorizontal.Left;
            TextMeshProUGUI name = UiFactory.AddText(UiFactory.Rect("NameLabel", root).Stretch(100f, 0f, 60f, 0f), string.Empty, 36f,
                Palette.White).Localize("monster.goblin.name");
            name.raycastTarget = false;
            UiFactory.Outlined(name);
            Component bar = root.gameObject.AddComponent(GameTypes.EnemyBarView);
            Fields.Set(bar, ("_full", full), ("_name", name.GetComponent<LocalizedLabel>()), ("_fillStart", 25f / 112f),
                ("_fillEnd", 96f / 112f));
            return bar;
        }

        // Frame with the gem area (clipped) and the selection frame; the targeting frame is a sibling above it, so its raycasts
        // never reach the board's pointer handlers. Cell and gem templates are inactive.
        private static Component BuildBoard(ScreenParts parts)
        {
            float frameSide = BoardSide + 2f * FramePadding;
            RectTransform frame = UiFactory.Rect("Board", parts.Body)
                .Place(UiFactory.BottomLeft, BoardCenter, new Vector2(frameSide, frameSide), UiFactory.Center);
            // A frame: only its border is drawn, the gems show through the middle.
            UiFactory.AddImage(frame, BuildUtility.Sprite("screens/gameplay/board-frame")).fillCenter = false;
            Component board = frame.gameObject.AddComponent(GameTypes.BoardView);

            RectTransform area = UiFactory.Rect("GemArea", frame).Place(UiFactory.Center, Vector2.zero, new Vector2(BoardSide, BoardSide));
            area.gameObject.AddComponent<RectMask2D>();

            RectTransform selection = UiFactory.Rect("Selection", area).Place(UiFactory.Center, Vector2.zero, new Vector2(80f, 80f));
            // Only the frame is drawn: the chosen gem shows through the middle.
            Image selected = UiFactory.AddImage(selection, BuildUtility.Sprite("ui-common/focus-frame"));
            selected.raycastTarget = false;
            selected.fillCenter = false;

            Image cell = UiFactory.AddImage(UiFactory.Rect("CellTemplate", parts.Templates).Place(UiFactory.Center, Vector2.zero,
                new Vector2(108f, 108f)), BuildUtility.Sprite("screens/gameplay/board-cell"));
            cell.raycastTarget = false;
            Image gem = UiFactory.AddImage(UiFactory.Rect("GemTemplate", parts.Templates).Place(UiFactory.Center, Vector2.zero,
                new Vector2(96f, 96f)), BuildUtility.Sprite("screens/gameplay/gem-attack"));
            gem.raycastTarget = false;
            gem.preserveAspect = true;

            RectTransform hint = UiFactory.Rect("TargetHint", parts.Body)
                .Place(UiFactory.BottomLeft, BoardCenter, new Vector2(BoardSide + 24f, BoardSide + 24f), UiFactory.Center);
            UiFactory.AddImage(hint, BuildUtility.Sprite("ui-common/focus-frame")).raycastTarget = false;
            hint.gameObject.SetActive(false);

            Fields.Set(board, ("_area", area), ("_cellTemplate", cell), ("_gemTemplate", gem), ("_selection", selection),
                ("_targetHint", hint.gameObject));
            return board;
        }

        // One slot per item key, left to right, alone in the bottom of the right half (the supports).
        private static Component BuildItemBar(RectTransform body)
        {
            const int slots = 4;
            RectTransform column = UiFactory.Rect("ItemBar", body)
                .Place(UiFactory.BottomLeft, ItemRowCenter, new Vector2(SlotSpacing * slots + 30f, SlotSize + 56f), UiFactory.Center);
            UiFactory.AddImage(column, BuildUtility.Sprite("ui-common/panel-wood")).raycastTarget = false;
            Component bar = column.gameObject.AddComponent(GameTypes.ItemBarView);
            var views = new Object[slots];
            for (int i = 0; i < slots; i++)
            {
                float x = i * SlotSpacing - SlotSpacing * (slots - 1) * 0.5f;
                RectTransform slot = UiFactory.Rect($"ItemSlot{i + 1}", column).Place(UiFactory.Center, new Vector2(x, 0f), new Vector2(SlotSize, SlotSize));
                Button button = UiFactory.AddButton(slot, BuildUtility.Sprite("screens/gameplay/item-slot"));
                Image icon = UiFactory.AddImage(UiFactory.Rect("Icon", slot).Place(UiFactory.Center, Vector2.zero, new Vector2(124f, 124f)),
                    BuildUtility.Sprite("screens/gameplay/item-potion"));
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                // Count and key sit inside the slot's frame (SlotFrame on each side), light with a dark outline over the icon.
                TextMeshProUGUI count = UiFactory.AddText(UiFactory.Rect("CountLabel", slot).Place(UiFactory.BottomRight,
                    new Vector2(-SlotFrame, SlotFrame - 4f), new Vector2(96f, 64f)), "0", 56f, Palette.TextLight,
                    TextAlignmentOptions.BottomRight).OnWood();
                count.raycastTarget = false;
                count.fontStyle = FontStyles.Bold;
                RectTransform plus = UiFactory.Rect("PlusIcon", slot).Place(UiFactory.BottomRight, new Vector2(-SlotFrame, SlotFrame),
                    new Vector2(56f, 56f));
                UiFactory.AddImage(plus, BuildUtility.Sprite("screens/gameplay/icon-plus")).raycastTarget = false;
                RectTransform active = UiFactory.Rect("ActiveFrame", slot).Stretch(-8f, -8f, -8f, -8f);
                UiFactory.AddImage(active, BuildUtility.Sprite("ui-common/focus-frame")).raycastTarget = false;
                active.gameObject.SetActive(false);
                TextMeshProUGUI key = UiFactory.AddText(UiFactory.Rect("KeyLabel", slot).Place(UiFactory.TopLeft,
                    new Vector2(SlotFrame + 2f, -SlotFrame + 2f), new Vector2(48f, 44f)), (i + 1).ToString(), 36f, Palette.Text,
                    TextAlignmentOptions.TopLeft);
                key.raycastTarget = false;

                Component view = slot.gameObject.AddComponent(GameTypes.ItemSlotView);
                Fields.Set(view, ("_button", button), ("_icon", icon), ("_count", count), ("_plus", plus.gameObject),
                    ("_active", active.gameObject), ("_key", key));
                views[i] = view;
            }

            Fields.Set(bar, ("_slots", views));
            return bar;
        }

        private static Component BuildBanner(RectTransform body)
        {
            RectTransform root = UiFactory.Rect("ShowcaseBanner", body)
                .Place(UiFactory.BottomLeft, BoardCenter + new Vector2(0f, 40f), new Vector2(860f, 360f), UiFactory.Center);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            Image icon = UiFactory.AddImage(UiFactory.Rect("GemIcon", root).Place(UiFactory.TopCenter, Vector2.zero, new Vector2(144f, 144f)),
                BuildUtility.Sprite("screens/gameplay/gem-attack"));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            LocalizedLabel caption = Label(root, "CaptionLabel", 96f, Palette.White, "gameplay.showcase.attack");
            caption.GetComponent<RectTransform>().Place(UiFactory.Center, new Vector2(0f, -30f), new Vector2(860f, 120f));
            Material thick = ThickOutline();
            UiFactory.Outlined(caption.GetComponent<TextMeshProUGUI>(), thick);
            LocalizedLabel combo = Label(root, "ComboLabel", 60f, new Color(1f, 0.85f, 0.3f), "gameplay.showcase.combo");
            combo.GetComponent<RectTransform>().Place(UiFactory.BottomCenter, new Vector2(0f, 0f), new Vector2(860f, 80f));
            UiFactory.Outlined(combo.GetComponent<TextMeshProUGUI>(), thick);
            Component banner = root.gameObject.AddComponent(GameTypes.ShowcaseBanner);
            Fields.Set(banner, ("_group", group), ("_icon", icon), ("_label", caption), ("_combo", combo));
            return banner;
        }

        /// <summary>
        /// The world part of the gameplay scene: the backdrops under the UI (board, camp) stay still; the stage (battlefield
        /// art, the hero facing left with the healer behind it and its magic bar over its head, two monster views, spots,
        /// numbers and bursts) is what screen shake moves.
        /// The battlefield art bleeds under the still backdrops and past the screen edge, so a shake shows no edge.
        /// </summary>
        internal static void BuildBattlefield()
        {
            var root = new GameObject("Battlefield");
            Component view = root.AddComponent(GameTypes.BattlefieldView);

            Transform environment = WorldFactory.Child("Environment", root.transform).transform;
            Backdrop(environment, "BoardBackdrop", "game/props/board-backdrop", new Vector3(-3.75f, 0f, 0f), -18);
            Backdrop(environment, "Camp", "game/props/camp", new Vector3(3.75f, (280f - 540f) / 128f, 0f), -18);

            Transform stage = WorldFactory.Child("Stage", root.transform).transform;
            Backdrop(stage, "Battlefield", "game/props/battlefield", new Vector3(3.75f, (820f - 540f) / 128f, 0f), -19);
            Transform characters = WorldFactory.Child("Characters", stage).transform;
            Component hero = Character(characters, "Hero", HeroSpot, true, true, 2);
            Component healer = Character(characters, "Healer", HealerSpot, true, false, 1, "game/props/magic-bar-fill");
            var monsters = new Object[]
            {
                Character(characters, "Monster1", MonsterEntry, false, false, 1),
                Character(characters, "Monster2", MonsterEntry, false, false, 1),
            };

            Transform spots = WorldFactory.Child("Spots", stage).transform;
            Transform heroSpot = WorldFactory.Child("HeroSpot", spots, HeroSpot).transform;
            Transform monsterSpot = WorldFactory.Child("MonsterSpot", spots, MonsterSpot).transform;
            Transform monsterEntry = WorldFactory.Child("MonsterEntry", spots, MonsterEntry).transform;

            Transform templates = WorldFactory.Child("Templates", stage).transform;
            templates.gameObject.SetActive(false);
            GameObject textObject = WorldFactory.Child("FloatingText", templates);
            var text = textObject.AddComponent<TextMeshPro>();
            text.font = GameFont.Asset;
            text.fontSharedMaterial = GameFont.Outline ?? text.fontSharedMaterial;
            text.fontSize = 8f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(5f, 1.6f);
            text.GetComponent<MeshRenderer>().sortingOrder = 30;
            var fx = WorldFactory.Child("Fx", templates).AddComponent<SpriteRenderer>();
            fx.sortingOrder = 20;

            Fields.Set(view, ("_hero", hero), ("_healer", healer), ("_monsters", monsters), ("_heroSpot", heroSpot),
                ("_monsterSpot", monsterSpot), ("_monsterEntry", monsterEntry), ("_textTemplate", text), ("_fxTemplate", fx),
                ("_hitFx", Frames("game/effects/fx-hit")), ("_healFx", Frames("game/effects/fx-heal")),
                ("_shieldFx", Frames("game/effects/fx-shield")), ("_magicFx", Frames("game/effects/fx-magic")),
                ("_orb", Frames("game/effects/fx-magic-orb")), ("_shakeRoot", stage));
            BuildUtility.SavePrefab(root, BuildPaths.Battlefield);
        }

        // A character root at its feet: Body (sprite and animator), and bars over the head (health, or the healer's magic).
        private static Component Character(Transform parent, string name, Vector3 position, bool faceLeft, bool armor, int order,
            string fill = null)
        {
            GameObject root = WorldFactory.Child(name, parent, position);
            GameObject body = WorldFactory.Child("Body", root.transform);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            var animator = body.AddComponent<Animator>();
            Component view = root.AddComponent(GameTypes.CharacterView);
            fill = fill ?? (faceLeft ? "game/props/hp-bar-fill" : "game/props/hp-bar-fill-enemy");
            Component health = Bar(root.transform, "HealthBar", fill, 1.05f);
            Component armorBar = armor ? Bar(root.transform, "ArmorBar", "game/props/armor-bar-fill", 1.05f, overlay: true) : null;
            Fields.Set(view, ("_body", renderer), ("_animator", animator), ("_health", health), ("_armor", armorBar),
                ("_faceLeft", faceLeft));
            return view;
        }

        // Track and fill drawn sliced, the fill growing from the left; an overlay bar has no track (drawn over the health bar).
        private static Component Bar(Transform parent, string name, string fillSprite, float y, bool overlay = false)
        {
            const float width = 1.6f;
            const float height = 0.24f;
            GameObject root = WorldFactory.Child(name, parent, new Vector3(0f, y, 0f));
            if (!overlay)
            {
                var track = WorldFactory.Child("Track", root.transform).AddComponent<SpriteRenderer>();
                track.sprite = BuildUtility.Sprite("game/props/hp-bar-track");
                track.drawMode = SpriteDrawMode.Sliced;
                track.size = new Vector2(width + 0.08f, height + 0.08f);
                track.sortingOrder = 5;
            }

            var fill = WorldFactory.Child("Fill", root.transform, new Vector3(-width / 2f, 0f, 0f)).AddComponent<SpriteRenderer>();
            fill.sprite = BuildUtility.Sprite(fillSprite);
            fill.drawMode = SpriteDrawMode.Sliced;
            fill.size = new Vector2(width, overlay ? height * 0.5f : height);
            fill.sortingOrder = overlay ? 7 : 6;
            if (overlay)
            {
                fill.transform.localPosition = new Vector3(-width / 2f, height * 0.25f, 0f);
            }

            Component bar = root.AddComponent(GameTypes.WorldBar);
            Fields.Set(bar, ("_fill", fill), ("_width", width));
            return bar;
        }

        // The four frames of an effect (<path>-0 .. <path>-3).
        private static Object[] Frames(string path)
        {
            var frames = new Object[4];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = BuildUtility.Sprite($"{path}-{i}");
            }

            return frames;
        }

        private static void Backdrop(Transform parent, string name, string sprite, Vector3 position, int order)
        {
            var renderer = WorldFactory.Child(name, parent, position).AddComponent<SpriteRenderer>();
            renderer.sprite = BuildUtility.Sprite(sprite);
            renderer.sortingOrder = order;
        }

        // A wood panel anchored top-left with an optional icon and a formatted label.
        private static LocalizedLabel Pill(RectTransform parent, string name, Vector2 position, float width, string key, string icon)
        {
            RectTransform rect = UiFactory.Rect(name, parent).Place(UiFactory.TopLeft, position, new Vector2(width, 84f));
            UiFactory.AddImage(rect, BuildUtility.Sprite("ui-common/panel-wood")).raycastTarget = false;
            if (icon != null)
            {
                UiFactory.AddImage(UiFactory.Rect("Icon", rect).Place(UiFactory.MiddleLeft, new Vector2(18f, 0f), new Vector2(56f, 56f)),
                    BuildUtility.Sprite(icon)).raycastTarget = false;
            }

            LocalizedLabel label = Label(rect, "Label", 40f, Palette.TextLight, key);
            label.GetComponent<RectTransform>().Stretch(icon != null ? 80f : 20f, 0f, 20f, 0f);
            return label;
        }

        private static LocalizedLabel Label(RectTransform parent, string name, float size, Color color, string key)
        {
            TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect(name, parent).Stretch(), string.Empty, size, color).Localize(key);
            text.raycastTarget = false;
            return text.GetComponent<LocalizedLabel>();
        }

        private static LocalizedLabel Text(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, bool muted,
            string key)
        {
            TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect(name, parent).Place(UiFactory.TopCenter, position, size), string.Empty,
                fontSize, Palette.TextLight).Localize(key).OnWood(muted);
            text.raycastTarget = false;
            text.Paragraph();
            return text.GetComponent<LocalizedLabel>();
        }

        // The outline material with a thicker outline, for the big texts shown over the board while gems pop.
        private static Material ThickOutline()
        {
            Material source = GameFont.Outline;
            if (source == null)
            {
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(BuildPaths.ShowcaseOutline);
            if (material == null)
            {
                BuildUtility.EnsureFolder(BuildPaths.DataUI);
                material = new Material(source);
                AssetDatabase.CreateAsset(material, BuildPaths.ShowcaseOutline);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
            }

            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.42f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
