using Game.Contracts.Navigation;
using Game.Foundation.UI;
using Paowa.Gamepad;
using Paowa.Pause;
using Paowa.Settings;
using Paowa.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Creates or updates every ScriptableObject at its path under <c>Assets/game/data</c>. Values set here are written
    /// on every run; assets kept at their defaults are only created, so later tuning survives a rebuild. Texts shown to
    /// the player are localization keys of <c>localization/Resources/Paowa/Localization/healer-time.csv</c>. Configs that
    /// reference prefabs are written by <see cref="BuildPrefabConfigs"/> once the prefabs exist. The settings catalog is
    /// built by <see cref="SettingsCatalogBuilder"/>.
    /// </summary>
    internal static class AssetBuilder
    {
        private const string CancelAction = "UI/Cancel";
        // The pause locks gameplay, never the UI or Menu maps.
        private static readonly string[] PauseLockedContexts = { "Gameplay" };

        /// <summary>Builds all assets that reference no prefab, in dependency order.</summary>
        internal static void Build()
        {
            CreateSceneKey(BuildPaths.SceneKeyUI, "UI", false);
            // The splash is entered from Boot: a plain cover, so no loading screen precedes the studio logo.
            CreateSceneKey(BuildPaths.SceneKeySplash, "Splash", true);
            CreateSceneKey(BuildPaths.SceneKeyMainMenu, "MainMenu", false);
            CreateSceneKey(BuildPaths.SceneKeyGameplay, "Gameplay", false);
            BuildUtility.Asset<ToastAnimationConfig>(BuildPaths.ToastAnimation);
            BuildUtility.Asset(GameTypes.LoadingTransitionConfig, BuildPaths.LoadingTransition);
            BuildUtility.Asset<PopupAnimationConfig>(BuildPaths.PopupAnimation);
            Fields.Set(BuildUtility.Asset(GameTypes.SplashConfig, BuildPaths.SplashConfig),
                ("_nextScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyMainMenu)));

            // Core's own canvas (used only if a view shows before the UI scene attaches its root) matches the game's.
            Fields.Set(BuildUtility.Asset<UISettings>(BuildPaths.UISettings),
                ("_referenceResolution", UiFactory.ReferenceResolution), ("_matchWidthOrHeight", UiFactory.MatchWidthAndHeight),
                ("_sortingOrder", ShellPrefabBuilder.UiRootSortOrder), ("_cancelAction", CancelAction));
            Fields.Set(BuildUtility.Asset<PauseSettings>(BuildPaths.PauseSettings), ("_lockedContexts", PauseLockedContexts));

            GlyphSet glyphs = GlyphSetBuilder.Build(BuildPaths.GlyphSet);
            SettingsCatalog settings = SettingsCatalogBuilder.Build();
            Fields.Set(BuildUtility.Asset<GamepadSettings>(BuildPaths.GamepadSettings),
                ("_keyboardMouseScheme", SettingsCatalogBuilder.KeyboardMouse), ("_gamepadScheme", SettingsCatalogBuilder.Gamepad),
                ("_glyphSet", glyphs));
            Fields.Set(BuildUtility.Asset<SettingsProfile>(BuildPaths.SettingsProfile), ("_catalog", settings));
            GameDataBuilder.Build();
            Fields.Set(BuildUtility.Asset(GameTypes.GameplayConfig, BuildPaths.GameplayConfig),
                ("_menuScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyMainMenu)), ("_balance", GameDataBuilder.Balance));
            Fields.Set(BuildUtility.Asset(GameTypes.GameplayConfig, BuildPaths.GameplayConfig), GameplaySounds);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Configs loaded by modules from Resources that point at popup prefabs; run after the prefabs.</summary>
        internal static void BuildPrefabConfigs()
        {
            Fields.Set(BuildUtility.Asset(GameTypes.UIShellConfig, BuildPaths.UIShellConfig),
                ("_confirmDialog", BuildUtility.PrefabComponent(BuildPaths.PopupConfirm, GameTypes.ConfirmDialogPopup)),
                ("_howToPlay", BuildUtility.PrefabComponent(BuildPaths.PopupHowToPlay, GameTypes.HowToPlayPopup)));
            Fields.Set(BuildUtility.Asset(GameTypes.SettingsConfig, BuildPaths.SettingsConfig),
                ("_popup", BuildUtility.PrefabComponent(BuildPaths.PopupSettings, GameTypes.SettingsPopup)),
                ("_displayConfirmPopup", BuildUtility.PrefabComponent(BuildPaths.PopupDisplayConfirm, GameTypes.DisplayConfirmPopup)));
            Fields.Set(BuildUtility.Asset(GameTypes.GameplayConfig, BuildPaths.GameplayConfig),
                ("_pausePopup", BuildUtility.PrefabComponent(BuildPaths.PopupPauseMenu, GameTypes.PauseMenuPopup)),
                ("_resultPopup", BuildUtility.PrefabComponent(BuildPaths.PopupResult, GameTypes.ResultPopup)));
            AssetDatabase.SaveAssets();
        }

        // The gameplay sounds (Tools/AudioGen), by field of GameplaySounds.
        private static (string, object)[] GameplaySounds => new (string, object)[]
        {
            ("_sounds._music", Clip("music/battle-theme")), ("_sounds._gemSwap", Clip("sfx/gem-swap")),
            ("_sounds._gemInvalid", Clip("sfx/gem-invalid")), ("_sounds._gemMatch", Clip("sfx/gem-match")),
            ("_sounds._shuffle", Clip("sfx/board-shuffle")), ("_sounds._bomb", Clip("sfx/bomb")),
            ("_sounds._heroAttack", Clip("sfx/hero-attack")), ("_sounds._heroStrike", Clip("sfx/hero-strike")),
            ("_sounds._monsterAttack", Clip("sfx/monster-attack")), ("_sounds._hit", Clip("sfx/hit")), ("_sounds._heal", Clip("sfx/heal")),
            ("_sounds._shield", Clip("sfx/shield")), ("_sounds._magicCharge", Clip("sfx/magic-charge")),
            ("_sounds._healerSkill", Clip("sfx/healer-skill")), ("_sounds._monsterDeath", Clip("sfx/monster-death")),
            ("_sounds._bossAppear", Clip("sfx/boss-appear")), ("_sounds._heroDeath", Clip("sfx/hero-death")),
            ("_sounds._coin", Clip("ui/coin")), ("_sounds._click", Clip("ui/button-click")),
        };

        /// <summary>A sound of <c>Assets/game/audio</c> by its path without extension (Tools/AudioGen).</summary>
        internal static AudioClip Clip(string path)
        {
            return BuildUtility.Load<AudioClip>($"{BuildPaths.Audio}/{path}.wav");
        }

        private static void CreateSceneKey(string path, string sceneName, bool plainTransition)
        {
            Fields.Set(BuildUtility.Asset<SceneKey>(path), ("_sceneName", sceneName), ("_plainTransition", plainTransition));
        }
    }
}
