using UnityEngine;

namespace Game.Contracts.Interaction
{
    /// <summary>
    /// State of one tracked action at the last sample of <see cref="IInputActivity"/>. Times are
    /// <c>Time.realtimeSinceStartup</c> seconds; -1 means "never".
    /// </summary>
    public readonly struct InputActionActivity
    {
        /// <summary>Creates the state.</summary>
        public InputActionActivity(string action, bool isPressed, Vector2 value, int pressCount, float lastPressedTime,
            float lastReleasedTime, float currentHoldSeconds, float totalHeldSeconds)
        {
            Action = action;
            IsPressed = isPressed;
            Value = value;
            PressCount = pressCount;
            LastPressedTime = lastPressedTime;
            LastReleasedTime = lastReleasedTime;
            CurrentHoldSeconds = currentHoldSeconds;
            TotalHeldSeconds = totalHeldSeconds;
        }

        /// <summary>The action (<c>"Context/Action"</c>); null for an action that is not tracked.</summary>
        public string Action { get; }

        /// <summary>True when the action is tracked by <see cref="IInputActivity"/>.</summary>
        public bool IsTracked => Action != null;

        /// <summary>
        /// Held (a button), or pushed past the activity threshold (a vector: stick, WASD, mouse delta; it stays pressed for
        /// the action's release grace after falling below, so mouse movement does not flicker).
        /// </summary>
        public bool IsPressed { get; }

        /// <summary>Raw value read this frame: (axis, 0) for a button (a trigger reads 0..1), the vector otherwise.</summary>
        public Vector2 Value { get; }

        /// <summary>Presses since the activity started or was cleared.</summary>
        public int PressCount { get; }

        /// <summary>Time of the last press, or -1.</summary>
        public float LastPressedTime { get; }

        /// <summary>Time of the last release, or -1.</summary>
        public float LastReleasedTime { get; }

        /// <summary>Seconds the current press has lasted (0 when released).</summary>
        public float CurrentHoldSeconds { get; }

        /// <summary>Seconds held in total, the current press included.</summary>
        public float TotalHeldSeconds { get; }
    }
}
