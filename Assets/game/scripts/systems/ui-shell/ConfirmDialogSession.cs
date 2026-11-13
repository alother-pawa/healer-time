using System;
using Game.Contracts.Presentation;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// One confirmation from request to answer: the popup receives it as its payload and answers through it. Only the first
    /// answer counts. The owner is told at once (it hides the popup and shows the next request), while the result is
    /// handed to <c>deliver</c>, which completes <see cref="Result"/> later (the next frame at runtime), so the awaiting code
    /// never runs inside a button's click handler or a popup callback, and always finds the dialog already closed.
    /// </summary>
    internal sealed class ConfirmDialogSession
    {
        private readonly AwaitableCompletionSource<bool> _result = new AwaitableCompletionSource<bool>();
        private readonly Action<ConfirmDialogSession> _onAnswered;
        private readonly Action<Action> _deliver;

        internal ConfirmDialogSession(ConfirmRequest request, Action<ConfirmDialogSession> onAnswered, Action<Action> deliver)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            _onAnswered = onAnswered;
            _deliver = deliver ?? throw new ArgumentNullException(nameof(deliver));
        }

        /// <summary>Texts of the dialog.</summary>
        internal ConfirmRequest Request { get; }

        /// <summary>True once answered (the result may still be on its way).</summary>
        internal bool IsAnswered { get; private set; }

        /// <summary>Completes with the answer, after <c>deliver</c> runs.</summary>
        internal Awaitable<bool> Result => _result.Awaitable;

        /// <summary>Records the answer (later calls do nothing), tells the owner, then schedules the result.</summary>
        internal void Answer(bool confirmed)
        {
            if (IsAnswered)
            {
                return;
            }

            IsAnswered = true;
            try
            {
                _onAnswered?.Invoke(this);
            }
            finally
            {
                _deliver(() => _result.SetResult(confirmed));
            }
        }
    }
}
