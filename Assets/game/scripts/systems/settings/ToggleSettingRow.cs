using Game.Foundation.Localization;
using Paowa.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// On/off row: Submit or a click flips the value; in navigation, left sets off and right sets on. The value text is the
    /// on or off key, written from the model on every redraw.
    /// REFERENCES: <c>_button</c> is the row's selectable (its root); <c>_value</c> shows <c>_onKey</c> or <c>_offKey</c>.
    /// </summary>
    internal sealed class ToggleSettingRow : SettingRow, IMoveHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private LocalizedLabel _value;
        [SerializeField] private string _onKey;
        [SerializeField] private string _offKey;

        internal override int ColumnCount => 1;

        public void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left)
            {
                Set(false);
                eventData.Use();
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                Set(true);
                eventData.Use();
            }
        }

        internal override void Redraw()
        {
            if (Context != null)
            {
                _value.SetKey(Context.Settings.GetBool(Definition.Id) ? _onKey : _offKey);
            }
        }

        internal override Selectable GetColumn(int column)
        {
            return column == 0 ? _button : null;
        }

        private void Awake()
        {
            _button.onClick.AddListener(() =>
            {
                if (Context != null)
                {
                    Set(!Context.Settings.GetBool(Definition.Id));
                }
            });
        }

        // The change event redraws the row.
        private void Set(bool value)
        {
            if (Context != null)
            {
                Context.Settings.SetBool(Definition.Id, value);
            }
        }
    }
}
