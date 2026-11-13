using System.Collections.Generic;
using System.Text;
using Paowa.Core.Foundation.Services;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.UI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Menu <c>Game › Diagnostics › UI Input Report</c> (Play Mode): one text report of everything uGUI input depends on:
    /// event systems and their input module actions, Core's interaction mode and top focus scope, what the pointer hits,
    /// non-interactable selectables and invisible raycast blockers. The report is logged and copied to the clipboard.
    /// </summary>
    internal static class UiInputDiagnostics
    {
        private const string MenuPath = "Game/Diagnostics/UI Input Report";
        private const int ListLimit = 12;

        [MenuItem(MenuPath, priority = 200)]
        private static void Report()
        {
            var text = new StringBuilder("[Game] UI input report\n");
            text.AppendLine($"Time.timeScale={Time.timeScale} focused={Application.isFocused} cursor={Cursor.lockState}/visible={Cursor.visible}");
            InputSettings settings = InputSystem.settings;
            text.AppendLine($"InputSystem update={settings.updateMode} background={settings.backgroundBehavior} editorPlayMode={settings.editorInputBehaviorInPlayMode}");
            text.AppendLine("Devices: " + string.Join(", ", DeviceNames()));

            AppendEventSystems(text);
            AppendCore(text);
            AppendPointerHits(text);
            AppendSelectables(text);
            AppendCanvasGroups(text);

            string report = text.ToString();
            Debug.Log(report);
            EditorGUIUtility.systemCopyBuffer = report;
        }

        [MenuItem(MenuPath, true)]
        private static bool CanReport()
        {
            return EditorApplication.isPlaying;
        }

        private static IEnumerable<string> DeviceNames()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                yield return device.enabled ? device.name : device.name + "(disabled)";
            }
        }

        private static void AppendEventSystems(StringBuilder text)
        {
            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            text.AppendLine($"EventSystems: {systems.Length}, current={Name(EventSystem.current)}");
            foreach (EventSystem system in systems)
            {
                text.AppendLine($"- {Path(system.transform)} scene={system.gameObject.scene.name} active={system.isActiveAndEnabled} " +
                                $"sendNavigation={system.sendNavigationEvents} selected={Name(system.currentSelectedGameObject)} " +
                                $"module={(system.currentInputModule != null ? system.currentInputModule.GetType().Name : "none")}");
                foreach (BaseInputModule module in system.GetComponents<BaseInputModule>())
                {
                    text.AppendLine($"  module {module.GetType().Name} enabled={module.isActiveAndEnabled}");
                    if (module is InputSystemUIInputModule ui)
                    {
                        text.AppendLine($"  actionsAsset={Name(ui.actionsAsset)} id={Id(ui.actionsAsset)} " +
                                        $"coreAsset id={CoreAssetId()}");
                        AppendAction(text, "move", ui.move);
                        AppendAction(text, "submit", ui.submit);
                        AppendAction(text, "cancel", ui.cancel);
                        AppendAction(text, "point", ui.point);
                        AppendAction(text, "leftClick", ui.leftClick);
                    }
                }
            }
        }

        private static string CoreAssetId()
        {
            return Id(Resources.Load<InputActionAsset>("Paowa/InputActions"));
        }

        /// <summary>Entity id of an object (to tell two loaded copies of one asset apart), or "none".</summary>
        private static string Id(Object target)
        {
            return target != null ? target.GetEntityId().ToString() : "none";
        }

        private static void AppendAction(StringBuilder text, string label, InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            if (action == null)
            {
                text.AppendLine($"    {label}: NO ACTION (reference={Name(reference)})");
                return;
            }

            text.AppendLine($"    {label}: {action.actionMap?.name}/{action.name} enabled={action.enabled} phase={action.phase} " +
                            $"controls={action.controls.Count} asset id={Id(action.actionMap?.asset)}");
        }

        private static void AppendCore(StringBuilder text)
        {
            text.AppendLine(CoreServiceLocator.TryGet(out IInteractionModeService modes)
                            && CoreServiceLocator.TryGet(out IInputService devices)
                            && CoreServiceLocator.TryGet(out IInputGlyphs glyphs)
                ? $"Paowa mode={modes.Mode} device={devices.CurrentDeviceInfo.Kind} scheme={glyphs.CurrentControlScheme}"
                : "Paowa: no interaction mode, input or glyph service");
            if (!CoreServiceLocator.TryGet(out IUIFocusService focus))
            {
                text.AppendLine("Core: no IUIFocusService");
                return;
            }

            FocusScope top = focus.TopScope;
            if (top == null)
            {
                text.AppendLine("Top focus scope: none");
                return;
            }

            Selectable first = top.FirstSelected;
            text.AppendLine($"Top focus scope: {Path(top.transform)} trap={top.TrapFocus} first={Name(first)} " +
                            $"firstInteractable={(first != null && first.IsInteractable())} firstActive={(first != null && first.gameObject.activeInHierarchy)}");
        }

        private static void AppendPointerHits(StringBuilder text)
        {
            EventSystem current = EventSystem.current;
            if (current == null || Mouse.current == null)
            {
                text.AppendLine("Pointer hits: no event system or mouse");
                return;
            }

            var data = new PointerEventData(current) { position = Mouse.current.position.ReadValue() };
            var hits = new List<RaycastResult>();
            current.RaycastAll(data, hits);
            text.AppendLine($"Pointer at {data.position}: {hits.Count} hits");
            for (int i = 0; i < hits.Count && i < ListLimit; i++)
            {
                text.AppendLine($"- {Path(hits[i].gameObject.transform)} (sort {hits[i].sortingOrder}, depth {hits[i].depth})");
            }
        }

        private static void AppendSelectables(StringBuilder text)
        {
            Selectable[] all = Object.FindObjectsByType<Selectable>();
            int blocked = 0;
            var lines = new StringBuilder();
            foreach (Selectable selectable in all)
            {
                if (selectable.IsInteractable())
                {
                    continue;
                }

                blocked++;
                if (blocked <= ListLimit)
                {
                    lines.AppendLine($"- {Path(selectable.transform)} interactable={selectable.interactable} (a parent CanvasGroup may block it)");
                }
            }

            text.AppendLine($"Active selectables: {all.Length}, not interactable: {blocked}");
            text.Append(lines);
        }

        private static void AppendCanvasGroups(StringBuilder text)
        {
            foreach (CanvasGroup group in Object.FindObjectsByType<CanvasGroup>())
            {
                bool invisibleBlocker = group.blocksRaycasts && group.alpha <= 0.01f;
                if (!group.interactable || invisibleBlocker)
                {
                    text.AppendLine($"CanvasGroup {Path(group.transform)} alpha={group.alpha} interactable={group.interactable} " +
                                    $"blocksRaycasts={group.blocksRaycasts} ignoreParents={group.ignoreParentGroups}");
                }
            }
        }

        private static string Name(Object target)
        {
            return target != null ? target.name : "null";
        }

        private static string Path(Transform target)
        {
            string path = target.name;
            for (Transform parent = target.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }
    }
}
