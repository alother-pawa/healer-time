using System;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Internal game types resolved by name, so the builders need no access to module internals. Each module's namespace
    /// equals its assembly name (folders are kebab-case and do not follow it). Public Contracts, Foundation and Paowa types
    /// are referenced directly instead.
    /// </summary>
    internal static class GameTypes
    {
        private const string UIShell = "Game.Systems.UIShell";
        private const string SceneFlow = "Game.Systems.SceneFlow";
        private const string Settings = "Game.Systems.Settings";
        private const string MainMenu = "Game.Systems.MainMenu";
        private const string Splash = "Game.Systems.Splash";
        private const string Gameplay = "Game.Systems.Gameplay";
        private const string Roster = "Game.Systems.Roster";

        internal static Type UIShellRoot => Resolve(UIShell, UIShell, "UIShellRoot");
        internal static Type ToastHost => Resolve(UIShell, UIShell, "ToastHost");
        internal static Type LoadingTransition => Resolve(UIShell, UIShell, "LoadingTransition");
        internal static Type LoadingTransitionConfig => Resolve(UIShell, UIShell, "LoadingTransitionConfig");
        internal static Type ConfirmDialogPopup => Resolve(UIShell, UIShell, "ConfirmDialogPopup");
        internal static Type UIShellConfig => Resolve(UIShell, UIShell, "UIShellConfig");
        internal static Type BootSceneEntry => Resolve(SceneFlow, SceneFlow, "BootSceneEntry");
        internal static Type SettingsPopup => Resolve(Settings, Settings, "SettingsPopup");
        internal static Type SettingsConfig => Resolve(Settings, Settings, "SettingsConfig");
        internal static Type SettingsTabButton => Resolve(Settings, Settings, "SettingsTabButton");
        internal static Type SettingsPage => Resolve(Settings, Settings, "SettingsPage");
        internal static Type ToggleSettingRow => Resolve(Settings, Settings, "ToggleSettingRow");
        internal static Type SliderSettingRow => Resolve(Settings, Settings, "SliderSettingRow");
        internal static Type ChoiceSettingRow => Resolve(Settings, Settings, "ChoiceSettingRow");
        internal static Type KeyBindingSettingRow => Resolve(Settings, Settings, "KeyBindingSettingRow");
        internal static Type ButtonSettingRow => Resolve(Settings, Settings, "ButtonSettingRow");
        internal static Type DisplayConfirmPopup => Resolve(Settings, Settings, "DisplayConfirmPopup");
        internal static Type RebindCancelButton => Resolve(Settings, Settings, "RebindCancelButton");
        internal static Type MainMenuScreen => Resolve(MainMenu, MainMenu, "MainMenuScreen");
        internal static Type CharacterPage => Resolve(MainMenu, MainMenu, "CharacterPage");
        internal static Type CharacterCard => Resolve(MainMenu, MainMenu, "CharacterCard");
        internal static Type SplashScreen => Resolve(Splash, Splash, "SplashScreen");
        internal static Type SplashConfig => Resolve(Splash, Splash, "SplashConfig");
        internal static Type GameplayScreen => Resolve(Gameplay, Gameplay, "GameplayScreen");
        internal static Type GameplayConfig => Resolve(Gameplay, Gameplay, "GameplayConfig");
        internal static Type PauseMenuPopup => Resolve(Gameplay, Gameplay, "PauseMenuPopup");
        internal static Type BalanceConfig => Resolve(Gameplay, Gameplay, "BalanceConfig");
        internal static Type ItemDefinition => Resolve(Gameplay, Gameplay, "ItemDefinition");
        internal static Type GemDefinition => Resolve(Gameplay, Gameplay, "GemDefinition");
        internal static Type MonsterDefinition => Resolve(Gameplay, Gameplay, "MonsterDefinition");
        internal static Type RunController => Resolve(Gameplay, Gameplay, "RunController");
        internal static Type BoardView => Resolve(Gameplay, Gameplay, "BoardView");
        internal static Type HudView => Resolve(Gameplay, Gameplay, "HudView");
        internal static Type ItemBarView => Resolve(Gameplay, Gameplay, "ItemBarView");
        internal static Type ItemSlotView => Resolve(Gameplay, Gameplay, "ItemSlotView");
        internal static Type UiBar => Resolve(Gameplay, Gameplay, "UiBar");
        internal static Type ShowcaseBanner => Resolve(Gameplay, Gameplay, "ShowcaseBanner");
        internal static Type BattlefieldView => Resolve(Gameplay, Gameplay, "BattlefieldView");
        internal static Type CharacterView => Resolve(Gameplay, Gameplay, "CharacterView");
        internal static Type WorldBar => Resolve(Gameplay, Gameplay, "WorldBar");
        internal static Type RevivePopup => Resolve(Gameplay, Gameplay, "RevivePopup");
        internal static Type ResultPopup => Resolve(Gameplay, Gameplay, "ResultPopup");
        internal static Type RosterConfig => Resolve(Roster, Roster, "RosterConfig");

        private static Type Resolve(string assembly, string ns, string name)
        {
            string qualified = $"{ns}.{name}, {assembly}";
            return Type.GetType(qualified)
                ?? throw new InvalidOperationException(
                    $"Type '{qualified}' was not found. Is the module installed and compiling?");
        }
    }
}
