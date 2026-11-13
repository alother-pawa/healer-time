using System;
using System.Collections.Generic;
using Game.Contracts.Interaction;
using Paowa.Core.Messaging;
using Paowa.Input;
using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>
    /// Default <see cref="IInputActivity"/>, fed one sample per tracked action per frame by <see cref="Sample"/> (pure logic,
    /// no Unity input). A button is pressed while held; a press and release inside one frame still counts and is recorded
    /// as both. A vector is pressed from <c>vectorThreshold</c>; it is released once it stayed below for the action's grace
    /// (mouse delta reads zero on frames without a mouse event). Every press and release updates the state first, then goes
    /// into the history ring buffer and out as <see cref="InputActivityRecordedEvent"/>.
    /// </summary>
    internal sealed class InputActivityTracker : IInputActivity
    {
        private const float Never = -1f;

        private readonly List<TrackedAction> _tracked = new List<TrackedAction>();
        private readonly List<string> _names = new List<string>();
        private readonly Dictionary<string, Slot> _slots = new Dictionary<string, Slot>(StringComparer.Ordinal);
        private readonly InputActivityEntry[] _history;
        private readonly float _vectorThreshold;
        private readonly IEventBus _events;
        private int _head;
        private int _count;
        private int _sequence;
        private float _now;

        /// <param name="actions">Tracked actions; empty or duplicate names are skipped.</param>
        /// <param name="historyCapacity">Ring buffer size (at least 1).</param>
        /// <param name="vectorThreshold">Magnitude from which a vector is pressed (at least a small positive value).</param>
        /// <param name="events">Bus for <see cref="InputActivityRecordedEvent"/>; null publishes nothing.</param>
        internal InputActivityTracker(IEnumerable<TrackedAction> actions, int historyCapacity, float vectorThreshold, IEventBus events)
        {
            foreach (TrackedAction action in actions)
            {
                if (action == null || string.IsNullOrEmpty(action.Action) || _slots.ContainsKey(action.Action))
                {
                    continue;
                }

                _tracked.Add(action);
                _names.Add(action.Action);
                _slots.Add(action.Action, new Slot());
            }

            _history = new InputActivityEntry[Math.Max(1, historyCapacity)];
            _vectorThreshold = Math.Max(0.0001f, vectorThreshold);
            _events = events;
        }

        public IReadOnlyList<string> Actions => _names;

        public int HistoryCount => _count;

        public int HistoryCapacity => _history.Length;

        public int Version { get; private set; }

        public InputActionActivity GetState(string action)
        {
            if (action == null || !_slots.TryGetValue(action, out Slot slot))
            {
                return default;
            }

            float current = slot.Pressed ? Math.Max(0f, _now - slot.PressedSince) : 0f;
            return new InputActionActivity(action, slot.Pressed, slot.Value, slot.PressCount, slot.LastPressed, slot.LastReleased,
                current, slot.HeldBefore + current);
        }

        public InputActivityEntry GetHistory(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"History holds {_count} entries.");
            }

            int position = (_head - 1 - index + _history.Length * 2) % _history.Length;
            return _history[position];
        }

        public void Clear()
        {
            Array.Clear(_history, 0, _history.Length);
            _head = 0;
            _count = 0;
            foreach (Slot slot in _slots.Values)
            {
                slot.PressCount = 0;
                slot.HeldBefore = 0f;
                slot.LastPressed = Never;
                slot.LastReleased = Never;
                if (slot.Pressed)
                {
                    slot.PressedSince = _now;
                }
            }

            Version++;
        }

        /// <summary>
        /// Samples every tracked action at <paramref name="time"/> (realtime seconds) with <paramref name="device"/> as the
        /// device in use; <paramref name="read"/> returns what the input service reports for an action this frame.
        /// </summary>
        /// <summary>
        /// Stops tracking <paramref name="action"/> (e.g. it cannot be read); its state reads as untracked afterwards. Not
        /// called from inside <see cref="Sample"/>.
        /// </summary>
        internal void Drop(string action)
        {
            if (action == null || !_slots.Remove(action))
            {
                return;
            }

            _tracked.RemoveAll(tracked => tracked.Action == action);
            _names.Remove(action);
            Version++;
        }

        internal void Sample(float time, InputDeviceInfo device, Func<TrackedAction, ActionSample> read)
        {
            _now = time;
            foreach (TrackedAction action in _tracked)
            {
                Slot slot = _slots[action.Action];
                ActionSample sample = read(action);
                slot.Value = sample.Value;
                if (action.Kind == TrackedActionKind.Vector)
                {
                    SampleVector(action, slot, sample.Value, time, device);
                }
                else
                {
                    SampleButton(action, slot, sample, time, device);
                }
            }
        }

        private void SampleButton(TrackedAction action, Slot slot, ActionSample sample, float time, InputDeviceInfo device)
        {
            // Released and pressed again between two samples: record the release first.
            if (slot.Pressed && sample.WasReleasedThisFrame && sample.WasPressedThisFrame)
            {
                Release(action, slot, time, device);
            }

            if (!slot.Pressed && (sample.IsPressed || sample.WasPressedThisFrame))
            {
                Press(action, slot, time, device);
            }

            if (slot.Pressed && !sample.IsPressed)
            {
                Release(action, slot, time, device);
            }
        }

        private void SampleVector(TrackedAction action, Slot slot, Vector2 value, float time, InputDeviceInfo device)
        {
            bool active = value.sqrMagnitude >= _vectorThreshold * _vectorThreshold;
            if (active)
            {
                slot.BelowSince = Never;
                if (!slot.Pressed)
                {
                    Press(action, slot, time, device);
                }

                return;
            }

            if (!slot.Pressed)
            {
                return;
            }

            if (slot.BelowSince < 0f)
            {
                slot.BelowSince = time;
            }

            if (time - slot.BelowSince >= action.ReleaseGraceSeconds)
            {
                Release(action, slot, time, device);
            }
        }

        private void Press(TrackedAction action, Slot slot, float time, InputDeviceInfo device)
        {
            slot.Pressed = true;
            slot.PressedSince = time;
            slot.BelowSince = Never;
            slot.PressCount++;
            slot.LastPressed = time;
            Record(action.Action, InputActivityPhase.Pressed, device, time, slot.Value);
        }

        private void Release(TrackedAction action, Slot slot, float time, InputDeviceInfo device)
        {
            slot.Pressed = false;
            slot.HeldBefore += Math.Max(0f, time - slot.PressedSince);
            slot.BelowSince = Never;
            slot.LastReleased = time;
            Record(action.Action, InputActivityPhase.Released, device, time, slot.Value);
        }

        private void Record(string action, InputActivityPhase phase, InputDeviceInfo device, float time, Vector2 value)
        {
            var entry = new InputActivityEntry(++_sequence, action, phase, device, time, value);
            _history[_head] = entry;
            _head = (_head + 1) % _history.Length;
            _count = Math.Min(_count + 1, _history.Length);
            Version++;
            if (_events != null)
            {
                _events.Publish(new InputActivityRecordedEvent(entry));
            }
        }

        private sealed class Slot
        {
            internal bool Pressed;
            internal Vector2 Value;
            internal int PressCount;
            internal float PressedSince;
            internal float HeldBefore;
            internal float BelowSince = Never;
            internal float LastPressed = Never;
            internal float LastReleased = Never;
        }
    }
}
