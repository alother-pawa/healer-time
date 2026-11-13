using System;
using System.Collections.Generic;
using Paowa.Core.Foundation.Ticking;
using Paowa.Input;
using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>
    /// Feeds <see cref="InputActivityTracker"/> once per frame (Core's frame driver) from <see cref="IInputService"/>, with
    /// realtime seconds, so a paused game still records the press that resumed or paused it. Actions of an inactive or
    /// locked context read as released, exactly like the input service reports them. An action whose read throws (its kind
    /// does not match its value type; the Input System only checks the type once a control drives it) reads as released
    /// that frame, is logged once and is dropped from the tracker after the sample.
    /// </summary>
    internal sealed class InputActivitySampler : ITickable
    {
        private readonly InputActivityTracker _tracker;
        private readonly IInputService _input;
        private readonly List<string> _failed = new List<string>();
        private readonly Func<TrackedAction, ActionSample> _read;

        internal InputActivitySampler(InputActivityTracker tracker, IInputService input)
        {
            _tracker = tracker;
            _input = input;
            _read = SafeRead;
        }

        public void Tick(float deltaTime)
        {
            _tracker.Sample(Time.realtimeSinceStartup, _input.CurrentDeviceInfo, _read);
            if (_failed.Count == 0)
            {
                return;
            }

            foreach (string action in _failed)
            {
                _tracker.Drop(action);
            }

            _failed.Clear();
        }

        /// <summary>Reads one action the way its kind needs (a vector read of a button action throws).</summary>
        internal static ActionSample Read(IInputService input, TrackedAction action)
        {
            if (action.Kind == TrackedActionKind.Vector)
            {
                return ActionSample.Vector(input.ReadVector2(action.Action));
            }

            return new ActionSample(input.IsPressed(action.Action), input.WasPressedThisFrame(action.Action),
                input.WasReleasedThisFrame(action.Action), new Vector2(input.ReadAxis(action.Action), 0f));
        }

        private ActionSample SafeRead(TrackedAction action)
        {
            try
            {
                return Read(_input, action);
            }
            catch (Exception exception)
            {
                _failed.Add(action.Action);
                Debug.LogError($"[Game] Input activity: action '{action.Action}' cannot be read as {action.Kind} ({exception.Message}); "
                    + "it is no longer tracked. Run Game › Build All.");
                return default;
            }
        }
    }
}
