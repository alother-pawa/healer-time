using Paowa.Input;
using UnityEngine;

namespace Game.Contracts.Interaction
{
    /// <summary>One press or release in the history of <see cref="IInputActivity"/>.</summary>
    public readonly struct InputActivityEntry
    {
        /// <summary>Creates the entry.</summary>
        public InputActivityEntry(int sequence, string action, InputActivityPhase phase, InputDeviceInfo device, float time, Vector2 value)
        {
            Sequence = sequence;
            Action = action;
            Phase = phase;
            Device = device;
            Time = time;
            Value = value;
        }

        /// <summary>Running number of the entry since the activity started (never reused, also after a clear).</summary>
        public int Sequence { get; }

        /// <summary>The action (<c>"Context/Action"</c>).</summary>
        public string Action { get; }

        /// <summary>Pressed or released.</summary>
        public InputActivityPhase Phase { get; }

        /// <summary>
        /// Device in use when the entry was sampled (Core's <c>CurrentDeviceInfo</c>: kind and gamepad family). Core does not
        /// report which device drove a given action, so with two devices moving in the same frame this is the latest one.
        /// </summary>
        public InputDeviceInfo Device { get; }

        /// <summary><c>Time.realtimeSinceStartup</c> of the sample.</summary>
        public float Time { get; }

        /// <summary>Value at that sample (see <see cref="InputActionActivity.Value"/>).</summary>
        public Vector2 Value { get; }
    }
}
