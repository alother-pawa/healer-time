using Game.Contracts.Interaction;
using Game.Contracts.Settings;
using Paowa.Core.Foundation.Services;
using Paowa.Input;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The Look tile's crosshair: every frame the raw <c>_action</c> value (from <see cref="IInputActivity"/>) goes through
    /// <see cref="LookNormalizer"/> with the player's sensitivity and invert Y (<see cref="IGameSettings"/>) and the config's
    /// <see cref="LookTuning"/>, and the resulting degrees turn a demo view clamped to ±<c>_range</c> degrees; the crosshair
    /// sits at that angle inside <c>_area</c>. The angle is state (no tween): it resets when the view is enabled and stays
    /// still while paused (the Gameplay context is locked and the frame time is zero).
    /// REFERENCES: <c>_crosshair</c> moves inside <c>_area</c>; <c>_config</c> holds the look tuning.
    /// </summary>
    internal sealed class LookCrosshairView : MonoBehaviour
    {
        [SerializeField] private string _action = "Gameplay/Look";
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private RectTransform _area;
        [SerializeField] private RectTransform _crosshair;
        [SerializeField] private Vector2 _range = new Vector2(45f, 30f);

        private IInputActivity _activity;
        private IInputService _input;
        private IGameSettings _settings;
        private Vector2 _angle;

        /// <summary>Current demo view angle in degrees (x = yaw, y = pitch).</summary>
        internal Vector2 Angle => _angle;

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _activity);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _settings);
        }

        private void OnEnable()
        {
            _angle = Vector2.zero;
            Draw();
        }

        private void LateUpdate()
        {
            if (_activity != null && _config != null)
            {
                Vector2 raw = _activity.GetState(_action).Value;
                LookSource source = LookNormalizer.SourceOf(_input != null ? _input.CurrentDeviceInfo.Kind : InputDeviceKind.KeyboardMouse, raw);
                Vector2 degrees = LookNormalizer.Normalize(raw, source, Time.deltaTime, _config.Look,
                    _settings != null ? _settings.MouseSensitivity : 1f, _settings != null ? _settings.StickSensitivity : 1f,
                    _settings != null && _settings.InvertY);
                _angle = Clamp(_angle + degrees, _range);
            }

            Draw();
        }

        /// <summary>Adds <paramref name="angle"/> within ±<paramref name="range"/> on each axis.</summary>
        internal static Vector2 Clamp(Vector2 angle, Vector2 range)
        {
            return new Vector2(Mathf.Clamp(angle.x, -range.x, range.x), Mathf.Clamp(angle.y, -range.y, range.y));
        }

        private void Draw()
        {
            Vector2 half = _area.rect.size * 0.5f;
            _crosshair.anchoredPosition = new Vector2(
                _range.x > 0f ? _angle.x / _range.x * half.x : 0f,
                _range.y > 0f ? _angle.y / _range.y * half.y : 0f);
        }
    }
}
