using System.Globalization;
using System.Text;
using Game.Contracts.Interaction;
using Game.Foundation.Localization;
using Paowa.Core.Foundation.Services;
using Paowa.Gamepad;
using Paowa.Input;
using Paowa.UI.Navigation;
using TMPro;
using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>
    /// Development overlay of the input activity: the device, control scheme and interaction mode in use and the last
    /// presses/releases (newest first). Exists only in development builds and the Editor (the object deactivates itself in
    /// release builds). <c>_toggleAction</c> shows or hides the panel; it is read through Core, so it works while its context
    /// is active (the gameplay screen requests it in development builds; the pause locks it). The panel is redrawn only when
    /// the history or the device state changes. Entry lines are developer text (action names, raw values) and are not
    /// localized; the status line is.
    /// REFERENCES: <c>_panel</c> is the toggled panel; <c>_status</c> formats <c>_statusKey</c> (device, scheme, mode);
    /// <c>_history</c> lists up to <c>_lines</c> entries or <c>_emptyKey</c>.
    /// </summary>
    internal sealed class InputActivityOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private LocalizedLabel _status;
        [SerializeField] private string _statusKey;
        [SerializeField] private TextMeshProUGUI _history;
        [SerializeField] private string _emptyKey;
        [SerializeField] private string _toggleAction = "Debug/ToggleOverlay";
        [SerializeField, Range(1, 64)] private int _lines = 12;

        private readonly StringBuilder _text = new StringBuilder();
        private IInputActivity _activity;
        private IInputService _input;
        private IInteractionModeService _modes;
        private IInputGlyphs _glyphs;
        private bool _toggleValid;
        private int _shownVersion = -1;
        private InputDeviceInfo _shownDevice;
        private InteractionMode _shownMode;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            CoreServiceLocator.TryGet(out _activity);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _modes);
            CoreServiceLocator.TryGet(out _glyphs);
            _toggleValid = _input != null && _input.HasAction(_toggleAction);
        }

        private void OnEnable()
        {
            _shownVersion = -1;
            Redraw();
        }

        private void Update()
        {
            if (_toggleValid && _input.WasPressedThisFrame(_toggleAction))
            {
                _panel.SetActive(!_panel.activeSelf);
                _shownVersion = -1;
            }

            Redraw();
        }

        private void Redraw()
        {
            if (_panel == null || !_panel.activeSelf)
            {
                return;
            }

            InputDeviceInfo device = _input != null ? _input.CurrentDeviceInfo : default;
            InteractionMode mode = _modes != null ? _modes.Mode : InteractionMode.Pointer;
            int version = _activity != null ? _activity.Version : 0;
            if (version == _shownVersion && device.HasSameFamily(_shownDevice) && device.DeviceId == _shownDevice.DeviceId && mode == _shownMode)
            {
                return;
            }

            _shownVersion = version;
            _shownDevice = device;
            _shownMode = mode;
            _status.SetFormat(_statusKey, DescribeDevice(device), _glyphs != null ? _glyphs.CurrentControlScheme : "-", mode.ToString());

            _text.Clear();
            int count = _activity != null ? Mathf.Min(_activity.HistoryCount, _lines) : 0;
            for (int i = 0; i < count; i++)
            {
                AppendEntry(_text, _activity.GetHistory(i));
                _text.Append('\n');
            }

            _history.text = count > 0 ? _text.ToString() : Loc.Get(_emptyKey);
        }

        /// <summary>"Gamepad/PlayStation" or the device kind.</summary>
        internal static string DescribeDevice(InputDeviceInfo device)
        {
            return device.IsGamepad ? $"{device.Kind}/{device.GamepadFamily}" : device.Kind.ToString();
        }

        /// <summary>One history line: time, + (pressed) or - (released), action, device, value x,y (invariant culture).</summary>
        internal static void AppendEntry(StringBuilder text, InputActivityEntry entry)
        {
            text.Append(entry.Time.ToString("0.00", CultureInfo.InvariantCulture)).Append(' ')
                .Append(entry.Phase == InputActivityPhase.Pressed ? '+' : '-').Append(' ')
                .Append(entry.Action).Append(' ')
                .Append(DescribeDevice(entry.Device)).Append(' ')
                .Append(entry.Value.x.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                .Append(entry.Value.y.ToString("0.00", CultureInfo.InvariantCulture));
        }
    }
}
