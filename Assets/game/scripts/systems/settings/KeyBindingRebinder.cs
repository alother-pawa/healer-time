using System;
using Game.Contracts.Presentation;
using Paowa.Input;
using Paowa.Input.Rebinding;
using Paowa.Settings;
using UnityEngine;

namespace Game.Systems.Settings
{
    /// <summary>
    /// State machine of one rebind from a key binding row: <see cref="Phase.Idle"/> → <see cref="Phase.Listening"/> (Core's
    /// <c>RebindAsync</c> waits for the new control; its cancel controls end it) → done, or, when the pressed control is
    /// already used by another binding of the scheme, <see cref="Phase.ConfirmingSwap"/> (a "Swap?" confirmation) → the
    /// two bindings exchange paths with <c>SetBindingOverride</c> (the other binding takes this one's old path, which may be
    /// empty for an unused secondary slot), or nothing changes. Core saves every override itself. One rebind at a time;
    /// <see cref="PhaseChanged"/> fires on every phase change, with the state already updated, so the screen redraws from
    /// <see cref="CurrentPhase"/>/<see cref="Target"/> and blocks UI navigation only while listening. A rebind cancelled from
    /// the screen (<see cref="Cancel"/>, e.g. the mouse-only Cancel button) that still completes in the same instant (the
    /// click that pressed Cancel was also taken as the new control) is undone: the binding gets its previous control back.
    /// </summary>
    internal sealed class KeyBindingRebinder
    {
        private readonly IInputService _input;
        private readonly IRebindService _rebind;
        private readonly RebindOptions _options;
        private readonly Func<ConfirmRequest, Awaitable<bool>> _confirm;
        private readonly Func<InputBindingInfo, ConfirmRequest> _swapRequest;
        private bool _cancelRequested;

        /// <param name="confirm">Shows a confirmation and completes with the answer.</param>
        /// <param name="swapRequest">The "Swap?" confirmation for the binding already using the pressed control.</param>
        internal KeyBindingRebinder(IInputService input, IRebindService rebind, RebindOptions options, Func<ConfirmRequest, Awaitable<bool>> confirm,
            Func<InputBindingInfo, ConfirmRequest> swapRequest)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rebind = rebind ?? throw new ArgumentNullException(nameof(rebind));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
            _swapRequest = swapRequest ?? throw new ArgumentNullException(nameof(swapRequest));
        }

        /// <summary>Where the rebind is.</summary>
        internal enum Phase
        {
            /// <summary>No rebind.</summary>
            Idle = 0,

            /// <summary>Waiting for the player to press the new control.</summary>
            Listening = 1,

            /// <summary>The control is used elsewhere; the "Swap?" confirmation is open.</summary>
            ConfirmingSwap = 2
        }

        /// <summary>How a rebind ended.</summary>
        internal enum Outcome
        {
            /// <summary>The new control was applied.</summary>
            Completed = 0,

            /// <summary>Cancelled (cancel control, timeout, screen closed).</summary>
            Cancelled = 1,

            /// <summary>The two bindings exchanged their controls.</summary>
            Swapped = 2,

            /// <summary>The control was used elsewhere and the player kept both bindings as they were.</summary>
            SwapDeclined = 3,

            /// <summary>The row's binding does not exist in the input actions.</summary>
            Unbound = 4,

            /// <summary>The input backend cannot rebind.</summary>
            Unsupported = 5,

            /// <summary>Another rebind is in progress; nothing started.</summary>
            Busy = 6
        }

        /// <summary>Current phase.</summary>
        /// <summary>True when the rebinding package has a backend (rows are editable).</summary>
        internal bool SupportsRebinding => _rebind.SupportsRebinding;

        internal Phase CurrentPhase { get; private set; }

        /// <summary>The row slot being rebound; null while idle.</summary>
        internal KeyBindingSettingDefinition Target { get; private set; }

        /// <summary>Raised after every phase change.</summary>
        internal event Action PhaseChanged;

        /// <summary>Rebinds the binding of <paramref name="definition"/>; completes when the rebind (and any swap question) is over.</summary>
        internal async Awaitable<Outcome> RebindAsync(KeyBindingSettingDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (CurrentPhase != Phase.Idle)
            {
                return Outcome.Busy;
            }

            if (!_rebind.SupportsRebinding)
            {
                return Outcome.Unsupported;
            }

            if (!definition.TryFindBinding(_input, out InputBindingInfo binding))
            {
                return Outcome.Unbound;
            }

            string previousPath = binding.EffectivePath;
            _cancelRequested = false;
            SetPhase(Phase.Listening, definition);
            try
            {
                RebindResult result = await _rebind.RebindAsync(definition.Action, binding.BindingIndex, _options);
                switch (result.Status)
                {
                    case RebindStatus.Completed when _cancelRequested:
                        // Core applied the control that raced the cancel: put the previous one back.
                        if (binding.IsOverridden)
                        {
                            _rebind.SetBindingOverride(definition.Action, binding.BindingIndex, previousPath);
                        }
                        else
                        {
                            _rebind.ResetBinding(definition.Action, binding.BindingIndex);
                        }

                        return Outcome.Cancelled;
                    case RebindStatus.Completed:
                        return Outcome.Completed;
                    case RebindStatus.Unsupported:
                        return Outcome.Unsupported;
                    case RebindStatus.Duplicate when _cancelRequested:
                        return Outcome.Cancelled;
                    case RebindStatus.Duplicate:
                        SetPhase(Phase.ConfirmingSwap, definition);
                        if (!await _confirm(_swapRequest(result.Conflict)))
                        {
                            return Outcome.SwapDeclined;
                        }

                        // The other binding takes our old control first, then we take the pressed one.
                        _rebind.SetBindingOverride(result.Conflict.Action, result.Conflict.BindingIndex, previousPath);
                        _rebind.SetBindingOverride(definition.Action, binding.BindingIndex, result.Path);
                        return Outcome.Swapped;
                    default:
                        return Outcome.Cancelled;
                }
            }
            finally
            {
                _cancelRequested = false;
                SetPhase(Phase.Idle, null);
            }
        }

        /// <summary>
        /// Cancels a rebind that is listening (the screen closed, the Cancel button); a swap question is answered by its dialog.
        /// </summary>
        internal void Cancel()
        {
            if (CurrentPhase == Phase.Listening)
            {
                _cancelRequested = true;
                _rebind.CancelRebind();
            }
        }

        private void SetPhase(Phase phase, KeyBindingSettingDefinition target)
        {
            CurrentPhase = phase;
            Target = target;
            PhaseChanged?.Invoke();
        }
    }
}
