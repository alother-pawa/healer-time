using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The gameplay scene's UI: <c>GameplayCanvas</c> (the screen host) and the pause menu popup (a variant of
    /// <c>Popup_Base</c>: Resume, Settings, Quit to main menu, Quit to desktop, prompts).
    /// </summary>
    internal static class GameplayPrefabBuilder
    {
        private const int ContentSortOrder = 0;
        private const float MenuButtonLabelSize = 40f;
        private const float MenuButtonSpacing = 104f;
        private const float PromptRowHeight = 56f;
        private static readonly Vector2 PausePanelSize = new Vector2(760f, 700f);
        private static readonly Vector2 MenuButtonSize = new Vector2(520f, 88f);

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

        // Resume (first selected), Settings, Quit to main menu, Quit to desktop; Confirm and Resume prompts.
        internal static void BuildPauseMenu()
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

        // The screen host only shows state; it has no focus scope.
        internal static void BuildCanvas()
        {
            ScreenParts parts = ScreenParts.Create("GameplayCanvas", ContentSortOrder, true, null);
            RectTransform canvas = parts.Canvas;
            canvas.gameObject.AddComponent<CanvasUIScale>();
            Component gameplay = parts.Screen.gameObject.AddComponent(GameTypes.GameplayScreen);
            Fields.Set(gameplay, ("_config", BuildUtility.Load<ScriptableObject>(BuildPaths.GameplayConfig)),
                ("_pauseAction", "Gameplay/Pause"),
                ("_quitToMenuTitleKey", "gameplay.quit-menu.title"), ("_quitToMenuMessageKey", "gameplay.quit-menu.message"),
                ("_quitToMenuConfirmKey", "gameplay.quit-menu.confirm"), ("_quitToDesktopTitleKey", "gameplay.quit-desktop.title"),
                ("_quitToDesktopMessageKey", "gameplay.quit-desktop.message"), ("_quitToDesktopConfirmKey", "gameplay.quit-desktop.confirm"),
                ("_cancelKey", "common.cancel"));
            BuildUtility.SavePrefab(canvas.gameObject, BuildPaths.GameplayCanvas);
        }
    }
}
