using Game.Contracts.Navigation;
using Game.Foundation.UI;
using Paowa.Gamepad;
using Paowa.Pause;
using Paowa.Platform;
using Paowa.Settings;
using Paowa.UI;
using Paowa.UI.Navigation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Creates or updates every ScriptableObject at its path under <c>Assets/game/data</c>. Values set here are written
    /// on every run; assets kept at their defaults are only created, so later tuning survives a rebuild. Texts shown to
    /// the player are localization keys of <c>localization/Resources/Paowa/Localization/pc-base.csv</c>. Configs that
    /// reference prefabs are written by <see cref="BuildPrefabConfigs"/> once the prefabs exist. The settings catalog is
    /// built by <see cref="SettingsCatalogBuilder"/>. The save slot data (<c>data/save/SaveSlotsData</c> and its example
    /// migrator), the credits sample (<c>data/credits/CreditsData</c>) and the achievement catalog with its definitions
    /// (<c>data/achievements</c>) are created once and then belong to the game.
    /// </summary>
    internal static class AssetBuilder
    {
        private const string CancelAction = "UI/Cancel";
        // The pause locks gameplay and the development keys (input history, checkpoint), never the UI or Menu maps.
        private static readonly string[] PauseLockedContexts = { "Gameplay", "Debug" };
        private const int SaveDataVersion = 1;
        private const int ThumbnailWidth = 320;
        private const int ThumbnailHeight = 180;
        private const int ThumbnailQuality = 75;
        private static readonly string[] SaveSlotIds = { "slot-1", "slot-2", "slot-3" };

        /// <summary>Rich presence (Steam status) per scene; scenes without an entry clear it.</summary>
        private static readonly (string scene, string statusKey)[] Presence =
        {
            (BuildPaths.SceneKeyMainMenu, "presence.main-menu"),
            (BuildPaths.SceneKeyGameplay, "presence.gameplay"),
            (BuildPaths.SceneKeyFirstLaunch, "presence.first-launch"),
        };

        /// <summary>Sample credits: section title key, then entries (name as written, role key, note as written).</summary>
        private static readonly (string titleKey, (string name, string roleKey, string note)[] entries)[] SampleCredits =
        {
            ("credits.section.studio", new[]
            {
                ("Paowa", "credits.role.studio", ""),
                ("Your Name", "credits.role.design-programming", ""),
            }),
            ("credits.section.third-party", new[]
            {
                ("DOTween (HOTween v2)", "credits.role.tweening",
                    "Copyright (c) Daniele Giardini - Demigiant. Used under the DOTween license: http://dotween.demigiant.com/license.php"),
                ("TextMesh Pro", "credits.role.text-rendering",
                    "Copyright (c) Unity Technologies. Unity Companion License: https://unity.com/legal/licenses/unity-companion-license"),
                ("Liberation Sans", "credits.role.font",
                    "Default TextMesh Pro font. Copyright (c) Red Hat, Inc. SIL Open Font License 1.1: https://openfontlicense.org"),
                ("Noto Sans JP", "credits.role.font",
                    "Copyright 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'. SIL Open Font License 1.1: https://openfontlicense.org"),
                ("Paowa Core", "credits.role.framework", ""),
            }),
            ("credits.section.thanks", new[]
            {
                ("You", "credits.role.player", ""),
            }),
        };

        /// <summary>Sample achievements: API name, file name, icon sprite, hidden until unlocked.</summary>
        private static readonly (string id, string file, string icon, bool hidden)[] SampleAchievements =
        {
            ("ACH_FIRST_ACTION", "first-action", "screens/achievements/achievement-first-action", false),
            ("ACH_ALL_ACTIONS", "all-actions", "screens/achievements/achievement-all-actions", false),
            ("ACH_FIRST_SAVE", "first-save", "screens/achievements/achievement-first-save", true),
        };

        /// <summary>Builds all assets that reference no prefab, in dependency order.</summary>
        internal static void Build()
        {
            CreateSceneKey(BuildPaths.SceneKeyUI, "UI", false);
            // The splash is entered from Boot: a plain cover, so no loading screen precedes the studio logo.
            CreateSceneKey(BuildPaths.SceneKeySplash, "Splash", true);
            CreateSceneKey(BuildPaths.SceneKeyMainMenu, "MainMenu", false);
            CreateSceneKey(BuildPaths.SceneKeyGameplay, "Gameplay", false);
            CreateSceneKey(BuildPaths.SceneKeyFirstLaunch, "FirstLaunch", false);
            BuildUtility.Asset<ToastAnimationConfig>(BuildPaths.ToastAnimation);
            BuildUtility.Asset(GameTypes.LoadingTransitionConfig, BuildPaths.LoadingTransition);
            BuildUtility.Asset<PopupAnimationConfig>(BuildPaths.PopupAnimation);
            Fields.Set(BuildUtility.Asset(GameTypes.SplashConfig, BuildPaths.SplashConfig),
                ("_nextScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyMainMenu)),
                ("_firstLaunchScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyFirstLaunch)));
            Fields.Set(BuildUtility.Asset(GameTypes.FirstLaunchConfig, BuildPaths.FirstLaunchConfig),
                ("_nextScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyMainMenu)));

            // Core's own canvas (used only if a view shows before the UI scene attaches its root) matches the game's.
            Fields.Set(BuildUtility.Asset<UISettings>(BuildPaths.UISettings),
                ("_referenceResolution", UiFactory.ReferenceResolution), ("_matchWidthOrHeight", UiFactory.MatchWidthAndHeight),
                ("_sortingOrder", ShellPrefabBuilder.UiRootSortOrder), ("_cancelAction", CancelAction));
            Fields.Set(BuildUtility.Asset<PauseSettings>(BuildPaths.PauseSettings), ("_lockedContexts", PauseLockedContexts));

            GlyphSet glyphs = GlyphSetBuilder.Build(BuildPaths.GlyphSet);
            SettingsCatalog settings = SettingsCatalogBuilder.Build();
            AchievementCatalog achievements = CreateAchievementCatalog();
            Fields.Set(BuildUtility.Asset<GamepadSettings>(BuildPaths.GamepadSettings),
                ("_keyboardMouseScheme", SettingsCatalogBuilder.KeyboardMouse), ("_gamepadScheme", SettingsCatalogBuilder.Gamepad),
                ("_glyphSet", glyphs));
            Fields.Set(BuildUtility.Asset<SettingsProfile>(BuildPaths.SettingsProfile), ("_catalog", settings));
            Fields.Set(BuildUtility.Asset<PlatformSettings>(BuildPaths.PlatformSettings), ("_achievementCatalog", achievements));
            CreateSaveData();
            CreateCreditsData();
            BuildGameplayConfigs();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Configs loaded by modules from Resources that point at popup prefabs; run after the prefabs.</summary>
        internal static void BuildPrefabConfigs()
        {
            Fields.Set(BuildUtility.Asset(GameTypes.UIShellConfig, BuildPaths.UIShellConfig),
                ("_confirmDialog", BuildUtility.PrefabComponent(BuildPaths.PopupConfirm, GameTypes.ConfirmDialogPopup)));
            Fields.Set(BuildUtility.Asset(GameTypes.SettingsConfig, BuildPaths.SettingsConfig),
                ("_popup", BuildUtility.PrefabComponent(BuildPaths.PopupSettings, GameTypes.SettingsPopup)),
                ("_displayConfirmPopup", BuildUtility.PrefabComponent(BuildPaths.PopupDisplayConfirm, GameTypes.DisplayConfirmPopup)));
            Fields.Set(BuildUtility.Asset(GameTypes.SaveSlotsConfig, BuildPaths.SaveSlotsConfig),
                ("_panel", BuildUtility.PrefabComponent(BuildPaths.PopupSaveSlots, GameTypes.SaveSlotPanel)),
                ("_gameplayScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyGameplay)),
                ("_data", BuildUtility.Load<ScriptableObject>(BuildPaths.SaveSlotsData)));
            Fields.Set(BuildUtility.Asset(GameTypes.GameplayConfig, BuildPaths.GameplayConfig),
                ("_pausePopup", BuildUtility.PrefabComponent(BuildPaths.PopupPauseMenu, GameTypes.PauseMenuPopup)));
            Fields.Set(BuildUtility.Asset(GameTypes.CreditsConfig, BuildPaths.CreditsConfig),
                ("_popup", BuildUtility.PrefabComponent(BuildPaths.PopupCredits, GameTypes.CreditsPopup)));
            ScriptableObject achievements = BuildUtility.Asset(GameTypes.AchievementsConfig, BuildPaths.AchievementsConfig);
            Fields.Set(achievements, ("_popup", BuildUtility.PrefabComponent(BuildPaths.PopupAchievements, GameTypes.AchievementsPopup)));
            Fields.SetList(achievements, "_presence", Presence.Length, (element, i) =>
            {
                Fields.Relative(element, "_scene", BuildUtility.Load<SceneKey>(Presence[i].scene));
                Fields.Relative(element, "_statusKey", Presence[i].statusKey);
            });
            AssetDatabase.SaveAssets();
        }

        // Gameplay config (the menu scene; the pause popup is set once the prefab exists, tuning keeps the game's values) and
        // the input activity's tracked actions: every action of the Gameplay map, vectors by their value type, a short release
        // grace for actions bound to mouse delta.
        private static void BuildGameplayConfigs()
        {
            Fields.Set(BuildUtility.Asset(GameTypes.GameplayConfig, BuildPaths.GameplayConfig),
                ("_menuScene", BuildUtility.Load<SceneKey>(BuildPaths.SceneKeyMainMenu)));
            var actions = GameplayPrefabBuilder.GameplayActions();
            Fields.SetList(BuildUtility.Asset(GameTypes.InputActivityConfig, BuildPaths.InputActivityConfig), "_actions", actions.Count,
                (element, i) =>
                {
                    Fields.Relative(element, "_action", actions[i].action);
                    Fields.Relative(element, "_kind", new EnumName(actions[i].vector ? "Vector" : "Button"));
                    Fields.Relative(element, "_releaseGraceSeconds", GameplayPrefabBuilder.ReleaseGrace(actions[i].mouseDelta));
                });
        }

        // Three slots, data version 1 with a no-op step from 0 (the pattern of a real migration), 320×180 JPG thumbnails.
        // Only when missing: afterwards the game owns these assets (data version bumps, its own migrators, slot ids).
        private static void CreateSaveData()
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(BuildPaths.SaveSlotsData) != null)
            {
                return;
            }

            ScriptableObject noOp = BuildUtility.Asset(GameTypes.NoOpSaveMigrator, BuildPaths.SaveMigratorNoOp);
            Fields.Set(noOp, ("_fromVersion", 0));
            Fields.Set(BuildUtility.Asset(GameTypes.SaveSlotsData, BuildPaths.SaveSlotsData), ("_slotIds", SaveSlotIds),
                ("_dataVersion", SaveDataVersion), ("_migrators", new Object[] { noOp }),
                ("_newGameLabelKey", "save-slots.location.start"), ("_thumbnailWidth", ThumbnailWidth),
                ("_thumbnailHeight", ThumbnailHeight), ("_thumbnailQuality", ThumbnailQuality));
        }

        // Sample sections (studio placeholder, third-party notices, thanks). Only when missing: the game edits it by hand.
        private static void CreateCreditsData()
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(BuildPaths.CreditsData) != null)
            {
                return;
            }

            Fields.SetList(BuildUtility.Asset(GameTypes.CreditsData, BuildPaths.CreditsData), "_sections", SampleCredits.Length,
                (element, i) =>
                {
                    Fields.Relative(element, "_titleKey", SampleCredits[i].titleKey);
                    SerializedProperty entries = element.FindPropertyRelative("_entries");
                    entries.arraySize = SampleCredits[i].entries.Length;
                    for (int j = 0; j < entries.arraySize; j++)
                    {
                        SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                        Fields.Relative(entry, "_name", SampleCredits[i].entries[j].name);
                        Fields.Relative(entry, "_roleKey", SampleCredits[i].entries[j].roleKey);
                        Fields.Relative(entry, "_note", SampleCredits[i].entries[j].note);
                    }
                });
        }

        // One definition per sample achievement (texts achievements.<file>.name/.description) and the catalog listing them.
        // Only when the catalog is missing: afterwards the game owns them (mirroring the Steamworks partner site).
        private static AchievementCatalog CreateAchievementCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AchievementCatalog>(BuildPaths.AchievementCatalog);
            if (existing != null)
            {
                return existing;
            }

            var definitions = new Object[SampleAchievements.Length];
            for (int i = 0; i < SampleAchievements.Length; i++)
            {
                (string id, string file, string icon, bool hidden) = SampleAchievements[i];
                definitions[i] = BuildUtility.Asset<AchievementDefinition>(BuildPaths.DataAchievements + "/Achievement_" + file + ".asset");
                Fields.Set(definitions[i], ("_id", id), ("_nameKey", "achievements." + file + ".name"),
                    ("_descriptionKey", "achievements." + file + ".description"), ("_icon", BuildUtility.Sprite(icon)),
                    ("_hidden", hidden));
            }

            var catalog = BuildUtility.Asset<AchievementCatalog>(BuildPaths.AchievementCatalog);
            Fields.Set(catalog, ("_achievements", definitions));
            return catalog;
        }

        private static void CreateSceneKey(string path, string sceneName, bool plainTransition)
        {
            Fields.Set(BuildUtility.Asset<SceneKey>(path), ("_sceneName", sceneName), ("_plainTransition", plainTransition));
        }
    }
}
