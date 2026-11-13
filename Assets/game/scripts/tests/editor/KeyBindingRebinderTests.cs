using System;
using System.Collections.Generic;
using System.Linq;
using Game.Contracts.Presentation;
using Game.Systems.Settings;
using NUnit.Framework;
using Paowa.Input;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Rebind state machine of the key binding rows with a fake input service: listening while Core's rebind runs, back to
    /// idle on completion or cancel, the secondary slot rebinding the second binding of its scheme, a duplicate asking
    /// "Swap?" and then exchanging the two paths (the other binding first), a declined swap changing nothing, and the
    /// refusals (missing binding, unsupported backend, a rebind already running).
    /// </summary>
    public sealed class KeyBindingRebinderTests
    {
        private const string Keyboard = "KeyboardMouse";

        private readonly SettingDefinitionFactory _factory = new SettingDefinitionFactory();
        private FakeInput _input;
        private AwaitableCompletionSource<bool> _answer;
        private ConfirmRequest _asked;
        private InputBindingInfo _swapConflict;
        private KeyBindingRebinder _rebinder;
        private readonly List<KeyBindingRebinder.Phase> _phases = new List<KeyBindingRebinder.Phase>();

        [SetUp]
        public void SetUp()
        {
            _input = new FakeInput();
            _input.Add(new InputBindingInfo("Gameplay/Jump", 0, "<Keyboard>/space", Keyboard, displayString: "Space"));
            _input.Add(new InputBindingInfo("Gameplay/Jump", 1, string.Empty, Keyboard));
            _input.Add(new InputBindingInfo("Gameplay/Jump", 2, "<Gamepad>/buttonSouth", "Gamepad", displayString: "A"));
            _answer = null;
            _asked = null;
            _phases.Clear();
            _rebinder = new KeyBindingRebinder(_input, _input, new RebindOptions(), Confirm, conflict =>
            {
                _swapConflict = conflict;
                return new ConfirmRequest("swap.title", "swap.message", "swap.confirm", "common.cancel", conflict.DisplayString);
            });
            _rebinder.PhaseChanged += () => _phases.Add(_rebinder.CurrentPhase);
        }

        [TearDown]
        public void TearDown()
        {
            _factory.DestroyAll();
        }

        [Test]
        public void ACompletedRebindListensThenReturnsToIdle()
        {
            KeyBindingSettingDefinition jump = _factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0);

            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(jump);

            Assert.That(_rebinder.CurrentPhase, Is.EqualTo(KeyBindingRebinder.Phase.Listening));
            Assert.That(_rebinder.Target, Is.SameAs(jump));
            Assert.That(_input.Started, Is.EqualTo(new[] { ("Gameplay/Jump", 0) }));
            Assert.That(outcome.GetAwaiter().IsCompleted, Is.False);

            _input.Finish(RebindResult.Completed("<Keyboard>/j"));

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Completed));
            Assert.That(_rebinder.CurrentPhase, Is.EqualTo(KeyBindingRebinder.Phase.Idle));
            Assert.That(_rebinder.Target, Is.Null);
            Assert.That(_phases, Is.EqualTo(new[] { KeyBindingRebinder.Phase.Listening, KeyBindingRebinder.Phase.Idle }));
            Assert.That(_input.Overrides, Is.Empty, "Core applied the completed rebind itself.");
        }

        [Test]
        public void TheSecondarySlotRebindsTheSecondBindingOfItsScheme()
        {
            KeyBindingSettingDefinition secondary = _factory.Binding("jump.2", "Gameplay/Jump", Keyboard, "", 1);

            _rebinder.RebindAsync(secondary);

            Assert.That(_input.Started, Is.EqualTo(new[] { ("Gameplay/Jump", 1) }));
        }

        [Test]
        public void CancellingWhileListeningEndsTheRebind()
        {
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0));

            _rebinder.Cancel();

            Assert.That(_input.CancelCount, Is.EqualTo(1));
            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Cancelled));
            Assert.That(_rebinder.CurrentPhase, Is.EqualTo(KeyBindingRebinder.Phase.Idle));
            Assert.That(_input.Overrides, Is.Empty);
        }

        [Test]
        public void AControlThatRacesTheCancelIsUndone()
        {
            // The click on the Cancel button was also taken as the new control before the cancel arrived.
            _input.CancelCompletes = false;
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0));

            _rebinder.Cancel();
            _input.Finish(RebindResult.Completed("<Mouse>/leftButton"));

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Cancelled));
            Assert.That(_input.Resets, Is.EqualTo(new[] { ("Gameplay/Jump", 0) }), "A binding at its default goes back to the default.");
            Assert.That(_input.Overrides, Is.Empty);
        }

        [Test]
        public void AnOverriddenBindingThatRacesTheCancelGetsItsOverrideBack()
        {
            _input.CancelCompletes = false;
            _input.Add(new InputBindingInfo("Gameplay/Crouch", 7, "<Keyboard>/x", Keyboard, isOverridden: true));
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("crouch", "Gameplay/Crouch", Keyboard, "", 0));

            _rebinder.Cancel();
            _input.Finish(RebindResult.Completed("<Mouse>/leftButton"));

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Cancelled));
            Assert.That(_input.Overrides, Is.EqualTo(new[] { ("Gameplay/Crouch", 7, "<Keyboard>/x") }));
        }

        [Test]
        public void ADuplicateAsksThenSwapsBothBindings()
        {
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0));
            var interact = new InputBindingInfo("Gameplay/Interact", 3, "<Keyboard>/e", Keyboard, displayString: "E");

            _input.Finish(RebindResult.Duplicate("<Keyboard>/e", interact));

            Assert.That(_rebinder.CurrentPhase, Is.EqualTo(KeyBindingRebinder.Phase.ConfirmingSwap));
            Assert.That(_asked, Is.Not.Null);
            Assert.That(_asked.MessageArguments, Is.EqualTo(new object[] { "E" }));
            Assert.That(_swapConflict.Action, Is.EqualTo("Gameplay/Interact"));
            Assert.That(_input.Overrides, Is.Empty, "Nothing changes before the answer.");

            _answer.SetResult(true);

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Swapped));
            Assert.That(_input.Overrides, Is.EqualTo(new[]
            {
                ("Gameplay/Interact", 3, "<Keyboard>/space"),
                ("Gameplay/Jump", 0, "<Keyboard>/e"),
            }));
            Assert.That(_rebinder.CurrentPhase, Is.EqualTo(KeyBindingRebinder.Phase.Idle));
            Assert.That(_phases, Is.EqualTo(new[]
            {
                KeyBindingRebinder.Phase.Listening, KeyBindingRebinder.Phase.ConfirmingSwap, KeyBindingRebinder.Phase.Idle,
            }));
        }

        [Test]
        public void SwappingIntoAnEmptySecondarySlotLeavesTheOtherBindingEmpty()
        {
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("jump.2", "Gameplay/Jump", Keyboard, "", 1));
            var interact = new InputBindingInfo("Gameplay/Interact", 3, "<Keyboard>/e", Keyboard, displayString: "E");

            _input.Finish(RebindResult.Duplicate("<Keyboard>/e", interact));
            _answer.SetResult(true);

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Swapped), "An empty path is a valid override (Core 0.6.1).");
            Assert.That(_input.Overrides[0], Is.EqualTo(("Gameplay/Interact", 3, string.Empty)));
            Assert.That(_input.Overrides[1], Is.EqualTo(("Gameplay/Jump", 1, "<Keyboard>/e")));
        }

        [Test]
        public void ADeclinedSwapChangesNothing()
        {
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0));

            _input.Finish(RebindResult.Duplicate("<Keyboard>/e", new InputBindingInfo("Gameplay/Interact", 3, "<Keyboard>/e", Keyboard)));
            _answer.SetResult(false);

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.SwapDeclined));
            Assert.That(_input.Overrides, Is.Empty);
            Assert.That(_rebinder.CurrentPhase, Is.EqualTo(KeyBindingRebinder.Phase.Idle));
        }

        [Test]
        public void AMissingBindingStartsNothing()
        {
            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("crouch", "Gameplay/Crouch", Keyboard, "", 0));

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Unbound));
            Assert.That(_input.Started, Is.Empty);
            Assert.That(_phases, Is.Empty);
        }

        [Test]
        public void ABackendWithoutRebindingStartsNothing()
        {
            _input.CanRebind = false;

            Awaitable<KeyBindingRebinder.Outcome> outcome = _rebinder.RebindAsync(_factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0));

            Assert.That(Result(outcome), Is.EqualTo(KeyBindingRebinder.Outcome.Unsupported));
            Assert.That(_input.Started, Is.Empty);
        }

        [Test]
        public void ASecondRebindWhileOneRunsIsRefused()
        {
            KeyBindingSettingDefinition jump = _factory.Binding("jump", "Gameplay/Jump", Keyboard, "", 0);
            _rebinder.RebindAsync(jump);

            Awaitable<KeyBindingRebinder.Outcome> second = _rebinder.RebindAsync(_factory.Binding("pad", "Gameplay/Jump", "Gamepad", "", 0));

            Assert.That(Result(second), Is.EqualTo(KeyBindingRebinder.Outcome.Busy));
            Assert.That(_input.Started, Has.Count.EqualTo(1));
            Assert.That(_rebinder.Target, Is.SameAs(jump));
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            _asked = request;
            _answer = new AwaitableCompletionSource<bool>();
            return _answer.Awaitable;
        }

        private static KeyBindingRebinder.Outcome Result(Awaitable<KeyBindingRebinder.Outcome> outcome)
        {
            Awaitable<KeyBindingRebinder.Outcome>.Awaiter awaiter = outcome.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "The rebind has not finished.");
            return awaiter.GetResult();
        }

        /// <summary>Input and rebind services with a binding list, a controllable rebind and a record of overrides.</summary>
        private sealed class FakeInput : IInputService, IRebindService
        {
            private readonly List<InputBindingInfo> _bindings = new List<InputBindingInfo>();
            private AwaitableCompletionSource<RebindResult> _rebind;

            internal readonly List<(string action, int index)> Started = new List<(string action, int index)>();
            internal readonly List<(string action, int index, string path)> Overrides = new List<(string action, int index, string path)>();
            internal readonly List<(string action, int index)> Resets = new List<(string action, int index)>();
            internal int CancelCount;
            internal bool CancelCompletes = true;
            internal bool CanRebind = true;

            public InputDeviceKind CurrentDevice => InputDeviceKind.KeyboardMouse;

            public InputDeviceInfo CurrentDeviceInfo => default;

            public bool SupportsRebinding => CanRebind;

            public bool IsRebinding => _rebind != null;

            internal void Add(InputBindingInfo binding)
            {
                _bindings.Add(binding);
            }

            internal void Finish(RebindResult result)
            {
                AwaitableCompletionSource<RebindResult> rebind = _rebind;
                _rebind = null;
                rebind.SetResult(result);
            }

            public bool HasAction(string action)
            {
                return _bindings.Any(binding => binding.Action == action);
            }

            public IReadOnlyList<InputBindingInfo> GetBindings(string action, string controlScheme = null)
            {
                return _bindings.Where(binding => binding.Action == action &&
                                                  InputBindingInfo.BelongsToScheme(binding.Groups, controlScheme)).ToList();
            }

            public Awaitable<RebindResult> RebindAsync(string action, int bindingIndex, RebindOptions options = null)
            {
                Started.Add((action, bindingIndex));
                _rebind = new AwaitableCompletionSource<RebindResult>();
                return _rebind.Awaitable;
            }

            public void CancelRebind()
            {
                CancelCount++;
                if (_rebind != null && CancelCompletes)
                {
                    Finish(RebindResult.Cancelled);
                }
            }

            // Same validation as the rebinding package: an empty path means "unbound", null is refused.
            public void SetBindingOverride(string action, int bindingIndex, string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                Overrides.Add((action, bindingIndex, path));
            }

            public bool IsPressed(string action) => false;

            public bool WasPressedThisFrame(string action) => false;

            public bool WasReleasedThisFrame(string action) => false;

            public float ReadAxis(string action) => 0f;

            public Vector2 ReadVector2(string action) => Vector2.zero;

            public void EnableContext(string context)
            {
            }

            public void DisableContext(string context)
            {
            }

            public IDisposable RequestContext(string context) => new Release();

            public bool IsContextActive(string context) => false;

            public IDisposable Lock(string context = null) => new Release();

            public string GetBindingDisplayName(string action, int bindingIndex = 0) => string.Empty;

            public void ResetBinding(string action, int bindingIndex)
            {
                Resets.Add((action, bindingIndex));
            }

            public void ResetBindings(string action, string controlScheme = null)
            {
            }

            private sealed class Release : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
