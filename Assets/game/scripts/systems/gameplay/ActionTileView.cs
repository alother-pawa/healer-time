using DG.Tweening;
using Game.Contracts.Interaction;
using Game.Contracts.Presentation;
using Game.Contracts.Settings;
using Game.Foundation.Localization;
using Game.Foundation.Tweening;
using Paowa.Core.Foundation.Services;
using Paowa.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One tile of the input demo, drawn every frame from <see cref="IInputActivity"/>'s state of <c>_action</c>: lit while the
    /// action is pressed, the value bar at the action's axis (a trigger reads 0–1), the press count, and for a vector action
    /// the knob at the value and the direction arrows past <c>_arrowThreshold</c>. The lit state and every value come from
    /// the state only; a new press adds a short punch of the tile (none with Reduce flashing; unscaled time, gated by the animation switch, always
    /// back to scale 1), so the tile looks the same with animations on or off apart from that pulse.
    /// REFERENCES: <c>_lit</c> is the highlight shown while pressed; <c>_pulseTarget</c> is scaled by the pulse;
    /// <c>_valueFill</c> (optional) is a Filled image; <c>_pressCount</c> formats <c>_pressCountKey</c>; <c>_knob</c>
    /// (optional) moves within <c>_knobRadius</c>; <c>_arrows</c> (optional) are up, down, left, right; <c>_partsPrompt</c>
    /// (optional) shows the composite parts (WASD) on keyboard and mouse, where <c>_prompt</c> then hides.
    /// </summary>
    internal sealed class ActionTileView : MonoBehaviour
    {
        private const int Up = 0;
        private const int Down = 1;
        private const int Left = 2;
        private const int Right = 3;

        [SerializeField] private string _action;
        [SerializeField] private GameObject _lit;
        [SerializeField] private RectTransform _pulseTarget;
        [SerializeField] private Image _valueFill;
        [SerializeField] private LocalizedLabel _pressCount;
        [SerializeField] private string _pressCountKey;
        [SerializeField] private RectTransform _knob;
        [SerializeField] private float _knobRadius = 60f;
        [SerializeField] private GameObject[] _arrows = new GameObject[0];
        [SerializeField, Range(0.1f, 1f)] private float _arrowThreshold = 0.5f;
        [SerializeField] private GameObject _prompt;
        [SerializeField] private GameObject _partsPrompt;
        [SerializeField] private float _pulseScale = 0.08f;
        [SerializeField] private float _pulseSeconds = 0.18f;

        private IInputActivity _activity;
        private IInputService _input;
        private IAnimationSettings _animations;
        private IGameSettings _settings;
        private Tween _pulse;
        private int _shownCount = -1;
        private bool _seenCount;

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _activity);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _animations);
            CoreServiceLocator.TryGet(out _settings);
        }

        private void OnEnable()
        {
            // The count shown on entry is the current one: entering gameplay never pulses.
            _seenCount = false;
            _shownCount = -1;
            Draw();
        }

        private void OnDisable()
        {
            _pulse.KillIfActive(true);
            _pulse = null;
            if (_pulseTarget != null)
            {
                _pulseTarget.localScale = Vector3.one;
            }
        }

        private void LateUpdate()
        {
            Draw();
        }

        private void Draw()
        {
            InputActionActivity state = _activity != null ? _activity.GetState(_action) : default;
            Vector2 value = state.Value;
            _lit.SetActive(state.IsPressed);

            if (_valueFill != null)
            {
                _valueFill.fillAmount = Mathf.Clamp01(Mathf.Abs(value.x));
            }

            if (_knob != null)
            {
                _knob.anchoredPosition = Vector2.ClampMagnitude(value, 1f) * _knobRadius;
            }

            if (_arrows.Length == 4)
            {
                SetArrow(Up, value.y >= _arrowThreshold);
                SetArrow(Down, value.y <= -_arrowThreshold);
                SetArrow(Left, value.x <= -_arrowThreshold);
                SetArrow(Right, value.x >= _arrowThreshold);
            }

            if (_partsPrompt != null)
            {
                bool keyboard = _input == null || !(_input.CurrentDeviceInfo.IsGamepad || _input.CurrentDeviceInfo.Kind == InputDeviceKind.Other);
                _partsPrompt.SetActive(keyboard);
                if (_prompt != null)
                {
                    _prompt.SetActive(!keyboard);
                }
            }

            int count = state.PressCount;
            if (count != _shownCount)
            {
                if (_seenCount && count > _shownCount)
                {
                    Pulse();
                }

                _shownCount = count;
                _seenCount = true;
                if (_pressCount != null)
                {
                    _pressCount.SetFormat(_pressCountKey, count);
                }
            }
        }

        private void SetArrow(int index, bool on)
        {
            if (_arrows[index] != null)
            {
                _arrows[index].SetActive(on);
            }
        }

        private void Pulse()
        {
            // Reduce flashing drops the pulse; the lit state (drawn from input every frame) is unchanged.
            if (_pulseTarget == null || _pulseSeconds <= 0f || (_settings != null && _settings.ReduceFlashing))
            {
                return;
            }

            _pulse.KillIfActive(true);
            _pulseTarget.localScale = Vector3.one;
            _pulse = _pulseTarget.DOPunchScale(Vector3.one * _pulseScale, _pulseSeconds, 1, 0f)
                .SetUpdate(true)
                .SetLink(gameObject)
                .Gate(_animations);
        }
    }
}
