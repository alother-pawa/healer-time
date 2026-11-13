using System.Collections.Generic;
using Game.Foundation.Localization;
using Paowa.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Stepper row "◄ value ►" (gamepad-friendly, unlike a dropdown). In navigation, left/right step and stop at the ends;
    /// the arrows do the same for the mouse (an arrow at an end is disabled); Submit or a click on the row steps forward and
    /// wraps (<see cref="ChoiceStepper"/>). Options are read from the model on every redraw (providers change: the refresh
    /// rates follow the staged resolution, the resolutions the monitor). A value outside the options shows as is; an empty
    /// value (an index choice meaning "keep the current setting") shows <c>_keepKey</c>.
    /// REFERENCES: <c>_button</c> is the row's selectable (its root); <c>_previous</c>/<c>_next</c> are the arrows (not
    /// navigable, so focus stays on the row); <c>_value</c> shows the option's label.
    /// </summary>
    internal sealed class ChoiceSettingRow : SettingRow, IMoveHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Button _previous;
        [SerializeField] private Button _next;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private string _keepKey;

        private readonly List<SettingOption> _options = new List<SettingOption>();

        internal override int ColumnCount => 1;

        public void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left)
            {
                Step(-1, false);
                eventData.Use();
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                Step(1, false);
                eventData.Use();
            }
        }

        internal override void Redraw()
        {
            if (Context == null)
            {
                return;
            }

            Context.Settings.GetOptions(Definition.Id, _options);
            string value = Context.Settings.GetString(Definition.Id);
            int index = ChoiceStepper.IndexOf(_options, value);
            _value.text = index >= 0 ? LabelOf(_options[index]) : value.Length == 0 ? Loc.Get(_keepKey) : value;
            _previous.interactable = ChoiceStepper.CanStep(index, _options.Count, -1);
            _next.interactable = ChoiceStepper.CanStep(index, _options.Count, 1);
        }

        internal override Selectable GetColumn(int column)
        {
            return column == 0 ? _button : null;
        }

        private static string LabelOf(SettingOption option)
        {
            return option.LabelIsKey ? Loc.Get(option.Label) : option.Label;
        }

        private void Awake()
        {
            _button.onClick.AddListener(() => Step(1, true));
            _previous.onClick.AddListener(() => Step(-1, false));
            _next.onClick.AddListener(() => Step(1, false));
        }

        // The change event redraws the row.
        private void Step(int delta, bool wrap)
        {
            if (Context == null)
            {
                return;
            }

            Context.Settings.GetOptions(Definition.Id, _options);
            int current = ChoiceStepper.IndexOf(_options, Context.Settings.GetString(Definition.Id));
            int next = ChoiceStepper.Step(current, _options.Count, delta, wrap);
            if (next >= 0 && next != current)
            {
                Context.Settings.SetString(Definition.Id, _options[next].Value);
            }
        }
    }
}
