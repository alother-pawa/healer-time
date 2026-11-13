using System.Collections.Generic;
using Game.Foundation.Localization;
using Paowa.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// One row of a settings page, on the root of a row prefab (one prefab per <see cref="SettingRowKind"/>). The page binds
    /// it once to its definitions; <see cref="Redraw"/> then writes everything visible from the settings model, so the row
    /// shows the same thing however often it is redrawn and never keeps a stale value. The row's selectables form columns
    /// that the page links for keyboard/gamepad navigation.
    /// REFERENCES: <c>_caption</c> shows the first definition's label key.
    /// </summary>
    internal abstract class SettingRow : MonoBehaviour
    {
        [SerializeField] private LocalizedLabel _caption;

        private readonly List<SettingDefinition> _definitions = new List<SettingDefinition>();

        /// <summary>First definition of the row.</summary>
        internal SettingDefinition Definition => _definitions.Count > 0 ? _definitions[0] : null;

        /// <summary>Every definition of the row (the slots of a key binding row).</summary>
        internal IReadOnlyList<SettingDefinition> Definitions => _definitions;

        /// <summary>Control scheme filtering the row; empty when it shows with every scheme.</summary>
        internal string Scheme { get; private set; } = string.Empty;

        /// <summary>Number of navigable columns.</summary>
        internal abstract int ColumnCount { get; }

        /// <summary>Shared services; null until bound.</summary>
        protected SettingRowContext Context { get; private set; }

        /// <summary>Binds the row to <paramref name="plan"/> and draws it.</summary>
        internal void Bind(SettingRowContext context, SettingRowPlan plan)
        {
            Context = context;
            _definitions.Clear();
            _definitions.AddRange(plan.Definitions);
            Scheme = plan.Scheme;
            if (_caption != null)
            {
                _caption.SetKey(Definition.LabelKey);
            }

            OnBind();
            Redraw();
        }

        /// <summary>Writes the row from the current state.</summary>
        internal abstract void Redraw();

        /// <summary>Selectable of <paramref name="column"/>; null when it has none.</summary>
        internal abstract Selectable GetColumn(int column);

        /// <summary>Definition described while <paramref name="selected"/> (one of the row's selectables) is selected.</summary>
        internal virtual SettingDefinition DefinitionFor(GameObject selected)
        {
            return Definition;
        }

        /// <summary>Column index of <paramref name="selected"/>; -1 when it is not one of the row's selectables.</summary>
        internal int ColumnOf(GameObject selected)
        {
            for (int i = 0; i < ColumnCount; i++)
            {
                Selectable column = GetColumn(i);
                if (column != null && column.gameObject == selected)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Called once the definitions are set, before the first redraw.</summary>
        protected virtual void OnBind()
        {
        }
    }
}
