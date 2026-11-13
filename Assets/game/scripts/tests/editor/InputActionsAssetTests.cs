using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Checks the game's input actions asset: it loads from the Resources path Core's Input System backend reads, the UI
    /// map has every action <c>InputSystemUIInputModule</c> is wired to (assigned by name by <c>UIInputModuleActions</c>),
    /// the Menu and Gameplay maps (Core contexts) have their actions, and every binding of those two maps belongs to exactly one of the two control schemes, with each
    /// action reachable from both schemes. Menu has no Back action: Cancel/Back is the UI map's Cancel, dispatched by the
    /// desktop Cancel stack, so one key press is never handled twice. The Debug map holds the development keys of gameplay;
    /// Look carries no processor (it is normalized in code).
    /// </summary>
    public sealed class InputActionsAssetTests
    {
        // Core: InputSystemBackendModule.ActionsResourcePath (internal).
        private const string ResourcePath = "Paowa/InputActions";
        private const string KeyboardMouse = "KeyboardMouse";
        private const string Gamepad = "Gamepad";
        private const char GroupSeparator = ';';

        private static readonly string[] Schemes = { KeyboardMouse, Gamepad };

        private static readonly Dictionary<string, string[]> RequiredActions = new Dictionary<string, string[]>
        {
            {
                "UI", new[]
                {
                    "Navigate", "Submit", "Cancel", "Point", "Click", "ScrollWheel", "MiddleClick", "RightClick",
                    "TrackedDevicePosition", "TrackedDeviceOrientation",
                }
            },
            { "Menu", new[] { "TabPrevious", "TabNext", "Pause", "Apply", "Revert", "Defaults", "Delete" } },
            { "Gameplay", new[] { "Item1", "Item2", "Item3", "Item4", "Pause" } },
        };

        // Maps driven by Core contexts, whose bindings must carry a control scheme.
        private static readonly string[] SchemeMaps = { "Menu", "Gameplay" };

        [Test]
        public void TheAssetLoadsFromCoresResourcePath()
        {
            Assert.That(Load(), Is.Not.Null, $"Core's Input System backend reads Resources/{ResourcePath}.");
        }

        [Test]
        public void EveryMapHasItsActions()
        {
            InputActionAsset asset = Load();
            var missing = new List<string>();
            foreach (KeyValuePair<string, string[]> map in RequiredActions)
            {
                missing.AddRange(map.Value.Where(action => asset.FindAction($"{map.Key}/{action}") == null)
                    .Select(action => $"{map.Key}/{action}"));
            }

            Assert.That(missing, Is.Empty, string.Join(", ", missing));
        }

        [Test]
        public void MenuHasNoBackAction()
        {
            Assert.That(Load().FindAction("Menu/Back"), Is.Null, "Back is UI/Cancel, handled by the Cancel stack.");
        }

        [Test]
        public void BothControlSchemesExist()
        {
            InputActionAsset asset = Load();
            List<string> missing = Schemes.Where(scheme => asset.FindControlSchemeIndex(scheme) < 0).ToList();
            Assert.That(missing, Is.Empty, string.Join(", ", missing));
        }

        [Test]
        public void MenuAndGameplayBindingsBelongToOneScheme()
        {
            InputActionAsset asset = Load();
            var problems = new List<string>();
            foreach (string mapName in SchemeMaps)
            {
                foreach (InputBinding binding in asset.FindActionMap(mapName, true).bindings.Where(binding => !binding.isComposite))
                {
                    string[] groups = (binding.groups ?? string.Empty)
                        .Split(new[] { GroupSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    if (groups.Length != 1 || !Schemes.Contains(groups[0]))
                    {
                        problems.Add($"{mapName}/{binding.action} {binding.path}: groups '{binding.groups}'");
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void MenuAndGameplayActionsAreReachableFromBothSchemes()
        {
            InputActionAsset asset = Load();
            var problems = new List<string>();
            foreach (string mapName in SchemeMaps)
            {
                foreach (InputAction action in asset.FindActionMap(mapName, true).actions)
                {
                    problems.AddRange(Schemes
                        .Where(scheme => !action.bindings.Any(binding => !binding.isComposite && binding.groups == scheme))
                        .Select(scheme => $"{mapName}/{action.name} has no {scheme} binding"));
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void TheUIInputModuleFindsEveryActionItAssignsByName()
        {
            InputActionAsset asset = Load();
            var root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            try
            {
                var actions = root.AddComponent<UIInputModuleActions>();
                string[] missing = actions.ActionNames.Where(action => asset.FindAction(action) == null).ToArray();
                Assert.That(missing, Is.Empty, "UI input breaks without: " + string.Join(", ", missing));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static InputActionAsset Load()
        {
            var asset = Resources.Load<InputActionAsset>(ResourcePath);
            Assert.That(asset, Is.Not.Null, $"Missing InputActionAsset at Resources/{ResourcePath}.");
            return asset;
        }
    }
}
