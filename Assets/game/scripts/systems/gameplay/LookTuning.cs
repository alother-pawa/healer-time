using System;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Base speeds of <c>Gameplay/Look</c> before the player's sensitivity (part of <see cref="GameplayConfig"/>). Mouse delta
    /// is a distance per frame (pixels), so it is scaled per pixel and never multiplied by the frame time; a stick is a rate
    /// (−1..1), so it is scaled per second and multiplied by the frame time. The stick dead zone here comes on top of the
    /// Input System's own stick dead zone (the gamepad layout already remaps about 0.125–0.925 to 0–1).
    /// </summary>
    [Serializable]
    internal sealed class LookTuning
    {
        [Tooltip("Degrees turned per pixel of mouse movement at sensitivity 1.")]
        [SerializeField, Min(0f)] private float _mouseDegreesPerPixel = 0.1f;
        [Tooltip("Degrees per second at full stick deflection and sensitivity 1.")]
        [SerializeField, Min(0f)] private float _stickDegreesPerSecond = 180f;
        [Tooltip("Stick magnitude below which look is zero (radial, on top of the Input System dead zone).")]
        [SerializeField, Range(0f, 0.9f)] private float _stickInnerDeadZone = 0.05f;
        [Tooltip("Stick magnitude that already counts as full deflection.")]
        [SerializeField, Range(0.1f, 1f)] private float _stickOuterDeadZone = 1f;
        [Tooltip("Response curve of the stick: 1 = linear, above 1 = finer control near the centre.")]
        [SerializeField, Range(1f, 3f)] private float _stickExponent = 1.5f;

        /// <summary>Creates the defaults; the overload sets every value (tests, code-built tuning).</summary>
        internal LookTuning()
        {
        }

        internal LookTuning(float mouseDegreesPerPixel, float stickDegreesPerSecond, float stickInnerDeadZone, float stickOuterDeadZone,
            float stickExponent)
        {
            _mouseDegreesPerPixel = mouseDegreesPerPixel;
            _stickDegreesPerSecond = stickDegreesPerSecond;
            _stickInnerDeadZone = stickInnerDeadZone;
            _stickOuterDeadZone = stickOuterDeadZone;
            _stickExponent = stickExponent;
        }

        internal float MouseDegreesPerPixel => Mathf.Max(0f, _mouseDegreesPerPixel);

        internal float StickDegreesPerSecond => Mathf.Max(0f, _stickDegreesPerSecond);

        internal float StickInnerDeadZone => Mathf.Clamp(_stickInnerDeadZone, 0f, 0.95f);

        /// <summary>Outer dead zone, always above the inner one (1 when misconfigured).</summary>
        internal float StickOuterDeadZone => _stickOuterDeadZone > StickInnerDeadZone ? Mathf.Min(1f, _stickOuterDeadZone) : 1f;

        internal float StickExponent => Mathf.Max(1f, _stickExponent);
    }
}
