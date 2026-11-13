using System;
using System.Collections.Generic;
using System.Linq;
using Game.Contracts.Settings;
using NUnit.Framework;
using Paowa.Desktop;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Tests.Editor
{
    /// <summary>
    /// The settings catalog built by Game › Build All: assigned in <c>DesktopSettings</c>, unique ids, every definition on a
    /// declared tab, the ids the plugin and the game apply carry the kind and apply mode their applier expects, every key
    /// binding row finds its binding in the input actions (the secondary keyboard slots included), and the settings popup
    /// has a page for every tab. Skipped until the project is built.
    /// </summary>
    public sealed class SettingsCatalogTests
    {
        private const string PopupPath = "Assets/game/prefabs/popups/Popup_Settings.prefab";

        [Test]
        public void IdsAreUnique()
        {
            List<string> duplicates = Definitions().GroupBy(d => d.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.That(duplicates, Is.Empty, string.Join(", ", duplicates));
        }

        [Test]
        public void EveryDefinitionIsOnADeclaredTab()
        {
            var tabs = new HashSet<string>(Tabs().Select(tab => tab.Id));
            List<string> orphans = Definitions().Where(d => !tabs.Contains(d.TabId)).Select(d => $"{d.Id} ({d.TabId})").ToList();
            Assert.That(orphans, Is.Empty, string.Join(", ", orphans));
        }

        [Test]
        public void AppliedIdsHaveTheKindTheirApplierExpects()
        {
            Dictionary<string, SettingDefinition> byId = Definitions().ToDictionary(d => d.Id);
            var problems = new List<string>();
            Expect<SliderSettingDefinition>(byId, problems, SettingApplyMode.Immediate, SettingIds.AudioMasterVolume,
                SettingIds.AudioMusicVolume, SettingIds.AudioSfxVolume, SettingIds.AudioUIVolume,
                SettingIds.AudioVoiceVolume, DesktopSettingIds.DisplayRenderScale, DesktopSettingIds.DisplayBrightness,
                GameSettingIds.CameraShake, GamepadSettingIds.VibrationIntensity, GameSettingIds.MouseSensitivity,
                GameSettingIds.StickSensitivity, GameSettingIds.TextScale);
            Expect<ToggleSettingDefinition>(byId, problems, SettingApplyMode.Immediate, SettingIds.AudioMasterMuted,
                SettingIds.AudioMuteInBackground, DesktopSettingIds.DisplayVSync, SettingIds.PauseOnFocusLoss,
                GameSettingIds.Animations, GameSettingIds.ShowHints, GamepadSettingIds.Vibration, GameSettingIds.InvertY,
                GameSettingIds.ReduceFlashing, GameSettingIds.Subtitles);
            Expect<ChoiceSettingDefinition>(byId, problems, SettingApplyMode.Deferred, DesktopSettingIds.DisplayResolution,
                DesktopSettingIds.DisplayFullScreenMode, DesktopSettingIds.DisplayRefreshRate, DesktopSettingIds.DisplayMonitor);
            Expect<ChoiceSettingDefinition>(byId, problems, SettingApplyMode.Immediate, SettingIds.Language,
                DesktopSettingIds.DisplayFrameRateCap, DesktopSettingIds.DisplayQuality, GameSettingIds.SprintMode,
                GameSettingIds.CrouchMode);
            Expect<SliderSettingDefinition>(byId, problems, SettingApplyMode.Deferred, GameSettingIds.UIScale);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EveryKeyBindingRowFindsItsBinding()
        {
            var asset = Resources.Load<InputActionAsset>("Paowa/InputActions");
            Assert.That(asset, Is.Not.Null);
            var problems = new List<string>();
            foreach (KeyBindingSettingDefinition row in Definitions().OfType<KeyBindingSettingDefinition>())
            {
                InputAction action = asset.FindAction(row.Action);
                if (action == null)
                {
                    problems.Add($"{row.Id}: no action {row.Action}");
                    continue;
                }

                // Same rule as KeyBindingSettingDefinition.TryFindBinding over Core's binding list.
                int matches = action.bindings.Count(binding => !binding.isComposite &&
                                                               InputBindingInfo.BelongsToScheme(binding.groups, row.ControlScheme) &&
                                                               string.Equals(binding.isPartOfComposite ? binding.name : string.Empty,
                                                                   row.CompositePart, StringComparison.OrdinalIgnoreCase));
                if (matches <= row.Slot)
                {
                    problems.Add($"{row.Id}: {row.Action} has {matches} {row.ControlScheme} binding(s) for part '{row.CompositePart}', slot {row.Slot} needed");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void ThePopupHasAPagePerTab()
        {
            var popup = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);
            if (popup == null)
            {
                Assert.Ignore("Nothing built yet: run Game › Build All first.");
            }

            Component component = popup.GetComponents<Component>().First(c => c != null && c.GetType().Name == "SettingsPopup");
            SerializedProperty pages = new SerializedObject(component).FindProperty("_pages");
            var pageTabs = new List<string>();
            for (int i = 0; i < pages.arraySize; i++)
            {
                var page = (Component)pages.GetArrayElementAtIndex(i).objectReferenceValue;
                pageTabs.Add(page != null ? new SerializedObject(page).FindProperty("_tabId").stringValue : null);
            }

            Assert.That(pageTabs, Is.EqualTo(Tabs().Select(tab => tab.Id).ToList()));
        }

        private static void Expect<T>(Dictionary<string, SettingDefinition> byId, List<string> problems, SettingApplyMode mode,
            params string[] ids) where T : SettingDefinition
        {
            foreach (string id in ids)
            {
                if (!byId.TryGetValue(id, out SettingDefinition definition))
                {
                    problems.Add($"{id}: missing");
                }
                else if (!(definition is T) || definition.ApplyMode != mode)
                {
                    problems.Add($"{id}: {definition.GetType().Name} {definition.ApplyMode}, expected {typeof(T).Name} {mode}");
                }
            }
        }

        private static SettingsCatalog Catalog()
        {
            var settings = Resources.Load<SettingsProfile>(SettingsProfile.ResourcePath);
            if (settings == null || settings.Catalog == null)
            {
                Assert.Ignore("No settings catalog yet: run Game › Build All first.");
            }

            return settings.Catalog;
        }

        private static List<SettingDefinition> Definitions()
        {
            var definitions = new List<SettingDefinition>();
            Walk(Catalog(), new HashSet<SettingsCatalog>(), catalog => definitions.AddRange(catalog.Definitions.Where(d => d != null)));
            return definitions;
        }

        private static List<SettingsTab> Tabs()
        {
            var tabs = new List<SettingsTab>();
            Walk(Catalog(), new HashSet<SettingsCatalog>(), catalog =>
                tabs.AddRange(catalog.Tabs.Where(tab => tab != null && tabs.All(known => known.Id != tab.Id))));
            return tabs;
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
    }
}
