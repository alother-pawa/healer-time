using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Foundation.InputPrompts;
using Game.Foundation.Localization;
using Game.Foundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The gameplay demo: <c>GameplayCanvas</c> (title, a 3×3 grid with one tile per action of the input actions' Gameplay
    /// map, the development input history overlay, the Pause prompt and the development key hints) and the pause menu popup
    /// (a variant of <c>Popup_Base</c>: Resume, Save, Settings, Quit to main menu, Quit to desktop, prompts). Tiles are made
    /// from the asset, so an action added to the Gameplay map gets a tile (name key <c>gameplay.action.&lt;kebab&gt;</c>,
    /// icon <c>gameplay/icon-&lt;kebab&gt;</c>) on the next build. The canvas has no focus scope and no selectable: nothing
    /// in it takes keyboard/gamepad focus while playing.
    /// </summary>
    internal static class GameplayPrefabBuilder
    {
        private const string GameplayMap = "Gameplay";
        private const string LookAction = "Gameplay/Look";
        private const int ContentSortOrder = 0;
        private const int GridColumns = 3;
        private const float TitleSize = 64f;
        private const float TileNameSize = 34f;
        private const float TileCountSize = 24f;
        private const float OverlayTitleSize = 30f;
        private const float OverlayTextSize = 20f;
        private const float StanceSize = 30f;
        private const float MenuButtonLabelSize = 40f;
        private const float MenuButtonSpacing = 96f;
        private const float PromptRowHeight = 56f;
        private const float StickAreaSize = 150f;
        private const float KnobSize = 44f;
        private const float ArrowSize = 34f;
        private const float CrosshairSize = 48f;
        private const float MouseReleaseGraceSeconds = 0.15f;
        private static readonly Vector2 TileSize = new Vector2(360f, 240f);
        private static readonly Vector2 TileSpacing = new Vector2(24f, 20f);
        private static readonly Vector2 GridOffset = new Vector2(250f, -20f);
        private static readonly Vector2 OverlaySize = new Vector2(600f, 640f);
        private static readonly Vector2 PausePanelSize = new Vector2(760f, 800f);
        private static readonly Vector2 MenuButtonSize = new Vector2(520f, 80f);
        private static readonly Vector2 IconSize = new Vector2(56f, 56f);

        /// <summary>Pause menu buttons, top to bottom: child name, sprite, label key.</summary>
        private static readonly (string name, string sprite, string key)[] PauseButtons =
        {
            ("ResumeButton", "ui-common/button-primary", "gameplay.pause.resume"),
            ("SaveButton", "ui-common/button-secondary", "gameplay.pause.save"),
            ("SettingsButton", "ui-common/button-secondary", "gameplay.pause.settings"),
            ("QuitToMenuButton", "ui-common/button-secondary", "gameplay.pause.quit-menu"),
            ("QuitToDesktopButton", "ui-common/button-secondary", "gameplay.pause.quit-desktop"),
        };

        private static readonly (string name, string[] actions, string labelKey)[] PausePrompts =
        {
            (PrefabBuilder.Prompts.Confirm, new[] { "UI/Submit" }, "prompt.confirm"),
            ("ResumePrompt", new[] { "Menu/Pause" }, "prompt.resume"),
        };

        private static readonly (string name, string[] actions, string labelKey)[] DevPrompts =
        {
            ("CheckpointPrompt", new[] { "Debug/Checkpoint" }, "prompt.checkpoint"),
            ("HistoryPrompt", new[] { "Debug/ToggleOverlay" }, "prompt.input-history"),
        };

        /// <summary>One tile per action of the Gameplay map, in asset order: action, kebab name, vector or button.</summary>
        internal static IReadOnlyList<(string action, string kebab, bool vector, bool mouseDelta)> GameplayActions()
        {
            InputActionMap map = BuildUtility.Load<InputActionAsset>(BuildPaths.InputActions).FindActionMap(GameplayMap, true);
            return map.actions.Select(action => (
                    $"{GameplayMap}/{action.name}",
                    Kebab(action.name),
                    action.expectedControlType == "Vector2",
                    action.bindings.Any(binding => binding.path != null && binding.path.EndsWith("/delta"))))
                .ToList();
        }

        /// <summary>"AltAttack" → "alt-attack".</summary>
        internal static string Kebab(string name)
        {
            return Regex.Replace(name, "(?<=[a-z0-9])([A-Z])", "-$1").ToLowerInvariant();
        }

        // Resume (first selected), Save, Settings, Quit to main menu, Quit to desktop; Confirm and Resume prompts.
        internal static void BuildPauseMenu()
        {
            PopupParts parts = PopupPrefabBuilder.Variant("Popup_PauseMenu", "gameplay.pause.title", PausePanelSize);
            GameObject root = parts.Root;
            RectTransform content = parts.Body;
            Component popup = root.AddComponent(GameTypes.PauseMenuPopup);
            root.AddComponent<InputContextRequest>();

            var buttons = new Button[PauseButtons.Length];
            for (int i = 0; i < PauseButtons.Length; i++)
            {
                RectTransform rect = UiFactory.Rect(PauseButtons[i].name, content)
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
            Fields.Set(popup, ("_viewId", "popup.pause-menu"), ("_resumeButton", buttons[0]), ("_saveButton", buttons[1]),
                ("_settingsButton", buttons[2]), ("_quitToMenuButton", buttons[3]), ("_quitToDesktopButton", buttons[4]),
                ("_resumeAction", "Menu/Pause"));
            BuildUtility.SavePrefab(root, BuildPaths.PopupPauseMenu);
        }

        // Background, title, tile grid, overlay (left), Pause prompt and development hints at the bottom. No focus scope,
        // no raycaster: the canvas only shows state.
        internal static void BuildCanvas()
        {
            ScreenParts parts = ScreenParts.Create("GameplayCanvas", ContentSortOrder, false, "backgrounds/gameplay-background");
            RectTransform canvas = parts.Canvas;
            canvas.gameObject.AddComponent<CanvasUIScale>();
            RectTransform screen = parts.Screen;
            Component gameplay = screen.gameObject.AddComponent(GameTypes.GameplayScreen);
            ScriptableObject config = BuildUtility.Load<ScriptableObject>(BuildPaths.GameplayConfig);

            UiFactory.AddText(UiFactory.Rect("TitleLabel", parts.Header).Place(UiFactory.TopCenter, new Vector2(GridOffset.x, -40f), new Vector2(1200f, 90f)),
                string.Empty, TitleSize, Palette.Text).Localize("gameplay.title").raycastTarget = false;

            RectTransform grid = UiFactory.Rect("TileGrid", parts.Body).Place(UiFactory.Center, GridOffset,
                new Vector2(GridColumns * TileSize.x + (GridColumns - 1) * TileSpacing.x, GridColumns * TileSize.y + (GridColumns - 1) * TileSpacing.y));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = TileSize;
            layout.spacing = TileSpacing;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = GridColumns;
            layout.childAlignment = TextAnchor.MiddleCenter;
            foreach (var (action, kebab, vector, _) in GameplayActions())
            {
                BuildTile(grid, action, kebab, vector, config);
            }

            BuildOverlay(parts.Body);

            // Sprint/crouch states with their hold/toggle mode, bottom-left under the overlay.
            RectTransform stanceRect = UiFactory.Rect("StanceLabel", parts.Footer).Place(UiFactory.BottomLeft, new Vector2(48f, 100f), new Vector2(560f, 110f));
            TextMeshProUGUI stanceText = UiFactory.AddText(stanceRect, string.Empty, StanceSize, Palette.Text, TextAlignmentOptions.BottomLeft);
            stanceText.raycastTarget = false;
            Component stance = stanceRect.gameObject.AddComponent(GameTypes.StanceView);
            Fields.Set(stance, ("_text", stanceText), ("_sprintKey", "gameplay.stance.sprint"), ("_crouchKey", "gameplay.stance.crouch"),
                ("_onKey", "gameplay.stance.on"), ("_offKey", "gameplay.stance.off"), ("_holdKey", "settings.press-mode.hold"),
                ("_toggleKey", "settings.press-mode.toggle"));

            RectTransform pauseRow = PrefabBuilder.PromptRow(parts.Footer, "Prompts", 24f, PromptRowHeight);
            PrefabBuilder.AddPrompt(pauseRow, "PausePrompt", new[] { "Gameplay/Pause" }, "prompt.pause");

            RectTransform devRow = UiFactory.Rect("DevHints", parts.Footer).BottomStretch(24f, PromptRowHeight);
            HorizontalLayoutGroup devLayout = UiFactory.AddRow(devRow.gameObject, 40f, TextAnchor.MiddleLeft);
            devLayout.padding = new RectOffset(48, 48, 0, 0);
            foreach ((string name, string[] actions, string labelKey) in DevPrompts)
            {
                PrefabBuilder.AddPrompt(devRow, name, actions, labelKey);
            }

            Fields.Set(gameplay, ("_config", config), ("_devHints", devRow.gameObject), ("_stance", stance),
                ("_sprintAction", "Gameplay/Sprint"), ("_crouchAction", "Gameplay/Crouch"), ("_pauseAction", "Gameplay/Pause"),
                ("_attackAction", "Gameplay/Attack"), ("_checkpointAction", "Debug/Checkpoint"),
                ("_checkpointLabelKey", "save-slots.location.demo"), ("_checkpointSavedKey", "gameplay.checkpoint.saved"),
                ("_savedKey", "gameplay.pause.saved"), ("_saveFailedKey", "gameplay.save-failed"), ("_noSlotKey", "gameplay.pause.no-slot"),
                ("_quitToMenuTitleKey", "gameplay.quit-menu.title"), ("_quitToMenuMessageKey", "gameplay.quit-menu.message"),
                ("_quitToMenuConfirmKey", "gameplay.quit-menu.confirm"), ("_quitToDesktopTitleKey", "gameplay.quit-desktop.title"),
                ("_quitToDesktopMessageKey", "gameplay.quit-desktop.message"), ("_quitToDesktopConfirmKey", "gameplay.quit-desktop.confirm"),
                ("_unsavedTitleKey", "gameplay.unsaved.title"), ("_unsavedMessageKey", "gameplay.unsaved.message"),
                ("_unsavedConfirmKey", "gameplay.unsaved.confirm"), ("_cancelKey", "common.cancel"));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.GameplayCanvas);
        }

        // Frame, highlight, icon, name, press count, glyphs; a value bar for buttons, a stick area with knob and arrows for
        // vectors (the Look tile has a crosshair instead of the knob).
        private static void BuildTile(RectTransform grid, string action, string kebab, bool vector, ScriptableObject config)
        {
            RectTransform tile = UiFactory.Rect($"Tile_{kebab}", grid);
            Component view = tile.gameObject.AddComponent(GameTypes.ActionTileView);
            RectTransform body = UiFactory.Rect("Body", tile).Stretch();
            UiFactory.AddImage(UiFactory.Rect("Frame", body).Stretch(), BuildUtility.Sprite("screens/gameplay-hud/action-tile")).raycastTarget = false;
            RectTransform lit = UiFactory.Rect("Lit", body).Stretch();
            UiFactory.AddImage(lit, BuildUtility.Sprite("screens/gameplay-hud/action-tile-lit")).raycastTarget = false;
            lit.gameObject.SetActive(false);

            Image icon = UiFactory.AddImage(UiFactory.Rect("Icon", body).Place(UiFactory.TopLeft, new Vector2(18f, -16f), IconSize),
                BuildUtility.Sprite($"screens/gameplay-hud/icon-{kebab}"));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UiFactory.AddText(UiFactory.Rect("Name", body).TopStretch(-18f, 52f, 86f, 18f), string.Empty, TileNameSize, Palette.Text,
                TextAlignmentOptions.MidlineLeft).Localize($"gameplay.action.{kebab}").raycastTarget = false;

            TextMeshProUGUI count = UiFactory.AddText(UiFactory.Rect("Count", body).Place(UiFactory.BottomRight, new Vector2(-18f, 16f),
                new Vector2(160f, 32f)), string.Empty, TileCountSize, Palette.TextMuted, TextAlignmentOptions.MidlineRight);
            count.raycastTarget = false;
            var countLabel = count.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(countLabel, ("_label", count));

            GameObject prompt = GlyphRow(body, "Prompt", new[] { action });
            GameObject partsPrompt = null;
            if (action == "Gameplay/Move")
            {
                partsPrompt = GlyphRow(body, "PartsPrompt",
                    new[] { "Gameplay/Move/up", "Gameplay/Move/left", "Gameplay/Move/down", "Gameplay/Move/right" });
            }

            Fields.Set(view, ("_action", action), ("_lit", lit.gameObject), ("_pulseTarget", body), ("_pressCount", countLabel),
                ("_pressCountKey", "gameplay.tile.presses"), ("_prompt", prompt), ("_partsPrompt", partsPrompt));

            if (!vector)
            {
                RectTransform bar = UiFactory.Rect("ValueBar", body).Place(UiFactory.Center, new Vector2(0f, -4f), new Vector2(280f, 24f));
                UiFactory.AddImage(bar, BuildUtility.Sprite("screens/settings/slider-track")).raycastTarget = false;
                Image fill = UiFactory.AddImage(UiFactory.Rect("Fill", bar).Stretch(), BuildUtility.Sprite("screens/gameplay-hud/value-fill"));
                fill.raycastTarget = false;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillAmount = 0f;
                Fields.Set(view, ("_valueFill", fill));
                return;
            }

            RectTransform area = UiFactory.Rect("StickArea", body).Place(UiFactory.MiddleRight, new Vector2(-24f, -6f),
                new Vector2(StickAreaSize, StickAreaSize));
            UiFactory.AddImage(area, BuildUtility.Sprite("screens/gameplay-hud/stick-area")).raycastTarget = false;
            if (action == LookAction)
            {
                RectTransform crosshair = UiFactory.Rect("Crosshair", area).Place(UiFactory.Center, Vector2.zero, new Vector2(CrosshairSize, CrosshairSize));
                UiFactory.AddImage(crosshair, BuildUtility.Sprite("screens/gameplay-hud/crosshair")).raycastTarget = false;
                Fields.Set(area.gameObject.AddComponent(GameTypes.LookCrosshairView), ("_action", action), ("_config", config),
                    ("_area", area), ("_crosshair", crosshair));
                return;
            }

            RectTransform knob = UiFactory.Rect("Knob", area).Place(UiFactory.Center, Vector2.zero, new Vector2(KnobSize, KnobSize));
            UiFactory.AddImage(knob, BuildUtility.Sprite("screens/gameplay-hud/stick-knob")).raycastTarget = false;
            var arrows = new Object[4];
            (string name, Vector2 position)[] sides =
            {
                ("up", new Vector2(0f, 1f)), ("down", new Vector2(0f, -1f)), ("left", new Vector2(-1f, 0f)), ("right", new Vector2(1f, 0f)),
            };
            for (int i = 0; i < sides.Length; i++)
            {
                RectTransform arrow = UiFactory.Rect($"Arrow_{sides[i].name}", area)
                    .Place(UiFactory.Center, sides[i].position * (StickAreaSize * 0.5f + ArrowSize * 0.5f), new Vector2(ArrowSize, ArrowSize));
                UiFactory.AddImage(arrow, BuildUtility.Sprite($"screens/gameplay-hud/direction-{sides[i].name}")).raycastTarget = false;
                arrow.gameObject.SetActive(false);
                arrows[i] = arrow.gameObject;
            }

            Fields.Set(view, ("_knob", knob), ("_knobRadius", (StickAreaSize - KnobSize) * 0.5f), ("_arrows", arrows));
        }

        // Prompt glyph slots without a label, bottom-left of the tile; a slot hides when its action has no binding.
        private static GameObject GlyphRow(RectTransform body, string name, string[] actions)
        {
            RectTransform row = UiFactory.Rect(name, body).Place(UiFactory.BottomLeft, new Vector2(18f, 14f), new Vector2(240f, 48f));
            UiFactory.AddRow(row.gameObject, 6f, TextAnchor.MiddleLeft);
            var layout = row.gameObject.AddComponent<LayoutElement>();
            var prompt = row.gameObject.AddComponent<InputPrompt>();
            var glyphs = new Object[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                glyphs[i] = PrefabBuilder.GlyphSlot(row, $"Glyph{i + 1}");
            }

            Fields.Set(prompt, ("_actions", actions), ("_glyphs", glyphs), ("_label", null), ("_layoutElement", layout));
            return row.gameObject;
        }

        // Top-left panel: title, status (device, scheme, mode), history lines; hidden until toggled.
        private static void BuildOverlay(RectTransform parent)
        {
            RectTransform holder = UiFactory.Rect("InputHistory", parent).Place(UiFactory.TopLeft, new Vector2(24f, -24f), OverlaySize);
            Component overlay = holder.gameObject.AddComponent(GameTypes.InputActivityOverlay);
            RectTransform panel = UiFactory.Rect("Panel", holder).Stretch();
            UiFactory.AddImage(panel, BuildUtility.Sprite("screens/gameplay-hud/overlay-panel")).raycastTarget = false;
            UiFactory.AddText(UiFactory.Rect("Title", panel).TopStretch(-16f, 40f, 20f, 20f), string.Empty, OverlayTitleSize, Palette.Text,
                TextAlignmentOptions.MidlineLeft).Localize("input-activity.overlay.title").raycastTarget = false;
            TextMeshProUGUI status = UiFactory.AddText(UiFactory.Rect("Status", panel).TopStretch(-60f, 64f, 20f, 20f), string.Empty,
                OverlayTextSize, Palette.TextMuted, TextAlignmentOptions.TopLeft);
            status.raycastTarget = false;
            var statusLabel = status.gameObject.AddComponent<LocalizedLabel>();
            Fields.Set(statusLabel, ("_label", status));
            TextMeshProUGUI history = UiFactory.AddText(UiFactory.Rect("History", panel).Stretch(20f, 16f, 20f, 132f), string.Empty,
                OverlayTextSize, Palette.Text, TextAlignmentOptions.TopLeft);
            history.raycastTarget = false;
            panel.gameObject.SetActive(false);

            Fields.Set(overlay, ("_panel", panel.gameObject), ("_status", statusLabel), ("_statusKey", "input-activity.overlay.status"),
                ("_history", history), ("_emptyKey", "input-activity.overlay.empty"), ("_toggleAction", "Debug/ToggleOverlay"), ("_lines", 16));
        }

        /// <summary>Release grace of a tracked action: mouse delta reads zero on frames without mouse events.</summary>
        internal static float ReleaseGrace(bool mouseDelta)
        {
            return mouseDelta ? MouseReleaseGraceSeconds : 0f;
        }
    }
}
