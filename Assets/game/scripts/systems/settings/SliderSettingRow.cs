using Paowa.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Slider row. The uGUI slider runs on whole positions (<see cref="SliderSteps"/>), so left/right in navigation and a
    /// drag both move one definition step at a time; the slider has no left/right neighbour, so it keeps those presses. The
    /// value text is the definition's own format (e.g. "80%").
    /// REFERENCES: <c>_slider</c> is the row's selectable; <c>_value</c> shows the formatted value.
    /// </summary>
    internal sealed class SliderSettingRow : SettingRow
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _value;

        private SliderSettingDefinition _definition;
        private int _steps = 1;
        private bool _configuring;

        internal override int ColumnCount => 1;

        internal override void Redraw()
        {
            if (Context == null || _definition == null)
            {
                return;
            }

            float value = Context.Settings.GetFloat(_definition.Id);
            _slider.SetValueWithoutNotify(SliderSteps.ToIndex(value, _definition.Min, _definition.Max, _definition.Step, _steps));
            _value.text = _definition.Format(value);
        }

        internal override Selectable GetColumn(int column)
        {
            return column == 0 ? _slider : null;
        }

        protected override void OnBind()
        {
            _definition = Definition as SliderSettingDefinition;
            if (_definition == null)
            {
                return;
            }

            // Changing the range can clamp the slider's value and raise its change event; that is not a player choice.
            _configuring = true;
            try
            {
                _steps = SliderSteps.Count(_definition.Min, _definition.Max, _definition.Step);
                _slider.wholeNumbers = true;
                _slider.minValue = 0f;
                _slider.maxValue = _steps;
            }
            finally
            {
                _configuring = false;
            }
        }

        private void Awake()
        {
            _slider.onValueChanged.AddListener(OnSliderMoved);
        }

        // The change event redraws the row (the model may snap or clamp the value).
        private void OnSliderMoved(float position)
        {
            if (!_configuring && Context != null && _definition != null)
            {
                float value = SliderSteps.ToValue(Mathf.RoundToInt(position), _definition.Min, _definition.Max, _definition.Step, _steps);
                Context.Settings.SetFloat(_definition.Id, value);
            }
        }
    }
}
