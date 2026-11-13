using System.Collections.Generic;
using System.Linq;
using Paowa.Gamepad;
using Paowa.Input;
using UnityEditor;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Writes the placeholder glyph set: one table for keyboard and mouse and one per gamepad family (Xbox, PlayStation,
    /// Switch, Generic), keyed by the control path inside the device (<c>buttonSouth</c>, <c>escape</c>, <c>leftButton</c>).
    /// Sprites come from <c>art/sprites/glyphs</c>. An entry with a text is a frame the prompt writes that label on (key cap,
    /// Xbox/Switch face buttons, shoulders, triggers, sticks, start/select: the family's names for its buttons); an entry
    /// without text is a complete picture (mouse, arrows, PlayStation symbols, generic face buttons, d-pad). Keys without
    /// an entry show their display string on the prompt's key frame, so only keys whose text differs from it are listed.
    /// Real glyph art replaces the PNG files one for one; art that already shows its label (one picture per key) needs its
    /// entry's text removed here, and frames keep a 9-slice border in <c>ProjectSettingsBuilder.SpriteBorders</c> so they
    /// stretch with the text.
    /// </summary>
    internal static class GlyphSetBuilder
    {
        private const string Key = "ui-glyphs/key";

        private static readonly (string path, string sprite, string text)[] KeyboardMouse =
        {
            ("escape", Key, "Esc"),
            ("enter", Key, "Enter"),
            ("numpadEnter", Key, "Enter"),
            ("space", Key, "Space"),
            ("tab", Key, "Tab"),
            ("backspace", Key, "Backspace"),
            ("leftShift", Key, "Shift"),
            ("rightShift", Key, "Shift"),
            ("leftCtrl", Key, "Ctrl"),
            ("rightCtrl", Key, "Ctrl"),
            ("leftAlt", Key, "Alt"),
            ("rightAlt", Key, "Alt"),
            ("upArrow", "ui-glyphs/key-arrow-up", null),
            ("downArrow", "ui-glyphs/key-arrow-down", null),
            ("leftArrow", "ui-glyphs/key-arrow-left", null),
            ("rightArrow", "ui-glyphs/key-arrow-right", null),
            ("leftButton", "ui-glyphs/mouse-left", null),
            ("rightButton", "ui-glyphs/mouse-right", null),
            ("middleButton", "ui-glyphs/mouse-middle", null),
            ("scroll", "ui-glyphs/mouse-scroll", null),
            ("delta", "ui-glyphs/mouse-move", null),
            ("position", "ui-glyphs/mouse-move", null),
        };

        /// <summary>Names of one gamepad family's controls.</summary>
        private sealed class PadNames
        {
            internal GamepadFamily Family;
            internal string FacePrefix;
            internal string South, East, West, North;
            internal string LeftShoulder, RightShoulder, LeftTrigger, RightTrigger;
            internal string LeftStickPress, RightStickPress, Start, Select;
        }

        private static readonly PadNames[] Pads =
        {
            new PadNames
            {
                Family = GamepadFamily.Xbox, FacePrefix = "ui-glyphs/xbox-face-", South = "A", East = "B", West = "X", North = "Y",
                LeftShoulder = "LB", RightShoulder = "RB", LeftTrigger = "LT", RightTrigger = "RT",
                LeftStickPress = "LS", RightStickPress = "RS", Start = "Menu", Select = "View",
            },
            new PadNames
            {
                // The face sprites are the symbols themselves: no label.
                Family = GamepadFamily.PlayStation, FacePrefix = "ui-glyphs/playstation-face-", South = null, East = null,
                West = null, North = null, LeftShoulder = "L1", RightShoulder = "R1", LeftTrigger = "L2",
                RightTrigger = "R2", LeftStickPress = "L3", RightStickPress = "R3", Start = "Options", Select = "Share",
            },
            new PadNames
            {
                // Nintendo layout: the bottom button is B and the right one A.
                Family = GamepadFamily.Switch, FacePrefix = "ui-glyphs/switch-face-", South = "B", East = "A", West = "Y", North = "X",
                LeftShoulder = "L", RightShoulder = "R", LeftTrigger = "ZL", RightTrigger = "ZR",
                LeftStickPress = "LS", RightStickPress = "RS", Start = "+", Select = "-",
            },
            new PadNames
            {
                // The face sprites show the button's position: no label.
                Family = GamepadFamily.Generic, FacePrefix = "ui-glyphs/generic-face-", South = null, East = null, West = null,
                North = null, LeftShoulder = "LB", RightShoulder = "RB", LeftTrigger = "LT", RightTrigger = "RT",
                LeftStickPress = "L3", RightStickPress = "R3", Start = "Start", Select = "Select",
            },
        };

        /// <summary>Creates or updates the glyph set at <paramref name="path"/>; returns it.</summary>
        internal static GlyphSet Build(string path)
        {
            var set = BuildUtility.Asset<GlyphSet>(path);
            var tables = new List<GlyphTable>
            {
                new GlyphTable(InputDeviceKind.KeyboardMouse, GamepadFamily.None,
                    KeyboardMouse.Select(entry => Entry(entry.path, entry.sprite, entry.text))),
            };
            tables.AddRange(Pads.Select(Pad));
            set.SetTables(tables);
            EditorUtility.SetDirty(set);
            return set;
        }

        private static GlyphTable Pad(PadNames names)
        {
            var entries = new List<GlyphEntry>
            {
                Entry("buttonSouth", names.FacePrefix + "south", names.South),
                Entry("buttonEast", names.FacePrefix + "east", names.East),
                Entry("buttonWest", names.FacePrefix + "west", names.West),
                Entry("buttonNorth", names.FacePrefix + "north", names.North),
                Entry("leftShoulder", "ui-glyphs/gamepad-shoulder-left", names.LeftShoulder),
                Entry("rightShoulder", "ui-glyphs/gamepad-shoulder-right", names.RightShoulder),
                Entry("leftTrigger", "ui-glyphs/gamepad-trigger-left", names.LeftTrigger),
                Entry("rightTrigger", "ui-glyphs/gamepad-trigger-right", names.RightTrigger),
                // "leftStick/up" and the like fall back to the stick's entry.
                Entry("leftStick", "ui-glyphs/gamepad-stick-left", "L"),
                Entry("rightStick", "ui-glyphs/gamepad-stick-right", "R"),
                Entry("leftStickPress", "ui-glyphs/gamepad-stick-left", names.LeftStickPress),
                Entry("rightStickPress", "ui-glyphs/gamepad-stick-right", names.RightStickPress),
                Entry("dpad", "ui-glyphs/gamepad-dpad", null),
                Entry("dpad/up", "ui-glyphs/gamepad-dpad-up", null),
                Entry("dpad/down", "ui-glyphs/gamepad-dpad-down", null),
                Entry("dpad/left", "ui-glyphs/gamepad-dpad-left", null),
                Entry("dpad/right", "ui-glyphs/gamepad-dpad-right", null),
                Entry("start", "ui-glyphs/gamepad-start", names.Start),
                Entry("select", "ui-glyphs/gamepad-select", names.Select),
            };
            return new GlyphTable(InputDeviceKind.Gamepad, names.Family, entries);
        }

        private static GlyphEntry Entry(string controlPath, string sprite, string text)
        {
            return new GlyphEntry(controlPath, BuildUtility.Sprite(sprite), text);
        }
    }
}
