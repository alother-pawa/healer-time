using Paowa.Input;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Turns the raw value of <c>Gameplay/Look</c> into degrees turned this frame (x = yaw right, y = pitch up), so mouse and
    /// stick feel alike: mouse pixels × degrees per pixel × mouse sensitivity (no frame time: a delta is already a distance);
    /// stick → radial dead zone → response curve → × degrees per second × stick sensitivity × frame time. Invert Y flips the
    /// vertical axis of both. Pure math; the source comes from the value and the device in use (<see cref="SourceOf"/>),
    /// because Core does not report which control drove an action.
    /// </summary>
    internal static class LookNormalizer
    {
        /// <summary>A stick never reads above this magnitude; anything larger is a mouse delta in pixels.</summary>
        internal const float MaxStickMagnitude = 1.001f;

        /// <summary>
        /// Mouse when <paramref name="raw"/> is larger than a stick can read (Core switches the device in use to keyboard and
        /// mouse only after 3 px of movement, so small mouse moves right after the gamepad would otherwise be read as a
        /// stick); otherwise stick for gamepads and other controllers (joysticks), mouse for anything else.
        /// </summary>
        internal static LookSource SourceOf(InputDeviceKind kind, Vector2 raw)
        {
            if (raw.sqrMagnitude > MaxStickMagnitude * MaxStickMagnitude)
            {
                return LookSource.Mouse;
            }

            return kind == InputDeviceKind.Gamepad || kind == InputDeviceKind.Other ? LookSource.Stick : LookSource.Mouse;
        }

        /// <summary>
        /// Degrees for one frame. Sensitivities below zero count as zero; a stick with a zero, negative or invalid frame time
        /// turns nothing (a paused frame).
        /// </summary>
        internal static Vector2 Normalize(Vector2 raw, LookSource source, float deltaTime, LookTuning tuning, float mouseSensitivity,
            float stickSensitivity, bool invertY)
        {
            if (tuning == null || !IsFinite(raw))
            {
                return Vector2.zero;
            }

            Vector2 degrees;
            if (source == LookSource.Mouse)
            {
                degrees = raw * (tuning.MouseDegreesPerPixel * Mathf.Max(0f, mouseSensitivity));
            }
            else
            {
                if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                {
                    return Vector2.zero;
                }

                degrees = ApplyStickCurve(raw, tuning) * (tuning.StickDegreesPerSecond * Mathf.Max(0f, stickSensitivity) * deltaTime);
            }

            if (invertY)
            {
                degrees.y = -degrees.y;
            }

            return degrees;
        }

        /// <summary>Radial dead zone (inner → 0, outer → 1, rescaled between) and the response curve; the direction is kept.</summary>
        internal static Vector2 ApplyStickCurve(Vector2 stick, LookTuning tuning)
        {
            float magnitude = stick.magnitude;
            float inner = tuning.StickInnerDeadZone;
            if (magnitude <= inner || magnitude <= 0f)
            {
                return Vector2.zero;
            }

            float scaled = Mathf.Clamp01((magnitude - inner) / (tuning.StickOuterDeadZone - inner));
            return stick / magnitude * Mathf.Pow(scaled, tuning.StickExponent);
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
        }
    }
}
