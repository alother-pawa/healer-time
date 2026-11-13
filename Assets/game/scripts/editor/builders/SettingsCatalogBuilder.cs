using System;
using System.Collections.Generic;
using Game.Contracts.Settings;
using Paowa.Desktop;
using Paowa.Gamepad;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// The settings model as data (settings package <c>SettingsCatalog</c> and <c>SettingDefinition</c> assets under
    /// <c>data/settings/&lt;tab&gt;/</c>): five tabs (Gameplay, Audio &amp; Language, Controls, Key Bindings, Graphics) and their
    /// rows. Ids in <c>DesktopSettingIds</c> are applied by the plugin, ids in <c>GameSettingIds</c> by the game's settings
    /// module; the Key Bindings rows point at the game's input actions (primary and secondary keyboard slots, one gamepad
    /// slot, a reset per device), the Controls tab keeps sensitivity, invert and vibration. The base
    /// catalog is rewritten on every run; the game catalog that includes it is only created, so a game's own definitions
    /// and tabs added to it survive a rebuild (a new tab also needs a rebuild for its page in the settings popup).
    /// Labels and descriptions are localization keys.
    /// </summary>
    internal static class SettingsCatalogBuilder
    {
        internal const string KeyboardMouse = "KeyboardMouse";
        internal const string Gamepad = "Gamepad";

        private const string Gameplay = SettingIds.TabGameplay;
        private const string AudioLanguage = SettingIds.TabAudioLanguage;
        private const string Controls = SettingIds.TabControls;
        private const string KeyBindings = "key-bindings";
        private const string Graphics = SettingIds.TabGraphics;
        private const string Percent = "{0:0}%";
        private const string Multiplier = "{0:0.0}×";
        private const string BindingDescription = "settings.bind.desc";

        private static readonly (string id, string labelKey)[] Tabs =
        {
            (Gameplay, "settings.tab.gameplay"), (AudioLanguage, "settings.tab.audio-language"),
            (Controls, "settings.tab.controls"), (KeyBindings, "settings.tab.key-bindings"), (Graphics, "settings.tab.graphics"),
        };

        // Gameplay actions with a keyboard row: asset name, action, composite part, label key, keyboard slots.
        private static readonly (string name, string action, string part, string labelKey, int slots)[] KeyboardBindings =
        {
            ("MoveUp", "Gameplay/Move", "up", "settings.bind.move-up", 2),
            ("MoveDown", "Gameplay/Move", "down", "settings.bind.move-down", 2),
            ("MoveLeft", "Gameplay/Move", "left", "settings.bind.move-left", 2),
            ("MoveRight", "Gameplay/Move", "right", "settings.bind.move-right", 2),
            ("Jump", "Gameplay/Jump", "", "settings.bind.jump", 2),
            ("Interact", "Gameplay/Interact", "", "settings.bind.interact", 2),
            ("Attack", "Gameplay/Attack", "", "settings.bind.attack", 2),
            ("AltAttack", "Gameplay/AltAttack", "", "settings.bind.alt-attack", 2),
            ("Sprint", "Gameplay/Sprint", "", "settings.bind.sprint", 2),
            ("Crouch", "Gameplay/Crouch", "", "settings.bind.crouch", 2),
            ("Pause", "Gameplay/Pause", "", "settings.bind.pause", 2),
        };

        private static readonly (string name, string action, string labelKey)[] GamepadBindings =
        {
            ("Move", "Gameplay/Move", "settings.bind.move"),
            ("Jump", "Gameplay/Jump", "settings.bind.jump"),
            ("Interact", "Gameplay/Interact", "settings.bind.interact"),
            ("Attack", "Gameplay/Attack", "settings.bind.attack"),
            ("AltAttack", "Gameplay/AltAttack", "settings.bind.alt-attack"),
            ("Sprint", "Gameplay/Sprint", "settings.bind.sprint"),
            ("Crouch", "Gameplay/Crouch", "settings.bind.crouch"),
            ("Pause", "Gameplay/Pause", "settings.bind.pause"),
        };

        private static readonly (string value, string labelKey)[] FrameRateCaps =
        {
            ("30", "settings.fps.30"), ("60", "settings.fps.60"), ("120", "settings.fps.120"), ("144", "settings.fps.144"),
            ("0", "settings.fps.unlimited"),
        };

        /// <summary>Writes every definition and the base catalog, creates the game catalog if missing; returns the game catalog.</summary>
        internal static SettingsCatalog Build()
        {
            DeleteMovedDefinitions();
            var definitions = new List<SettingDefinition>();
            AddGameplay(definitions);
            AddAudioLanguage(definitions);
            AddControls(definitions);
            AddKeyBindings(definitions);
            AddGraphics(definitions);

            SettingsCatalog baseCatalog = BuildUtility.Asset<SettingsCatalog>(BuildPaths.SettingsCatalogBase);
            Fields.SetList(baseCatalog, "_tabs", Tabs.Length, (element, i) =>
            {
                Fields.Relative(element, "_id", Tabs[i].id);
                Fields.Relative(element, "_labelKey", Tabs[i].labelKey);
            });
            Fields.Set(baseCatalog, ("_includes", Array.Empty<UnityEngine.Object>()), ("_definitions", definitions.ToArray()));

            SettingsCatalog gameCatalog = BuildUtility.Asset<SettingsCatalog>(BuildPaths.SettingsCatalogGame);
            var includes = new List<SettingsCatalog>(gameCatalog.Includes);
            if (!includes.Contains(baseCatalog))
            {
                includes.Insert(0, baseCatalog);
                Fields.Set(gameCatalog, ("_includes", includes.ToArray()));
            }

            return gameCatalog;
        }

        /// <summary>
        /// Tabs of <paramref name="root"/> in the order the settings service shows them: declared tabs, included catalogs
        /// first, a tab id declared twice keeping its first place.
        /// </summary>
        internal static List<SettingsTab> DeclaredTabs(SettingsCatalog root)
        {
            var tabs = new List<SettingsTab>();
            Walk(root, new HashSet<SettingsCatalog>(), catalog =>
            {
                foreach (SettingsTab tab in catalog.Tabs)
                {
                    if (tab != null && tab.Id.Length > 0 && tabs.FindIndex(t => t.Id == tab.Id) < 0)
                    {
                        tabs.Add(tab);
                    }
                }
            });
            return tabs;
        }

        /// <summary>Control schemes of the key binding definitions of <paramref name="tabId"/>, in catalog order.</summary>
        internal static List<string> BindingSchemes(SettingsCatalog root, string tabId)
        {
            var schemes = new List<string>();
            Walk(root, new HashSet<SettingsCatalog>(), catalog =>
            {
                foreach (SettingDefinition definition in catalog.Definitions)
                {
                    if (definition is KeyBindingSettingDefinition binding && binding.TabId == tabId &&
                        binding.ControlScheme.Length > 0 && !schemes.Contains(binding.ControlScheme))
                    {
                        schemes.Add(binding.ControlScheme);
                    }
                }
            });
            return schemes;
        }

        private static void Walk(SettingsCatalog catalog, HashSet<SettingsCatalog> visited, Action<SettingsCatalog> visit)
        {
            if (catalog == null || !visited.Add(catalog))
            {
                return;
            }

            foreach (SettingsCatalog include in catalog.Includes)
            {
                Walk(include, visited, visit);
            }

            visit(catalog);
        }

        // General rows, then the accessibility rows together (text size, camera shake, flashing, hold/toggle, subtitles).
        private static void AddGameplay(List<SettingDefinition> definitions)
        {
            definitions.Add(Toggle(Gameplay, "Animations", GameSettingIds.Animations, "settings.gameplay.animations",
                "settings.gameplay.animations.desc", true));
            definitions.Add(Toggle(Gameplay, "ShowHints", GameSettingIds.ShowHints, "settings.gameplay.show-hints",
                "settings.gameplay.show-hints.desc", true));
            definitions.Add(Toggle(Gameplay, "PauseOnFocusLoss", SettingIds.PauseOnFocusLoss, "settings.gameplay.pause-on-focus-loss",
                "settings.gameplay.pause-on-focus-loss.desc", true));
            // Up to 130%: fixed-size boxes (setting rows, buttons) still fit their text.
            definitions.Add(Slider(Gameplay, "TextSize", GameSettingIds.TextScale, "settings.gameplay.text-size",
                "settings.gameplay.text-size.desc", 1f, 1.3f, 0.05f, 1f, Percent, 100f));
            definitions.Add(Slider(Gameplay, "CameraShake", GameSettingIds.CameraShake, "settings.gameplay.camera-shake",
                "settings.gameplay.camera-shake.desc", 0f, 1f, 0.1f, 1f, Percent, 100f));
            definitions.Add(Toggle(Gameplay, "ReduceFlashing", GameSettingIds.ReduceFlashing, "settings.gameplay.reduce-flashing",
                "settings.gameplay.reduce-flashing.desc", false));
            definitions.Add(PressMode("SprintMode", GameSettingIds.SprintMode, "settings.gameplay.sprint-mode",
                "settings.gameplay.sprint-mode.desc"));
            definitions.Add(PressMode("CrouchMode", GameSettingIds.CrouchMode, "settings.gameplay.crouch-mode",
                "settings.gameplay.crouch-mode.desc"));
            definitions.Add(Toggle(Gameplay, "Subtitles", GameSettingIds.Subtitles, "settings.gameplay.subtitles",
                "settings.gameplay.subtitles.desc", true));
        }

        // Hold (default) or Toggle, static options.
        private static ChoiceSettingDefinition PressMode(string name, string id, string labelKey, string descriptionKey)
        {
            ChoiceSettingDefinition definition = Choice(Gameplay, name, id, labelKey, descriptionKey, string.Empty,
                GameSettingIds.PressModeHold, SettingApplyMode.Immediate);
            (string value, string labelKey)[] options =
            {
                (GameSettingIds.PressModeHold, "settings.press-mode.hold"), (GameSettingIds.PressModeToggle, "settings.press-mode.toggle"),
            };
            Fields.SetList(definition, "_options", options.Length, (element, i) =>
            {
                Fields.Relative(element, "_value", options[i].value);
                Fields.Relative(element, "_label", options[i].labelKey);
                Fields.Relative(element, "_labelIsKey", true);
            });
            return definition;
        }

        private static void AddAudioLanguage(List<SettingDefinition> definitions)
        {
            (string name, string id, string labelKey)[] volumes =
            {
                ("MasterVolume", SettingIds.AudioMasterVolume, "settings.audio.master-volume"),
                ("MusicVolume", SettingIds.AudioMusicVolume, "settings.audio.music-volume"),
                ("SfxVolume", SettingIds.AudioSfxVolume, "settings.audio.sfx-volume"),
                ("UIVolume", SettingIds.AudioUIVolume, "settings.audio.ui-volume"),
                ("VoiceVolume", SettingIds.AudioVoiceVolume, "settings.audio.voice-volume"),
            };
            (string name, string id, string labelKey)[] mutes =
            {
                ("MasterMuted", SettingIds.AudioMasterMuted, "settings.audio.master-muted"),
                ("MusicMuted", SettingIds.AudioMusicMuted, "settings.audio.music-muted"),
                ("SfxMuted", SettingIds.AudioSfxMuted, "settings.audio.sfx-muted"),
                ("UIMuted", SettingIds.AudioUIMuted, "settings.audio.ui-muted"),
                ("VoiceMuted", SettingIds.AudioVoiceMuted, "settings.audio.voice-muted"),
            };

            // Each volume followed by its mute.
            for (int i = 0; i < volumes.Length; i++)
            {
                definitions.Add(Slider(AudioLanguage, volumes[i].name, volumes[i].id, volumes[i].labelKey, "settings.audio.volume.desc",
                    0f, 1f, 0.05f, 1f, Percent, 100f));
                definitions.Add(Toggle(AudioLanguage, mutes[i].name, mutes[i].id, mutes[i].labelKey, "settings.audio.muted.desc", false));
            }

            definitions.Add(Toggle(AudioLanguage, "MuteInBackground", SettingIds.AudioMuteInBackground,
                "settings.audio.mute-in-background", "settings.audio.mute-in-background.desc", false));
            // Empty default: "Defaults" keeps the language.
            definitions.Add(Choice(AudioLanguage, "Language", SettingIds.Language, "settings.language", "settings.language.desc",
                SettingOptionProviderIds.Languages, string.Empty, SettingApplyMode.Immediate));
        }

        private static void AddControls(List<SettingDefinition> definitions)
        {
            definitions.Add(Slider(Controls, "MouseSensitivity", GameSettingIds.MouseSensitivity, "settings.controls.mouse-sensitivity",
                "settings.controls.mouse-sensitivity.desc", 0.1f, 3f, 0.1f, 1f, Multiplier, 1f));
            definitions.Add(Slider(Controls, "StickSensitivity", GameSettingIds.StickSensitivity, "settings.controls.stick-sensitivity",
                "settings.controls.stick-sensitivity.desc", 0.1f, 3f, 0.1f, 1f, Multiplier, 1f));
            definitions.Add(Toggle(Controls, "InvertY", GameSettingIds.InvertY, "settings.controls.invert-y",
                "settings.controls.invert-y.desc", false));
            // Applied by the gamepad package's rumble (IRumbleService.Enabled / Intensity).
            definitions.Add(Toggle(Controls, "Vibration", GamepadSettingIds.Vibration, "settings.controls.vibration",
                "settings.controls.vibration.desc", true));
            definitions.Add(Slider(Controls, "VibrationIntensity", GamepadSettingIds.VibrationIntensity,
                "settings.controls.vibration-intensity", "settings.controls.vibration-intensity.desc", 0f, 1f, 0.1f, 1f, Percent, 100f));
        }

        // Keyboard rows, keyboard reset, gamepad rows, gamepad reset: the reset buttons and the footer prompts work per device.
        private static void AddKeyBindings(List<SettingDefinition> definitions)
        {
            foreach ((string name, string action, string part, string labelKey, int slots) in KeyboardBindings)
            {
                for (int slot = 0; slot < slots; slot++)
                {
                    string id = $"controls.keyboard.{Kebab(name)}.{(slot == 0 ? "primary" : "secondary")}";
                    definitions.Add(Binding($"Keyboard{name}{slot}", id, labelKey, action, KeyboardMouse, part, slot));
                }
            }

            definitions.Add(Button(KeyBindings, "ResetKeyboard", "controls.reset-keyboard", "settings.controls.reset-keyboard",
                "settings.controls.reset-keyboard.desc", KeyBindingActionIds.ResetBindings, KeyboardMouse));

            foreach ((string name, string action, string labelKey) in GamepadBindings)
            {
                definitions.Add(Binding($"Gamepad{name}", $"controls.gamepad.{Kebab(name)}", labelKey, action, Gamepad, string.Empty, 0));
            }

            definitions.Add(Button(KeyBindings, "ResetGamepad", "controls.reset-gamepad", "settings.controls.reset-gamepad",
                "settings.controls.reset-gamepad.desc", KeyBindingActionIds.ResetBindings, Gamepad));
        }

        // Monitor first: the resolutions are those of the monitor the window is on. Mode values wait for Apply (one display
        // change, then "Keep these display settings?"); empty defaults keep the current value.
        private static void AddGraphics(List<SettingDefinition> definitions)
        {
            definitions.Add(Choice(Graphics, "Monitor", DesktopSettingIds.DisplayMonitor, "settings.display.monitor",
                "settings.display.monitor.desc", DisplayOptionProviderIds.Monitors, string.Empty, SettingApplyMode.Deferred));
            definitions.Add(Choice(Graphics, "FullScreenMode", DesktopSettingIds.DisplayFullScreenMode, "settings.display.fullscreen-mode",
                "settings.display.fullscreen-mode.desc", DisplayOptionProviderIds.FullScreenModes,
                nameof(FullScreenMode.FullScreenWindow), SettingApplyMode.Deferred));
            definitions.Add(Choice(Graphics, "Resolution", DesktopSettingIds.DisplayResolution, "settings.display.resolution",
                "settings.display.resolution.desc", DisplayOptionProviderIds.Resolutions, string.Empty, SettingApplyMode.Deferred));
            definitions.Add(Choice(Graphics, "RefreshRate", DesktopSettingIds.DisplayRefreshRate, "settings.display.refresh-rate",
                "settings.display.refresh-rate.desc", DisplayOptionProviderIds.RefreshRates, string.Empty, SettingApplyMode.Deferred));
            definitions.Add(Toggle(Graphics, "VSync", DesktopSettingIds.DisplayVSync, "settings.display.vsync",
                "settings.display.vsync.desc", true));

            ChoiceSettingDefinition frameRate = Choice(Graphics, "FrameRateCap", DesktopSettingIds.DisplayFrameRateCap,
                "settings.display.frame-rate-cap", "settings.display.frame-rate-cap.desc", string.Empty, "0", SettingApplyMode.Immediate);
            Fields.SetList(frameRate, "_options", FrameRateCaps.Length, (element, i) =>
            {
                Fields.Relative(element, "_value", FrameRateCaps[i].value);
                Fields.Relative(element, "_label", FrameRateCaps[i].labelKey);
                Fields.Relative(element, "_labelIsKey", true);
            });
            definitions.Add(frameRate);

            definitions.Add(Choice(Graphics, "Quality", DesktopSettingIds.DisplayQuality, "settings.display.quality",
                "settings.display.quality.desc", DisplayOptionProviderIds.QualityLevels, string.Empty, SettingApplyMode.Immediate));
            definitions.Add(Slider(Graphics, "RenderScale", DesktopSettingIds.DisplayRenderScale, "settings.display.render-scale",
                "settings.display.render-scale.desc", 0.5f, 1.5f, 0.05f, 1f, Percent, 100f));
            definitions.Add(Slider(Graphics, "Brightness", DesktopSettingIds.DisplayBrightness, "settings.display.brightness",
                "settings.display.brightness.desc", 0.5f, 1.5f, 0.05f, 1f, Percent, 100f));
            // Deferred: rescaling the canvases while the slider is dragged would move the slider under the pointer. The
            // largest scale still fits the settings popup on the reference screen.
            SliderSettingDefinition uiScale = Slider(Graphics, "UIScale", GameSettingIds.UIScale, "settings.graphics.ui-scale",
                "settings.graphics.ui-scale.desc", 0.8f, 1.2f, 0.05f, 1f, Percent, 100f);
            Fields.Set(uiScale, ("_applyMode", new EnumName(nameof(SettingApplyMode.Deferred))));
            definitions.Add(uiScale);
        }

        private static ToggleSettingDefinition Toggle(string tab, string name, string id, string labelKey, string descriptionKey, bool value)
        {
            var definition = Definition<ToggleSettingDefinition>(tab, name, id, labelKey, descriptionKey, SettingApplyMode.Immediate);
            Fields.Set(definition, ("_defaultValue", value));
            return definition;
        }

        private static SliderSettingDefinition Slider(string tab, string name, string id, string labelKey, string descriptionKey,
            float min, float max, float step, float value, string format, float multiplier)
        {
            var definition = Definition<SliderSettingDefinition>(tab, name, id, labelKey, descriptionKey, SettingApplyMode.Immediate);
            Fields.Set(definition, ("_min", min), ("_max", max), ("_step", step), ("_defaultValue", value), ("_displayFormat", format),
                ("_displayMultiplier", multiplier));
            return definition;
        }

        private static ChoiceSettingDefinition Choice(string tab, string name, string id, string labelKey, string descriptionKey,
            string providerId, string value, SettingApplyMode mode)
        {
            var definition = Definition<ChoiceSettingDefinition>(tab, name, id, labelKey, descriptionKey, mode);
            Fields.Set(definition, ("_optionsProviderId", providerId), ("_defaultValue", value));
            Fields.SetList(definition, "_options", 0, (element, i) => { });
            return definition;
        }

        private static KeyBindingSettingDefinition Binding(string name, string id, string labelKey, string action, string scheme,
            string part, int slot)
        {
            var definition = Definition<KeyBindingSettingDefinition>(KeyBindings, name, id, labelKey, BindingDescription,
                SettingApplyMode.Immediate);
            Fields.Set(definition, ("_action", action), ("_controlScheme", scheme), ("_compositePart", part), ("_slot", slot));
            return definition;
        }

        private static ButtonSettingDefinition Button(string tab, string name, string id, string labelKey, string descriptionKey,
            string actionId, string argument)
        {
            var definition = Definition<ButtonSettingDefinition>(tab, name, id, labelKey, descriptionKey, SettingApplyMode.Immediate);
            Fields.Set(definition, ("_actionId", actionId), ("_argument", argument));
            return definition;
        }

        // Definitions that moved to another tab: P5 moved vibration from Gameplay to Controls, the key binding rows moved from
        // Controls to Key Bindings. The assets left in the old tab folder are no longer in any catalog. Setting ids (and so
        // the saved values) did not change.
        private static void DeleteMovedDefinitions()
        {
            var moved = new List<string> { $"{Gameplay}/Setting_Vibration", $"{Gameplay}/Setting_VibrationIntensity" };
            moved.Add($"{Controls}/Setting_ResetKeyboard");
            moved.Add($"{Controls}/Setting_ResetGamepad");
            foreach ((string name, _, _, _, int slots) in KeyboardBindings)
            {
                for (int slot = 0; slot < slots; slot++)
                {
                    moved.Add($"{Controls}/Setting_Keyboard{name}{slot}");
                }
            }

            foreach ((string name, _, _) in GamepadBindings)
            {
                moved.Add($"{Controls}/Setting_Gamepad{name}");
            }

            foreach (string name in moved)
            {
                string path = $"{BuildPaths.DataSettings}/{name}.asset";
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null && !AssetDatabase.DeleteAsset(path))
                {
                    Debug.LogWarning($"[Game] Could not delete the moved setting definition {path}; delete it by hand.");
                }
            }
        }

        private static T Definition<T>(string tab, string name, string id, string labelKey, string descriptionKey, SettingApplyMode mode)
            where T : SettingDefinition
        {
            var definition = BuildUtility.Asset<T>($"{BuildPaths.DataSettings}/{tab}/Setting_{name}.asset");
            Fields.Set(definition, ("_id", id), ("_labelKey", labelKey), ("_descriptionKey", descriptionKey), ("_tabId", tab),
                ("_applyMode", new EnumName(mode.ToString())));
            return definition;
        }

        // "AltAttack" → "alt-attack".
        private static string Kebab(string name)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0)
                {
                    text.Append('-');
                }

                text.Append(char.ToLowerInvariant(name[i]));
            }

            return text.ToString();
        }
    }
}
