using Paowa.Input.Rebinding;
namespace Game.Systems.Settings
{
    /// <summary>Row prefab a setting definition is drawn with.</summary>
    internal enum SettingRowKind
    {
        /// <summary>A definition kind the settings screen has no row for; skipped with a warning.</summary>
        Unknown = 0,

        /// <summary><c>ToggleSettingDefinition</c>: on/off value button.</summary>
        Toggle = 1,

        /// <summary><c>SliderSettingDefinition</c>: slider with its formatted value.</summary>
        Slider = 2,

        /// <summary><c>ChoiceSettingDefinition</c>: stepper "◄ value ►".</summary>
        Choice = 3,

        /// <summary><c>KeyBindingSettingDefinition</c>: one rebind button per slot.</summary>
        KeyBinding = 4,

        /// <summary><c>ButtonSettingDefinition</c>: runs the button's action.</summary>
        Button = 5
    }
}
