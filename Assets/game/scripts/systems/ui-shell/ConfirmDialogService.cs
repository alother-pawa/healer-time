using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Logging;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// Default <see cref="IConfirmDialog"/>: one dialog at a time, later requests queued in order. Whatever closes the popup
    /// (a button, Cancel/Back closing the top popup, a scene teardown hiding every popup) answers the shown session, so no
    /// awaiting flow hangs. An answer hides the popup and shows the next request immediately; the result reaches the
    /// awaiting code through the delivery callback (the next frame by default). A presenter that throws answers false and
    /// moves on to the next request.
    /// </summary>
    internal sealed class ConfirmDialogService : IConfirmDialog
    {
        private static readonly LogCategory Category = new LogCategory("Confirm", new Color(0.7f, 0.85f, 1f));

        private readonly IConfirmDialogPresenter _presenter;
        private readonly ICoreLogger _logger;
        private readonly Action<Action> _deliver;
        private readonly Queue<ConfirmDialogSession> _pending = new Queue<ConfirmDialogSession>();
        private ConfirmDialogSession _current;

        /// <param name="logger">Receives presenter failures; may be null.</param>
        /// <param name="deliver">Runs the completion of an answered request; null completes it on the next frame.</param>
        internal ConfirmDialogService(IConfirmDialogPresenter presenter, ICoreLogger logger, Action<Action> deliver = null)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _logger = logger;
            _deliver = deliver ?? new Action<Action>(DeliverNextFrame);
        }

        public bool IsOpen => _current != null;

        /// <summary>Requests waiting behind the shown one.</summary>
        internal int PendingCount => _pending.Count;

        public Awaitable<bool> ConfirmAsync(ConfirmRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var session = new ConfirmDialogSession(request, OnAnswered, _deliver);
            _pending.Enqueue(session);
            ShowNext();
            return session.Result;
        }

        private void ShowNext()
        {
            while (_current == null && _pending.Count > 0)
            {
                ConfirmDialogSession next = _pending.Dequeue();
                _current = next;
                try
                {
                    _presenter.Show(next);
                }
                catch (Exception exception)
                {
                    if (_logger != null)
                    {
                        _logger.LogError(Category, $"Showing the confirmation dialog failed: {exception}");
                    }

                    // Answering clears _current through OnAnswered, so the loop moves on to the next request.
                    next.Answer(false);
                }
            }
        }

        private static void DeliverNextFrame(Action complete)
        {
            _ = DeliverNextFrameAsync(complete);
        }

        private static async Awaitable DeliverNextFrameAsync(Action complete)
        {
            await Awaitable.NextFrameAsync();
            complete();
        }

        // Runs when the answer is given, before its result is delivered: the popup is hidden and the next request shown.
        private void OnAnswered(ConfirmDialogSession session)
        {
            if (session != _current)
            {
                return;
            }

            _current = null;
            _presenter.Hide();
            ShowNext();
        }
    }
}
