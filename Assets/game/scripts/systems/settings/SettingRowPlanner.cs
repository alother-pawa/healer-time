using System;
using System.Collections.Generic;
using Paowa.Input.Rebinding;
using Paowa.Settings;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Turns a tab's definitions (catalog order) into rows: one row per definition, except that key binding definitions of
    /// the same action, composite part and control scheme share one row with a column per slot (primary, secondary),
    /// placed where the first of them appears. Definitions of an unknown kind are left out.
    /// </summary>
    internal static class SettingRowPlanner
    {
        /// <summary>Row kind of <paramref name="definition"/>.</summary>
        internal static SettingRowKind KindOf(SettingDefinition definition)
        {
            switch (definition)
            {
                case ToggleSettingDefinition _:
                    return SettingRowKind.Toggle;
                case SliderSettingDefinition _:
                    return SettingRowKind.Slider;
                case ChoiceSettingDefinition _:
                    return SettingRowKind.Choice;
                case KeyBindingSettingDefinition _:
                    return SettingRowKind.KeyBinding;
                case ButtonSettingDefinition _:
                    return SettingRowKind.Button;
                default:
                    return SettingRowKind.Unknown;
            }
        }

        /// <summary>
        /// Control scheme a row belongs to: a key binding's scheme, the scheme argument of a reset-bindings button; empty
        /// (every scheme) for anything else.
        /// </summary>
        internal static string SchemeOf(SettingDefinition definition)
        {
            switch (definition)
            {
                case KeyBindingSettingDefinition binding:
                    return binding.ControlScheme;
                case ButtonSettingDefinition button when button.ActionId == KeyBindingActionIds.ResetBindings:
                    return button.Argument;
                default:
                    return string.Empty;
            }
        }

        /// <summary>Rows of <paramref name="definitions"/> in display order.</summary>
        internal static List<SettingRowPlan> Plan(IReadOnlyList<SettingDefinition> definitions)
        {
            var plans = new List<SettingRowPlan>();
            var bindingRows = new Dictionary<string, List<SettingDefinition>>(StringComparer.OrdinalIgnoreCase);
            if (definitions == null)
            {
                return plans;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                SettingDefinition definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                SettingRowKind kind = KindOf(definition);
                if (kind == SettingRowKind.Unknown)
                {
                    continue;
                }

                if (definition is KeyBindingSettingDefinition binding)
                {
                    string key = binding.Action + "|" + binding.CompositePart + "|" + binding.ControlScheme;
                    if (bindingRows.TryGetValue(key, out List<SettingDefinition> slots))
                    {
                        InsertBySlot(slots, binding);
                        continue;
                    }

                    slots = new List<SettingDefinition> { binding };
                    bindingRows[key] = slots;
                    plans.Add(new SettingRowPlan(kind, slots, binding.ControlScheme));
                    continue;
                }

                plans.Add(new SettingRowPlan(kind, new[] { definition }, SchemeOf(definition)));
            }

            return plans;
        }

        /// <summary>Distinct control schemes of the key binding rows in <paramref name="plans"/>, in order of appearance.</summary>
        internal static List<string> BindingSchemes(IReadOnlyList<SettingRowPlan> plans)
        {
            var schemes = new List<string>();
            for (int i = 0; i < plans.Count; i++)
            {
                string scheme = plans[i].Scheme;
                if (plans[i].Kind == SettingRowKind.KeyBinding && scheme.Length > 0 && !Contains(schemes, scheme))
                {
                    schemes.Add(scheme);
                }
            }

            return schemes;
        }

        // Keeps the slots ordered (primary first); equal slots keep catalog order.
        private static void InsertBySlot(List<SettingDefinition> slots, KeyBindingSettingDefinition binding)
        {
            int index = slots.Count;
            while (index > 0 && ((KeyBindingSettingDefinition)slots[index - 1]).Slot > binding.Slot)
            {
                index--;
            }

            slots.Insert(index, binding);
        }

        private static bool Contains(List<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
