using System;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using Paowa.UI.Navigation;

namespace Game.Systems.Settings
{
    /// <summary>
    /// What the rows of the settings popup share: the settings model, input and glyph services (any may be null when its
    /// module is disabled; rows then show what they can), the rebind state, and the popup's handlers for a rebind request
    /// and a button row.
    /// </summary>
    internal sealed class SettingRowContext
    {
        private readonly Action<KeyBindingSettingDefinition> _rebind;
        private readonly Action<ButtonSettingDefinition> _invoke;

        internal SettingRowContext(ISettingsService settings, IInputService input, IInputGlyphs glyphs,
            KeyBindingRebinder rebinder, string gamepadScheme, Action<KeyBindingSettingDefinition> rebind,
            Action<ButtonSettingDefinition> invoke)
        {
            Settings = settings;
            Input = input;
            Glyphs = glyphs;
            Rebinder = rebinder;
            GamepadScheme = gamepadScheme ?? string.Empty;
            _rebind = rebind;
            _invoke = invoke;
        }

        /// <summary>Settings model.</summary>
        internal ISettingsService Settings { get; }

        /// <summary>Input service (bindings); null without the input package.</summary>
        internal IInputService Input { get; }

        /// <summary>Glyphs of bindings and the last gamepad family; null without the gamepad package.</summary>
        internal IInputGlyphs Glyphs { get; }

        /// <summary>Rebind state; null without the rebinding package.</summary>
        internal KeyBindingRebinder Rebinder { get; }

        /// <summary>Name of the gamepad control scheme (rows of that scheme draw gamepad glyphs).</summary>
        internal string GamepadScheme { get; }

        /// <summary>Starts rebinding a key binding slot.</summary>
        internal void Rebind(KeyBindingSettingDefinition definition)
        {
            _rebind?.Invoke(definition);
        }

        /// <summary>Runs a button row.</summary>
        internal void Invoke(ButtonSettingDefinition definition)
        {
            _invoke?.Invoke(definition);
        }
    }
}
