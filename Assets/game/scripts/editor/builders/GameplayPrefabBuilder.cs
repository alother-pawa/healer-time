using Game.Foundation.Localization;
using Game.Foundation.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The gameplay scene's prefabs (landscape 1920×1080; the battlefield band is the top third, the lower part holds the
    /// item column on the left, the board in the middle and the healer with the magic bar on the right):
    /// <c>GameplayCanvas</c> (HUD, board, item bar, magic bar, showcase banner; the screen host and the run controller),
    /// <c>Battlefield</c> (world: backgrounds, hero, two monster views, healer, spots, number and burst templates) and the
    /// popups (pause menu, revive, result; variants of <c>Popup_Base</c>).
    /// </summary>
    internal static class GameplayPrefabBuilder
    {
        private const int ContentSortOrder = 0;
        private const float MenuButtonLabelSize = 40f;
        private const float MenuButtonSpacing = 104f;
        private const float PromptRowHeight = 56f;
        private const float BoardY = 352f;
        private const float BoardSide = 616f;
        private const float FramePadding = 22f;
        private const float SlotSize = 128f;
        private const float SlotSpacing = 146f;
        private const float ItemColumnX = 330f;
        private const float HealerColumnX = 1590f;
        private const string OutlineMaterial = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat";
        private static readonly Vector2 PausePanelSize = new Vector2(760f, 700f);
        private static readonly Vector2 MenuButtonSize = new Vector2(520f, 88f);
        private static readonly Vector2 DialogPanelSize = new Vector2(900f, 620f);

        // World layout in units (camera at the origin, 128 px per unit, 1080 px = 8.4375 units tall).
        private const float GroundY = 1.85f;
        private static readonly Vector3 HeroSpot = new Vector3(3.2f, GroundY, 0f);
        private static readonly Vector3 MonsterSpot = new Vector3(1.5f, GroundY, 0f);
        private static readonly Vector3 MonsterEntry = new Vector3(-8.8f, GroundY, 0f);
        private static readonly Vector3 HealerSpot = new Vector3((HealerColumnX - 960f) / 128f, -1.5f, 0f);

        /// <summary>Pause menu buttons, top to bottom: child name, sprite, label key.</summary>
        private static readonly (string name, string sprite, string key)[] PauseButtons =
        {
            ("ResumeButton", "ui-common/button-primary", "gameplay.pause.resume"),
            ("SettingsButton", "ui-common/button-secondary", "gameplay.pause.settings"),
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
            BuildRevive();
            BuildResult();
        }

        // Resume (first selected), Settings, Quit to main menu, Quit to desktop; Confirm and Resume prompts.
        private static void BuildPauseMenu()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_PauseMenu", "gameplay.pause.title", PausePanelSize);
            GameObject root = parts.Root;
            Component popup = root.AddComponent(GameTypes.PauseMenuPopup);
            root.AddComponent<InputContextRequest>();

            var buttons = new Button[PauseButtons.Length];
            for (int i = 0; i < PauseButtons.Length; i++)
            {
                RectTransform rect = UiFactory.Rect(PauseButtons[i].name, parts.Body)
                    .Place(UiFactory.TopCenter, new Vector2(0f, -10f - i * MenuButtonSpacing), MenuButtonSize);
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
                ("_quitToMenuButton", buttons[2]), ("_quitToDesktopButton", buttons[3]), ("_resumeAction", "Menu/Pause"));
            BuildUtility.SavePrefab(root, BuildPaths.PopupPauseMenu);
        }

        // Price message, coins owned, Revive (first selected) and Give up.
        private static void BuildRevive()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_Revive", "gameplay.revive.title", DialogPanelSize);
            GameObject root = parts.Root;
            Component popup = root.AddComponent(GameTypes.RevivePopup);

            LocalizedLabel message = Text(parts.Body, "MessageLabel", new Vector2(0f, -40f), new Vector2(780f, 140f), 40f, Palette.Text,
                "gameplay.revive.message");
            RectTransform balance = UiFactory.Rect("Balance", parts.Body).Place(UiFactory.TopCenter, new Vector2(0f, -190f), new Vector2(520f, 70f));
            UiFactory.AddImage(UiFactory.Rect("CoinIcon", balance).Place(UiFactory.MiddleLeft, new Vector2(70f, 0f), new Vector2(56f, 56f)),
                BuildUtility.Sprite("ui-common/coin-icon")).raycastTarget = false;
            LocalizedLabel balanceLabel = Text(balance, "BalanceLabel", Vector2.zero, new Vector2(400f, 70f), 36f, Palette.TextMuted,
                "gameplay.revive.balance");

            Button revive = PopupPrefabBuilder.DialogButton(parts.Footer, "ReviveButton", -180f, "ui-common/button-primary",
                "gameplay.revive.confirm", out _);
            Button giveUp = PopupPrefabBuilder.DialogButton(parts.Footer, "GiveUpButton", 180f, "ui-common/button-secondary",
                "gameplay.revive.give-up", out _);
            UiFactory.LinkHorizontal(revive, giveUp);
            PopupPrefabBuilder.SetFirstSelected(root, revive);
            Fields.Set(popup, ("_viewId", "popup.revive"), ("_message", message), ("_balance", balanceLabel),
                ("_reviveButton", revive), ("_giveUpButton", giveUp));
            BuildUtility.SavePrefab(root, BuildPaths.PopupRevive);
        }

        // Score (big), best, monsters, coins, new-best badge; Play again (first selected) and Main menu.
        private static void BuildResult()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_Result", "gameplay.result.title", new Vector2(900f, 720f));
            GameObject root = parts.Root;
            Component popup = root.AddComponent(GameTypes.ResultPopup);

            LocalizedLabel score = Text(parts.Body, "ScoreLabel", new Vector2(0f, -20f), new Vector2(780f, 100f), 72f, Palette.Text,
                "gameplay.result.score");
            score.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            LocalizedLabel best = Text(parts.Body, "BestLabel", new Vector2(0f, -120f), new Vector2(780f, 56f), 34f, Palette.TextMuted,
                "gameplay.result.best");
            LocalizedLabel kills = Text(parts.Body, "KillsLabel", new Vector2(0f, -200f), new Vector2(780f, 56f), 38f, Palette.Text,
                "gameplay.result.kills");
            LocalizedLabel coins = Text(parts.Body, "CoinsLabel", new Vector2(0f, -260f), new Vector2(780f, 56f), 38f, Palette.Text,
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

        // Screen host and run controller on Screen; HUD in Header; board, items, magic bar and banner in Body; templates.
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
            Component board = BuildBoard(parts);
            Component items = BuildItemBar(parts.Body);
            Component magic = BuildMagicBar(parts.Body);
            Component banner = BuildBanner(parts.Body);

            Fields.Set(run, ("_screen", screen), ("_config", config), ("_board", board), ("_hud", hud), ("_items", items),
                ("_magicBar", magic), ("_banner", banner), ("_comboKey", "gameplay.showcase.combo"),
                ("_boughtKey", "gameplay.item.bought"), ("_noCoinsKey", "gameplay.item.no-coins"),
                ("_cannotUseKey", "gameplay.item.cannot-use"), ("_pickTargetKey", "gameplay.item.pick-target"));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.GameplayCanvas);
        }

        // Score, monsters beaten and coins on wood panels (top left), the monster on the field (top centre), Pause (top right).
        private static Component BuildHud(RectTransform header)
        {
            RectTransform root = UiFactory.Rect("Hud", header).Stretch();
            Component hud = root.gameObject.AddComponent(GameTypes.HudView);
            LocalizedLabel score = Pill(root, "ScorePanel", new Vector2(24f, -20f), 280f, "gameplay.hud.score", null);
            LocalizedLabel kills = Pill(root, "KillsPanel", new Vector2(316f, -20f), 220f, "gameplay.hud.kills", null);
            LocalizedLabel coins = Pill(root, "CoinsPanel", new Vector2(548f, -20f), 200f, "gameplay.hud.coins", "ui-common/coin-icon");

            RectTransform monsterRect = UiFactory.Rect("MonsterPanel", root).Place(UiFactory.TopCenter, new Vector2(0f, -20f), new Vector2(360f, 72f));
            UiFactory.AddImage(monsterRect, BuildUtility.Sprite("ui-common/toast-background")).raycastTarget = false;
            LocalizedLabel monster = Label(monsterRect, "MonsterLabel", 34f, Palette.Text, "gameplay.hud.monster");

            RectTransform pauseRect = UiFactory.Rect("PauseButton", root).Place(UiFactory.TopRight, new Vector2(-24f, -16f), new Vector2(88f, 88f));
            Button pause = UiFactory.AddButton(pauseRect, BuildUtility.Sprite("ui-common/button-secondary"));
            UiFactory.AddImage(UiFactory.Rect("Icon", pauseRect).Place(UiFactory.Center, Vector2.zero, new Vector2(40f, 45f)),
                BuildUtility.Sprite("screens/gameplay/icon-pause")).raycastTarget = false;

            Fields.Set(hud, ("_scoreLabel", score), ("_killsLabel", kills), ("_coinsLabel", coins), ("_monsterLabel", monster),
                ("_pauseButton", pause));
            return hud;
        }

        // Frame with the gem area (clipped) and the selection frame; the mask and the targeting frame are siblings above it,
        // so their raycasts never reach the board's pointer handlers. Cell and gem templates are inactive.
        private static Component BuildBoard(ScreenParts parts)
        {
            float frameSide = BoardSide + 2f * FramePadding;
            RectTransform frame = UiFactory.Rect("Board", parts.Body)
                .Place(UiFactory.BottomCenter, new Vector2(0f, BoardY), new Vector2(frameSide, frameSide), UiFactory.Center);
            // A frame: only its border is drawn, the gems show through the middle.
            UiFactory.AddImage(frame, BuildUtility.Sprite("screens/gameplay/board-frame")).fillCenter = false;
            Component board = frame.gameObject.AddComponent(GameTypes.BoardView);

            RectTransform area = UiFactory.Rect("GemArea", frame).Place(UiFactory.Center, Vector2.zero, new Vector2(BoardSide, BoardSide));
            area.gameObject.AddComponent<RectMask2D>();

            RectTransform selection = UiFactory.Rect("Selection", area).Place(UiFactory.Center, Vector2.zero, new Vector2(80f, 80f));
            UiFactory.AddImage(selection, BuildUtility.Sprite("screens/gameplay/board-cell-selected")).raycastTarget = false;

            Image cell = UiFactory.AddImage(UiFactory.Rect("CellTemplate", parts.Templates).Place(UiFactory.Center, Vector2.zero,
                new Vector2(76f, 76f)), BuildUtility.Sprite("screens/gameplay/board-cell"));
            cell.raycastTarget = false;
            Image gem = UiFactory.AddImage(UiFactory.Rect("GemTemplate", parts.Templates).Place(UiFactory.Center, Vector2.zero,
                new Vector2(64f, 64f)), BuildUtility.Sprite("screens/gameplay/gem-attack"));
            gem.raycastTarget = false;
            gem.preserveAspect = true;

            RectTransform maskRect = UiFactory.Rect("BoardMask", parts.Body)
                .Place(UiFactory.BottomCenter, new Vector2(0f, BoardY), new Vector2(BoardSide, BoardSide), UiFactory.Center);
            UiFactory.AddImage(maskRect, BuildUtility.Sprite("screens/gameplay/board-mask"));
            var mask = maskRect.gameObject.AddComponent<CanvasGroup>();
            mask.alpha = 0f;
            mask.blocksRaycasts = false;

            RectTransform hint = UiFactory.Rect("TargetHint", parts.Body)
                .Place(UiFactory.BottomCenter, new Vector2(0f, BoardY), new Vector2(frameSide + 16f, frameSide + 16f), UiFactory.Center);
            UiFactory.AddImage(hint, BuildUtility.Sprite("ui-common/focus-frame")).raycastTarget = false;
            hint.gameObject.SetActive(false);

            Fields.Set(board, ("_area", area), ("_cellTemplate", cell), ("_gemTemplate", gem), ("_selection", selection), ("_mask", mask),
                ("_targetHint", hint.gameObject));
            return board;
        }

        // One slot per item key, top to bottom, left of the board.
        private static Component BuildItemBar(RectTransform body)
        {
            const int slots = 4;
            RectTransform column = UiFactory.Rect("ItemBar", body)
                .Place(UiFactory.BottomLeft, new Vector2(ItemColumnX, BoardY), new Vector2(SlotSize + 40f, SlotSpacing * slots), UiFactory.Center);
            UiFactory.AddImage(column, BuildUtility.Sprite("ui-common/panel-wood")).raycastTarget = false;
            Component bar = column.gameObject.AddComponent(GameTypes.ItemBarView);
            var views = new Object[slots];
            for (int i = 0; i < slots; i++)
            {
                float y = SlotSpacing * (slots - 1) * 0.5f - i * SlotSpacing;
                RectTransform slot = UiFactory.Rect($"ItemSlot{i + 1}", column).Place(UiFactory.Center, new Vector2(0f, y), new Vector2(SlotSize, SlotSize));
                Button button = UiFactory.AddButton(slot, BuildUtility.Sprite("screens/gameplay/item-slot"));
                Image icon = UiFactory.AddImage(UiFactory.Rect("Icon", slot).Place(UiFactory.Center, Vector2.zero, new Vector2(80f, 80f)),
                    BuildUtility.Sprite("screens/gameplay/item-potion"));
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                TextMeshProUGUI count = UiFactory.AddText(UiFactory.Rect("CountLabel", slot).Place(UiFactory.BottomRight, new Vector2(-10f, 6f),
                    new Vector2(70f, 44f)), "0", 34f, Palette.Text, TextAlignmentOptions.BottomRight);
                count.raycastTarget = false;
                count.fontStyle = FontStyles.Bold;
                RectTransform plus = UiFactory.Rect("PlusIcon", slot).Place(UiFactory.BottomRight, new Vector2(-8f, 8f), new Vector2(44f, 44f));
                UiFactory.AddImage(plus, BuildUtility.Sprite("screens/gameplay/icon-plus")).raycastTarget = false;
                RectTransform active = UiFactory.Rect("ActiveFrame", slot).Stretch(-8f, -8f, -8f, -8f);
                UiFactory.AddImage(active, BuildUtility.Sprite("ui-common/focus-frame")).raycastTarget = false;
                active.gameObject.SetActive(false);
                TextMeshProUGUI key = UiFactory.AddText(UiFactory.Rect("KeyLabel", slot).Place(UiFactory.TopLeft, new Vector2(10f, -6f),
                    new Vector2(44f, 36f)), (i + 1).ToString(), 26f, Palette.TextMuted, TextAlignmentOptions.TopLeft);
                key.raycastTarget = false;

                Component view = slot.gameObject.AddComponent(GameTypes.ItemSlotView);
                Fields.Set(view, ("_button", button), ("_icon", icon), ("_count", count), ("_plus", plus.gameObject),
                    ("_active", active.gameObject), ("_key", key));
                views[i] = view;
            }

            Fields.Set(bar, ("_slots", views));
            return bar;
        }

        // Under the healer (right of the board): caption and bar.
        private static Component BuildMagicBar(RectTransform body)
        {
            RectTransform root = UiFactory.Rect("MagicBar", body)
                .Place(UiFactory.BottomLeft, new Vector2(HealerColumnX, 160f), new Vector2(360f, 96f), UiFactory.Center);
            Label(root, "MagicLabel", 30f, Palette.TextLight, "gameplay.hud.magic").GetComponent<RectTransform>().TopStretch(0f, 40f);
            RectTransform track = UiFactory.Rect("Track", root).BottomStretch(0f, 40f);
            UiFactory.AddImage(track, BuildUtility.Sprite("screens/gameplay/bar-track")).raycastTarget = false;
            RectTransform fill = UiFactory.Rect("Fill", track).Stretch(4f, 4f, 4f, 4f);
            UiFactory.AddImage(fill, BuildUtility.Sprite("screens/gameplay/bar-fill-magic")).raycastTarget = false;
            fill.pivot = new Vector2(0f, 0.5f);
            Component bar = root.gameObject.AddComponent(GameTypes.UiBar);
            Fields.Set(bar, ("_fill", fill));
            return bar;
        }

        // Over the middle of the board: gem icon, caption, combo caption.
        private static Component BuildBanner(RectTransform body)
        {
            RectTransform root = UiFactory.Rect("ShowcaseBanner", body)
                .Place(UiFactory.BottomCenter, new Vector2(0f, BoardY + 40f), new Vector2(760f, 300f), UiFactory.Center);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            Image icon = UiFactory.AddImage(UiFactory.Rect("GemIcon", root).Place(UiFactory.TopCenter, Vector2.zero, new Vector2(120f, 120f)),
                BuildUtility.Sprite("screens/gameplay/gem-attack"));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            LocalizedLabel caption = Label(root, "CaptionLabel", 76f, Palette.White, "gameplay.showcase.attack");
            caption.GetComponent<RectTransform>().Place(UiFactory.Center, new Vector2(0f, -30f), new Vector2(760f, 100f));
            Outlined(caption.GetComponent<TextMeshProUGUI>());
            LocalizedLabel combo = Label(root, "ComboLabel", 48f, new Color(1f, 0.85f, 0.3f), "gameplay.showcase.combo");
            combo.GetComponent<RectTransform>().Place(UiFactory.BottomCenter, new Vector2(0f, 0f), new Vector2(760f, 64f));
            Outlined(combo.GetComponent<TextMeshProUGUI>());
            Component banner = root.gameObject.AddComponent(GameTypes.ShowcaseBanner);
            Fields.Set(banner, ("_group", group), ("_icon", icon), ("_label", caption), ("_combo", combo));
            return banner;
        }

        /// <summary>
        /// The world part of the gameplay scene: battlefield and camp art, the hero (right, facing left), two monster views,
        /// the healer (right of the board), spots and the templates of numbers and bursts.
        /// </summary>
        internal static void BuildBattlefield()
        {
            var root = new GameObject("Battlefield");
            Component view = root.AddComponent(GameTypes.BattlefieldView);

            Transform environment = WorldFactory.Child("Environment", root.transform).transform;
            Backdrop(environment, "Battlefield", "game/props/battlefield", new Vector3(0f, (856f - 540f) / 128f, 0f), -19);
            Backdrop(environment, "Camp", "game/props/camp", new Vector3(0f, (352f - 540f) / 128f, 0f), -20);

            Transform characters = WorldFactory.Child("Characters", root.transform).transform;
            Component hero = Character(characters, "Hero", HeroSpot, true, true, 2);
            var monsters = new Object[]
            {
                Character(characters, "Monster1", MonsterEntry, false, false, 1),
                Character(characters, "Monster2", MonsterEntry, false, false, 1),
            };
            Component healer = Character(characters, "Healer", HealerSpot, false, false, 2, bars: false);

            Transform spots = WorldFactory.Child("Spots", root.transform).transform;
            Transform heroSpot = WorldFactory.Child("HeroSpot", spots, HeroSpot).transform;
            Transform monsterSpot = WorldFactory.Child("MonsterSpot", spots, MonsterSpot).transform;
            Transform monsterEntry = WorldFactory.Child("MonsterEntry", spots, MonsterEntry).transform;

            Transform templates = WorldFactory.Child("Templates", root.transform).transform;
            templates.gameObject.SetActive(false);
            GameObject textObject = WorldFactory.Child("FloatingText", templates);
            var text = textObject.AddComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterial) ?? text.fontSharedMaterial;
            text.fontSize = 6f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
            text.GetComponent<MeshRenderer>().sortingOrder = 30;
            var fx = WorldFactory.Child("Fx", templates).AddComponent<SpriteRenderer>();
            fx.sortingOrder = 20;

            Fields.Set(view, ("_hero", hero), ("_healer", healer), ("_monsters", monsters), ("_heroSpot", heroSpot),
                ("_monsterSpot", monsterSpot), ("_monsterEntry", monsterEntry), ("_textTemplate", text), ("_fxTemplate", fx),
                ("_hitFx", BuildUtility.Sprite("game/effects/fx-hit")), ("_healFx", BuildUtility.Sprite("game/effects/fx-heal")),
                ("_shieldFx", BuildUtility.Sprite("game/effects/fx-shield")), ("_magicFx", BuildUtility.Sprite("game/effects/fx-magic")),
                ("_orb", BuildUtility.Sprite("game/effects/fx-magic-orb")));
            BuildUtility.SavePrefab(root, BuildPaths.Battlefield);
        }

        // A character root at its feet: Body (sprite and animator), and bars over the head.
        private static Component Character(Transform parent, string name, Vector3 position, bool faceLeft, bool armor, int order,
            bool bars = true)
        {
            GameObject root = WorldFactory.Child(name, parent, position);
            GameObject body = WorldFactory.Child("Body", root.transform);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            var animator = body.AddComponent<Animator>();
            Component view = root.AddComponent(GameTypes.CharacterView);
            Component health = bars ? Bar(root.transform, "HealthBar", faceLeft ? "game/props/hp-bar-fill" : "game/props/hp-bar-fill-enemy", 1.05f) : null;
            Component armorBar = armor ? Bar(root.transform, "ArmorBar", "game/props/armor-bar-fill", 1.05f, overlay: true) : null;
            Fields.Set(view, ("_body", renderer), ("_animator", animator), ("_health", health), ("_armor", armorBar),
                ("_faceLeft", faceLeft));
            return view;
        }

        // Track and fill drawn sliced, the fill growing from the left; an overlay bar has no track (drawn over the health bar).
        private static Component Bar(Transform parent, string name, string fillSprite, float y, bool overlay = false)
        {
            const float width = 1.2f;
            const float height = 0.2f;
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

        private static void Backdrop(Transform parent, string name, string sprite, Vector3 position, int order)
        {
            var renderer = WorldFactory.Child(name, parent, position).AddComponent<SpriteRenderer>();
            renderer.sprite = BuildUtility.Sprite(sprite);
            renderer.sortingOrder = order;
        }

        // A wood panel anchored top-left with an optional icon and a formatted label.
        private static LocalizedLabel Pill(RectTransform parent, string name, Vector2 position, float width, string key, string icon)
        {
            RectTransform rect = UiFactory.Rect(name, parent).Place(UiFactory.TopLeft, position, new Vector2(width, 72f));
            UiFactory.AddImage(rect, BuildUtility.Sprite("ui-common/panel-wood")).raycastTarget = false;
            if (icon != null)
            {
                UiFactory.AddImage(UiFactory.Rect("Icon", rect).Place(UiFactory.MiddleLeft, new Vector2(14f, 0f), new Vector2(48f, 48f)),
                    BuildUtility.Sprite(icon)).raycastTarget = false;
            }

            LocalizedLabel label = Label(rect, "Label", 32f, Palette.TextLight, key);
            label.GetComponent<RectTransform>().Stretch(icon != null ? 64f : 16f, 0f, 16f, 0f);
            return label;
        }

        private static LocalizedLabel Label(RectTransform parent, string name, float size, Color color, string key)
        {
            TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect(name, parent).Stretch(), string.Empty, size, color).Localize(key);
            text.raycastTarget = false;
            return text.GetComponent<LocalizedLabel>();
        }

        private static LocalizedLabel Text(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color,
            string key)
        {
            TextMeshProUGUI text = UiFactory.AddText(UiFactory.Rect(name, parent).Place(UiFactory.TopCenter, position, size), string.Empty,
                fontSize, color).Localize(key);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text.GetComponent<LocalizedLabel>();
        }

        private static void Outlined(TextMeshProUGUI text)
        {
            var outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterial);
            if (outline != null)
            {
                text.fontSharedMaterial = outline;
            }

            text.fontStyle = FontStyles.Bold;
        }
    }
}
