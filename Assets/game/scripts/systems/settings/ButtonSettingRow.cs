using Paowa.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Action row (e.g. "Reset keyboard bindings"): Submit or a click hands the button definition to the popup, which asks
    /// for confirmation and then runs it through <c>ISettingsService.Invoke</c>.
    /// REFERENCES: <c>_button</c> is the row's selectable (its root).
    /// </summary>
    internal sealed class ButtonSettingRow : SettingRow
    {
        [SerializeField] private Button _button;

        internal override int ColumnCount => 1;

        internal override void Redraw()
        {
        }

        internal override Selectable GetColumn(int column)
        {
            return column == 0 ? _button : null;
        }

        private void Awake()
        {
            _button.onClick.AddListener(() =>
            {
                if (Context != null && Definition is ButtonSettingDefinition button)
                {
                    Context.Invoke(button);
                }
            });
        }
    }
}
