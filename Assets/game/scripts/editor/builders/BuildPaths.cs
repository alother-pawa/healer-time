using Paowa.Gamepad;
namespace Game.Editor.Builders
{
    /// <summary>
    /// Asset locations used by the builders. Folders are lowercase (kebab-case); files under a <c>Resources</c> folder keep
    /// the exact path their loader expects (the Paowa packages read <c>Resources/Paowa/&lt;Name&gt;</c>: InputActions,
    /// UISettings, PauseSettings, GamepadSettings, SettingsProfile, PlatformSettings, Localization; game modules read
    /// <c>Resources/game/config/&lt;Name&gt;</c>).
    /// </summary>
    internal static class BuildPaths
    {
        internal const string Root = "Assets/game";

        internal const string Sprites = Root + "/art/sprites";
        internal const string Atlases = Root + "/art/atlases";

        /// <summary>Image spec written by Tools/ShapeGen with the placeholders (sizes, borders, pivots, sheets).</summary>
        internal const string ArtSpec = Root + "/art/art-spec.json";
        internal const string Audio = Root + "/audio";

        /// <summary>Sound spec written by Tools/AudioGen with the placeholders (category, length, loop).</summary>
        internal const string AudioSpec = Audio + "/audio-spec.json";
        internal const string Fonts = Root + "/fonts";
        internal const string Scenes = Root + "/scenes";

        internal const string Data = Root + "/data";
        internal const string Animations = Data + "/animations";
        internal const string DataSceneKeys = Data + "/scene-keys";
        internal const string DataUI = Data + "/ui";
        internal const string DataInput = Data + "/input";
        internal const string DataSplash = Data + "/splash";
        internal const string DataSettings = Data + "/settings";
        internal const string DataSave = Data + "/save";
        internal const string DataCredits = Data + "/credits";
        internal const string DataFirstLaunch = Data + "/first-launch";
        internal const string DataAchievements = Data + "/achievements";
        internal const string DataPaowa = Data + "/Resources/Paowa";
        internal const string DataConfig = Data + "/Resources/game/config";

        /// <summary>The game's input actions, at the Resources path Core's Input System backend loads.</summary>
        internal const string InputActions = DataInput + "/Resources/Paowa/InputActions.inputactions";

        internal const string Prefabs = Root + "/prefabs";
        internal const string PrefabsShell = Prefabs + "/ui-shell";
        internal const string PrefabsPopups = Prefabs + "/popups";
        internal const string PrefabsSplash = Prefabs + "/splash";
        internal const string PrefabsMainMenu = Prefabs + "/main-menu";
        internal const string PrefabsSettings = Prefabs + "/settings";
        internal const string PrefabsSaveSlots = Prefabs + "/save-slots";
        internal const string PrefabsGameplay = Prefabs + "/gameplay";
        internal const string PrefabsFirstLaunch = Prefabs + "/first-launch";

        // Prefabs, in the order they are built.
        internal const string ToastBase = PrefabsShell + "/Toast_Base.prefab";
        internal const string InputPrompt = PrefabsShell + "/InputPrompt.prefab";
        internal const string PromptBar = PrefabsShell + "/PromptBar.prefab";
        internal const string EventSystem = PrefabsShell + "/EventSystem.prefab";
        internal const string UIRoot = PrefabsShell + "/UIRoot.prefab";
        internal const string ToastCanvas = PrefabsShell + "/ToastCanvas.prefab";
        internal const string LoadingCanvas = PrefabsShell + "/LoadingCanvas.prefab";
        internal const string PopupBase = PrefabsPopups + "/Popup_Base.prefab";
        internal const string PopupConfirm = PrefabsPopups + "/Popup_Confirm.prefab";
        internal const string SettingRowToggle = PrefabsSettings + "/SettingRow_Toggle.prefab";
        internal const string SettingRowSlider = PrefabsSettings + "/SettingRow_Slider.prefab";
        internal const string SettingRowChoice = PrefabsSettings + "/SettingRow_Choice.prefab";
        internal const string SettingRowKeyBinding = PrefabsSettings + "/SettingRow_KeyBinding.prefab";
        internal const string SettingRowButton = PrefabsSettings + "/SettingRow_Button.prefab";
        internal const string PopupSettings = PrefabsPopups + "/Popup_Settings.prefab";
        internal const string PopupDisplayConfirm = PrefabsPopups + "/Popup_DisplayConfirm.prefab";
        internal const string SaveSlotCard = PrefabsSaveSlots + "/SaveSlotCard.prefab";
        internal const string PopupSaveSlots = PrefabsPopups + "/Popup_SaveSlots.prefab";
        internal const string PopupPauseMenu = PrefabsPopups + "/Popup_PauseMenu.prefab";
        internal const string PopupCredits = PrefabsPopups + "/Popup_Credits.prefab";
        internal const string PopupAchievements = PrefabsPopups + "/Popup_Achievements.prefab";
        internal const string SplashCanvas = PrefabsSplash + "/SplashCanvas.prefab";
        internal const string MainMenuCanvas = PrefabsMainMenu + "/MainMenuCanvas.prefab";
        internal const string GameplayCanvas = PrefabsGameplay + "/GameplayCanvas.prefab";
        internal const string FirstLaunchCanvas = PrefabsFirstLaunch + "/FirstLaunchCanvas.prefab";

        // Assets referenced across builders.
        internal const string SceneKeyUI = DataSceneKeys + "/SceneKey_UI.asset";
        internal const string SceneKeySplash = DataSceneKeys + "/SceneKey_Splash.asset";
        internal const string SceneKeyMainMenu = DataSceneKeys + "/SceneKey_MainMenu.asset";
        internal const string SceneKeyGameplay = DataSceneKeys + "/SceneKey_Gameplay.asset";
        internal const string SceneKeyFirstLaunch = DataSceneKeys + "/SceneKey_FirstLaunch.asset";
        internal const string ToastAnimation = DataUI + "/ToastAnimation_Default.asset";
        internal const string LoadingTransition = DataUI + "/LoadingTransition_Default.asset";
        internal const string PopupAnimation = DataUI + "/PopupAnimation_Default.asset";
        internal const string GlyphSet = DataInput + "/GlyphSet_Placeholder.asset";
        internal const string SplashConfig = DataSplash + "/SplashConfig.asset";
        internal const string UISettings = DataPaowa + "/UISettings.asset";
        internal const string PauseSettings = DataPaowa + "/PauseSettings.asset";
        internal const string GamepadSettings = DataPaowa + "/GamepadSettings.asset";
        internal const string SettingsProfile = DataPaowa + "/SettingsProfile.asset";
        internal const string PlatformSettings = DataPaowa + "/PlatformSettings.asset";
        internal const string UIShellConfig = DataConfig + "/UIShellConfig.asset";
        internal const string SettingsConfig = DataConfig + "/SettingsConfig.asset";
        internal const string SaveSlotsConfig = DataConfig + "/SaveSlotsConfig.asset";
        internal const string CreditsConfig = DataConfig + "/CreditsConfig.asset";

        /// <summary>Achievements popup and rich presence per scene (written every run).</summary>
        internal const string AchievementsConfig = DataConfig + "/AchievementsConfig.asset";

        /// <summary>
        /// The achievement catalog assigned in <c>DesktopSettings</c>, with one definition per sample achievement beside it.
        /// Created once with the demo's achievements, then owned by the game.
        /// </summary>
        internal const string AchievementCatalog = DataAchievements + "/AchievementCatalog.asset";

        /// <summary>The credits content and scroll pacing. Created once with sample entries, then owned by the game.</summary>
        internal const string CreditsData = DataCredits + "/CreditsData.asset";

        /// <summary>First-launch destination (written every run) and quick choices (kept).</summary>
        internal const string FirstLaunchConfig = DataFirstLaunch + "/FirstLaunchConfig.asset";

        /// <summary>
        /// Build stamp read by Core's <c>BuildInfo</c> (<c>Resources/Paowa/BuildInfo</c>): written by the build pipeline
        /// right before a player build and removed after it (gitignored).
        /// </summary>
        internal const string BuildInfoFile = DataPaowa + "/BuildInfo.txt";

        /// <summary>
        /// Gameplay config: the builder writes its references (pause popup, menu scene) every run; tuning values (cursor,
        /// look, rumble) keep what the game set.
        /// </summary>
        internal const string GameplayConfig = DataConfig + "/GameplayConfig.asset";

        /// <summary>Tracked actions (written every run from the Gameplay map) and history size of the input activity.</summary>
        internal const string InputActivityConfig = DataConfig + "/InputActivityConfig.asset";

        /// <summary>
        /// The game's save settings (slot ids, data version, migrators, thumbnail size). Created once, never overwritten, so a
        /// data version bump and its migrator survive a rebuild.
        /// </summary>
        internal const string SaveSlotsData = DataSave + "/SaveSlotsData.asset";

        /// <summary>Example migration step (no-op from data version 0), created with <see cref="SaveSlotsData"/>.</summary>
        internal const string SaveMigratorNoOp = DataSave + "/migrations/SaveMigrator_From0_NoOp.asset";

        /// <summary>The base's settings (builder-owned: rewritten on every run).</summary>
        internal const string SettingsCatalogBase = DataSettings + "/SettingsCatalog_Base.asset";

        /// <summary>
        /// The game's catalog, assigned in <c>DesktopSettings</c>: includes the base catalog and holds the game's own tabs and
        /// settings. Created once, never overwritten, so settings added to it by hand survive a rebuild.
        /// </summary>
        internal const string SettingsCatalogGame = DataSettings + "/SettingsCatalog_Game.asset";

        /// <summary>Sound played when a volume changes in the settings (Tools/AudioGen).</summary>
        internal const string VolumePreview = Root + "/audio/ui/volume-preview.wav";

        /// <summary>
        /// TextMeshPro font asset with Japanese glyphs, made by hand (setup checklist §2); when it exists the project
        /// settings step adds it to the fallbacks of the default font.
        /// </summary>
        internal const string JapaneseFont = Fonts + "/NotoSansJP-SDF.asset";

        /// <summary>Source font of <see cref="JapaneseFont"/> (Noto Sans JP, OFL), copied in by hand.</summary>
        internal const string JapaneseFontSource = Fonts + "/NotoSansJP-Regular.ttf";

        // Scenes, in Build Settings order.
        internal const string SceneBoot = Scenes + "/Boot.unity";
        internal const string SceneUI = Scenes + "/UI.unity";
        internal const string SceneSplash = Scenes + "/Splash.unity";
        internal const string SceneMainMenu = Scenes + "/MainMenu.unity";

        /// <summary>Gameplay demo entered by New game / Continue (action tiles, pause menu).</summary>
        internal const string SceneGameplay = Scenes + "/Gameplay.unity";

        /// <summary>First-launch screen, entered from the splash once per install.</summary>
        internal const string SceneFirstLaunch = Scenes + "/FirstLaunch.unity";

        /// <summary>Editor-only scene showing every game image (not in the build).</summary>
        internal const string SceneArtPreview = Scenes + "/ArtPreview.unity";
    }
}
