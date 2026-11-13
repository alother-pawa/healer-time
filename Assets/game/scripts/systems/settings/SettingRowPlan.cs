using System.Collections.Generic;
using Paowa.Settings;

namespace Game.Systems.Settings
{
    /// <summary>
    /// One row of a settings page: its kind, its definitions (one, or the slots of a key binding in slot order) and the
    /// control scheme it belongs to (empty for rows shown with every scheme).
    /// </summary>
    internal sealed class SettingRowPlan
    {
        internal SettingRowPlan(SettingRowKind kind, IReadOnlyList<SettingDefinition> definitions, string scheme)
        {
            Kind = kind;
            Definitions = definitions;
            Scheme = scheme ?? string.Empty;
        }

        /// <summary>Prefab kind.</summary>
        internal SettingRowKind Kind { get; }

        /// <summary>Definitions drawn by the row; never empty.</summary>
        internal IReadOnlyList<SettingDefinition> Definitions { get; }

        /// <summary>Control scheme filtering the row; empty when the row shows with every scheme.</summary>
        internal string Scheme { get; }
    }
}
