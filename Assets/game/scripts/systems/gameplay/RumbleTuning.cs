using System;
using Paowa.Gamepad;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One controller rumble played through the gamepad package's <c>IRumbleService</c> (part of <see cref="GameplayConfig"/>):
    /// motor speeds before the player's vibration intensity, and duration. The service applies the vibration setting,
    /// intensity, pause, focus loss and the device in use.
    /// </summary>
    [Serializable]
    internal sealed class RumbleTuning
    {
        [Tooltip("Low-frequency (left, heavy) motor speed, 0–1, before the player's vibration intensity.")]
        [SerializeField, Range(0f, 1f)] private float _lowFrequency = 0.35f;
        [Tooltip("High-frequency (right, light) motor speed, 0–1, before the player's vibration intensity.")]
        [SerializeField, Range(0f, 1f)] private float _highFrequency = 0.7f;
        [Tooltip("Seconds the rumble lasts (unscaled time).")]
        [SerializeField, Min(0f)] private float _durationSeconds = 0.12f;

        internal RumbleTuning()
        {
        }

        internal RumbleTuning(float lowFrequency, float highFrequency, float durationSeconds)
        {
            _lowFrequency = lowFrequency;
            _highFrequency = highFrequency;
            _durationSeconds = durationSeconds;
        }

        internal float LowFrequency => Mathf.Clamp01(_lowFrequency);

        internal float HighFrequency => Mathf.Clamp01(_highFrequency);

        internal float DurationSeconds => Mathf.Max(0f, _durationSeconds);
    }
}
