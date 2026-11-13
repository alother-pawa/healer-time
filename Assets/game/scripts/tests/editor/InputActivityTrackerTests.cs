using System.Collections.Generic;
using Game.Contracts.Interaction;
using Game.Systems.InputActivity;
using NUnit.Framework;
using Paowa.Input;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// The input activity's state and history from scripted samples: a button press/hold/release, a tap inside one frame,
    /// a release and press between two samples, a vector past the threshold with and without release grace, press counts
    /// and hold times, the ring buffer dropping the oldest entry (newest first), the device stored per entry, the event
    /// published after the state changed, untracked actions, duplicate entries skipped, a dropped action, and Clear.
    /// </summary>
    public sealed class InputActivityTrackerTests
    {
        private const string Jump = "Gameplay/Jump";
        private const string Move = "Gameplay/Move";
        private const string Look = "Gameplay/Look";

        private static readonly InputDeviceInfo Keyboard = new InputDeviceInfo(InputDeviceKind.KeyboardMouse);
        private static readonly InputDeviceInfo Pad = new InputDeviceInfo(InputDeviceKind.Gamepad, GamepadFamily.PlayStation);

        private readonly Dictionary<string, ActionSample> _samples = new Dictionary<string, ActionSample>();
        private readonly List<InputActivityEntry> _published = new List<InputActivityEntry>();
        private TestEventBus _bus;
        private InputActivityTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _samples.Clear();
            _published.Clear();
            _bus = new TestEventBus();
            _tracker = Create(32);
        }

        [Test]
        public void AButtonIsPressedWhileHeldAndRecordedOnce()
        {
            Button(Jump, true, true, false);
            Sample(1f, Keyboard);
            Button(Jump, true, false, false);
            Sample(1.5f, Keyboard);

            InputActionActivity state = _tracker.GetState(Jump);
            Assert.That(state.IsTracked, Is.True);
            Assert.That(state.IsPressed, Is.True);
            Assert.That(state.PressCount, Is.EqualTo(1));
            Assert.That(state.LastPressedTime, Is.EqualTo(1f));
            Assert.That(state.CurrentHoldSeconds, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(_tracker.HistoryCount, Is.EqualTo(1));

            Button(Jump, false, false, true);
            Sample(2f, Keyboard);

            state = _tracker.GetState(Jump);
            Assert.That(state.IsPressed, Is.False);
            Assert.That(state.LastReleasedTime, Is.EqualTo(2f));
            Assert.That(state.CurrentHoldSeconds, Is.EqualTo(0f));
            Assert.That(state.TotalHeldSeconds, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(_tracker.GetHistory(0).Phase, Is.EqualTo(InputActivityPhase.Released));
            Assert.That(_tracker.GetHistory(1).Phase, Is.EqualTo(InputActivityPhase.Pressed));
        }

        [Test]
        public void ATapInsideOneFrameCountsAsPressAndRelease()
        {
            Button(Jump, false, true, true);
            Sample(1f, Keyboard);

            Assert.That(_tracker.GetState(Jump).IsPressed, Is.False);
            Assert.That(_tracker.GetState(Jump).PressCount, Is.EqualTo(1));
            Assert.That(_tracker.HistoryCount, Is.EqualTo(2));
        }

        [Test]
        public void AReleaseAndPressBetweenTwoSamplesIsRecordedAsBoth()
        {
            Button(Jump, true, true, false);
            Sample(1f, Keyboard);
            Button(Jump, true, true, true);
            Sample(1.1f, Keyboard);

            Assert.That(_tracker.GetState(Jump).PressCount, Is.EqualTo(2));
            Assert.That(_tracker.GetState(Jump).IsPressed, Is.True);
            Assert.That(_tracker.HistoryCount, Is.EqualTo(3));
        }

        [Test]
        public void AVectorIsPressedFromTheThresholdAndReleasedAtOnceWithoutGrace()
        {
            Vector(Move, new Vector2(0.1f, 0f));
            Sample(1f, Pad);
            Assert.That(_tracker.GetState(Move).IsPressed, Is.False, "Below the 0.2 threshold.");
            Assert.That(_tracker.GetState(Move).Value, Is.EqualTo(new Vector2(0.1f, 0f)), "The raw value is kept anyway.");

            Vector(Move, new Vector2(0f, 0.9f));
            Sample(1.1f, Pad);
            Assert.That(_tracker.GetState(Move).IsPressed, Is.True);

            Vector(Move, Vector2.zero);
            Sample(1.2f, Pad);
            Assert.That(_tracker.GetState(Move).IsPressed, Is.False);
            Assert.That(_tracker.GetState(Move).PressCount, Is.EqualTo(1));
        }

        [Test]
        public void MouseLookStaysPressedForItsGraceSoFramesWithoutMouseEventsDoNotFlicker()
        {
            Vector(Look, new Vector2(5f, 0f));
            Sample(1f, Keyboard);
            Vector(Look, Vector2.zero);
            Sample(1.05f, Keyboard);
            Assert.That(_tracker.GetState(Look).IsPressed, Is.True, "Inside the 0.15 s grace.");

            Vector(Look, new Vector2(3f, 1f));
            Sample(1.1f, Keyboard);
            Vector(Look, Vector2.zero);
            Sample(1.2f, Keyboard);
            Assert.That(_tracker.GetState(Look).IsPressed, Is.True, "The grace restarted with the new movement.");

            Sample(1.3f, Keyboard);
            Assert.That(_tracker.GetState(Look).IsPressed, Is.True, "0.1 s below: still inside the grace.");
            Sample(1.4f, Keyboard);
            Assert.That(_tracker.GetState(Look).IsPressed, Is.False);
            Assert.That(_tracker.GetState(Look).PressCount, Is.EqualTo(1), "One continuous movement is one press.");
        }

        [Test]
        public void TheHistoryKeepsTheNewestEntriesFirstAndDropsTheOldest()
        {
            _tracker = Create(3);
            for (int i = 0; i < 3; i++)
            {
                Button(Jump, true, true, false);
                Sample(i, i == 2 ? Pad : Keyboard);
                Button(Jump, false, false, true);
                Sample(i + 0.5f, i == 2 ? Pad : Keyboard);
            }

            Assert.That(_tracker.HistoryCount, Is.EqualTo(3));
            Assert.That(_tracker.HistoryCapacity, Is.EqualTo(3));
            Assert.That(_tracker.GetHistory(0).Time, Is.EqualTo(2.5f));
            Assert.That(_tracker.GetHistory(0).Phase, Is.EqualTo(InputActivityPhase.Released));
            Assert.That(_tracker.GetHistory(1).Time, Is.EqualTo(2f));
            Assert.That(_tracker.GetHistory(2).Time, Is.EqualTo(1.5f));
            Assert.That(_tracker.GetHistory(0).Device.GamepadFamily, Is.EqualTo(GamepadFamily.PlayStation));
            Assert.That(_tracker.GetHistory(2).Device.Kind, Is.EqualTo(InputDeviceKind.KeyboardMouse));
            Assert.That(_tracker.GetHistory(0).Sequence, Is.EqualTo(6));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _tracker.GetHistory(3));
        }

        [Test]
        public void EveryEntryIsPublishedAfterTheStateChanged()
        {
            bool pressedWhenPublished = false;
            _bus.Subscribe<InputActivityRecordedEvent>(evt =>
            {
                _published.Add(evt.Entry);
                pressedWhenPublished = _tracker.GetState(Jump).IsPressed;
            });
            int version = _tracker.Version;

            Button(Jump, true, true, false);
            Sample(1f, Pad);

            Assert.That(_published, Has.Count.EqualTo(1));
            Assert.That(_published[0].Action, Is.EqualTo(Jump));
            Assert.That(_published[0].Device.Kind, Is.EqualTo(InputDeviceKind.Gamepad));
            Assert.That(pressedWhenPublished, Is.True);
            Assert.That(_tracker.Version, Is.GreaterThan(version));
        }

        [Test]
        public void UntrackedActionsReadAsUntrackedAndDuplicatesAreSkipped()
        {
            var tracker = new InputActivityTracker(new[]
            {
                new TrackedAction(Jump, TrackedActionKind.Button, 0f), new TrackedAction(Jump, TrackedActionKind.Vector, 0f),
                new TrackedAction(string.Empty, TrackedActionKind.Button, 0f), null,
            }, 4, 0.2f, null);

            Assert.That(tracker.Actions, Is.EqualTo(new[] { Jump }));
            Assert.That(tracker.GetState("Menu/Pause").IsTracked, Is.False);
            Assert.That(tracker.GetState(null).IsTracked, Is.False);
        }

        [Test]
        public void ADroppedActionIsNoLongerTrackedOrSampled()
        {
            _tracker.Drop(Jump);
            Button(Jump, true, true, false);
            Sample(1f, Keyboard);

            Assert.That(_tracker.GetState(Jump).IsTracked, Is.False);
            Assert.That(_tracker.Actions, Is.EqualTo(new[] { Move, Look }));
            Assert.That(_tracker.HistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void ClearEmptiesTheHistoryAndCountsButKeepsAHeldAction()
        {
            Button(Jump, true, true, false);
            Sample(1f, Keyboard);
            Button(Jump, true, false, false);
            Sample(2f, Keyboard);

            _tracker.Clear();

            InputActionActivity state = _tracker.GetState(Jump);
            Assert.That(_tracker.HistoryCount, Is.EqualTo(0));
            Assert.That(state.PressCount, Is.EqualTo(0));
            Assert.That(state.IsPressed, Is.True);
            Assert.That(state.TotalHeldSeconds, Is.EqualTo(0f));
            Assert.That(state.LastPressedTime, Is.EqualTo(-1f));
        }

        private InputActivityTracker Create(int capacity)
        {
            return new InputActivityTracker(new[]
            {
                new TrackedAction(Move, TrackedActionKind.Vector, 0f), new TrackedAction(Look, TrackedActionKind.Vector, 0.15f),
                new TrackedAction(Jump, TrackedActionKind.Button, 0f),
            }, capacity, 0.2f, _bus);
        }

        private void Button(string action, bool isPressed, bool wasPressed, bool wasReleased)
        {
            _samples[action] = new ActionSample(isPressed, wasPressed, wasReleased, new Vector2(isPressed ? 1f : 0f, 0f));
        }

        private void Vector(string action, Vector2 value)
        {
            _samples[action] = ActionSample.Vector(value);
        }

        private void Sample(float time, InputDeviceInfo device)
        {
            _tracker.Sample(time, device, action => _samples.TryGetValue(action.Action, out ActionSample sample) ? sample : default);

            // A button's frame flags last one sample, like the input service's.
            foreach (string action in new List<string>(_samples.Keys))
            {
                ActionSample sample = _samples[action];
                _samples[action] = new ActionSample(sample.IsPressed, false, false, sample.Value);
            }
        }
    }
}
