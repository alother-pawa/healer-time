using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>What the input service reported for one action in one frame.</summary>
    internal readonly struct ActionSample
    {
        internal ActionSample(bool isPressed, bool wasPressedThisFrame, bool wasReleasedThisFrame, Vector2 value)
        {
            IsPressed = isPressed;
            WasPressedThisFrame = wasPressedThisFrame;
            WasReleasedThisFrame = wasReleasedThisFrame;
            Value = value;
        }

        /// <summary>Button held (unused for vector actions).</summary>
        internal bool IsPressed { get; }

        /// <summary>Button went down this frame (catches a press and release inside one frame).</summary>
        internal bool WasPressedThisFrame { get; }

        /// <summary>Button went up this frame.</summary>
        internal bool WasReleasedThisFrame { get; }

        /// <summary>(axis, 0) for a button, the vector otherwise.</summary>
        internal Vector2 Value { get; }

        /// <summary>A vector sample.</summary>
        internal static ActionSample Vector(Vector2 value)
        {
            return new ActionSample(false, false, false, value);
        }
    }
}
