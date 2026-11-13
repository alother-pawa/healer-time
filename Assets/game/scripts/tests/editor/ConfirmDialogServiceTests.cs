using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Systems.UIShell;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Logic of the confirmation dialog without Unity UI: one dialog at a time, queued requests, every way of closing
    /// answers exactly once, the popup is hidden (and the next request shown) as soon as the answer is given, and the result
    /// reaches the awaiting code only on a later frame (simulated by running the queued deliveries).
    /// </summary>
    public sealed class ConfirmDialogServiceTests
    {
        private readonly List<Action> _deliveries = new List<Action>();
        private FakePresenter _presenter;
        private ConfirmDialogService _service;

        [SetUp]
        public void SetUp()
        {
            _deliveries.Clear();
            _presenter = new FakePresenter();
            _service = new ConfirmDialogService(_presenter, null, _deliveries.Add);
        }

        [Test]
        public void ARequestShowsTheDialogAndWaits()
        {
            Awaitable<bool> answer = _service.ConfirmAsync(Request("quit"));

            Assert.That(_service.IsOpen, Is.True);
            Assert.That(_presenter.Shown, Has.Count.EqualTo(1));
            Assert.That(_presenter.Shown[0].Request.TitleKey, Is.EqualTo("quit.title"));
            Assert.That(answer.GetAwaiter().IsCompleted, Is.False);
        }

        [Test]
        public void ConfirmingAnswersTrueAndHides()
        {
            Awaitable<bool> answer = _service.ConfirmAsync(Request("quit"));

            _presenter.Current.Answer(true);

            Assert.That(Result(answer), Is.True);
            Assert.That(_presenter.HideCount, Is.EqualTo(1));
            Assert.That(_service.IsOpen, Is.False);
        }

        [Test]
        public void TheCancelButtonAnswersFalse()
        {
            Awaitable<bool> answer = _service.ConfirmAsync(Request("quit"));

            _presenter.Current.Answer(false);

            Assert.That(Result(answer), Is.False);
            Assert.That(_service.IsOpen, Is.False);
        }

        [Test]
        public void ClosingThePopupFromOutsideAnswersFalseAndShowsTheNextRequest()
        {
            Awaitable<bool> first = _service.ConfirmAsync(Request("first"));
            Awaitable<bool> second = _service.ConfirmAsync(Request("second"));

            // Cancel/Back closing the top popup: Core hides it, the popup's OnHidden answers false.
            _presenter.CloseExternally();

            Assert.That(Result(first), Is.False);
            Assert.That(_presenter.Current.Request.TitleKey, Is.EqualTo("second.title"));
            Assert.That(second.GetAwaiter().IsCompleted, Is.False);
        }

        [Test]
        public void TheResultArrivesOnALaterFrameAfterThePopupIsHidden()
        {
            Awaitable<bool> answer = _service.ConfirmAsync(Request("quit"));

            _presenter.Current.Answer(true);

            // Same frame (still inside the button's click): hidden, not delivered.
            Assert.That(_presenter.HideCount, Is.EqualTo(1));
            Assert.That(_service.IsOpen, Is.False);
            Assert.That(answer.GetAwaiter().IsCompleted, Is.False);
            Assert.That(_deliveries, Has.Count.EqualTo(1));

            Assert.That(Result(answer), Is.True);
        }

        [Test]
        public void OnlyTheFirstAnswerCounts()
        {
            Awaitable<bool> answer = _service.ConfirmAsync(Request("quit"));
            ConfirmDialogSession session = _presenter.Current;

            session.Answer(true);
            session.Answer(false);

            Assert.That(_deliveries, Has.Count.EqualTo(1));
            Assert.That(Result(answer), Is.True);
            Assert.That(_presenter.HideCount, Is.EqualTo(1));
        }

        [Test]
        public void RequestsWhileOpenWaitInOrder()
        {
            Awaitable<bool> first = _service.ConfirmAsync(Request("first"));
            Awaitable<bool> second = _service.ConfirmAsync(Request("second"));

            Assert.That(_presenter.Shown, Has.Count.EqualTo(1));
            Assert.That(_service.PendingCount, Is.EqualTo(1));

            _presenter.Current.Answer(false);

            Assert.That(_presenter.Shown, Has.Count.EqualTo(2), "The next request opens at once.");
            Assert.That(_presenter.Current.Request.TitleKey, Is.EqualTo("second.title"));
            Assert.That(Result(first), Is.False);
            Assert.That(second.GetAwaiter().IsCompleted, Is.False);

            _presenter.Current.Answer(true);

            Assert.That(Result(second), Is.True);
            Assert.That(_service.IsOpen, Is.False);
        }

        [Test]
        public void APresenterFailureAnswersFalseAndShowsTheNextRequest()
        {
            _presenter.FailNextShow = true;

            Awaitable<bool> failed = _service.ConfirmAsync(Request("broken"));
            Awaitable<bool> next = _service.ConfirmAsync(Request("next"));

            Assert.That(Result(failed), Is.False);
            Assert.That(_service.IsOpen, Is.True);
            Assert.That(_presenter.Current.Request.TitleKey, Is.EqualTo("next.title"));
            Assert.That(next.GetAwaiter().IsCompleted, Is.False);
        }

        [Test]
        public void ANoticeHasNoCancelButton()
        {
            var notice = new ConfirmRequest("play.title", "play.message", "common.ok", null);
            var question = new ConfirmRequest("quit.title", "quit.message", "quit.confirm", "common.cancel");

            Assert.That(notice.HasCancel, Is.False);
            Assert.That(notice.CancelKey, Is.Empty);
            Assert.That(notice.MessageArguments, Is.Null);
            Assert.That(question.HasCancel, Is.True);
        }

        [Test]
        public void ARequestNeedsTitleMessageAndConfirmKeys()
        {
            Assert.Throws<ArgumentException>(() => new ConfirmRequest(null, "m", "c", "x"));
            Assert.Throws<ArgumentException>(() => new ConfirmRequest("t", "", "c", "x"));
            Assert.Throws<ArgumentException>(() => new ConfirmRequest("t", "m", null, "x"));
            Assert.Throws<ArgumentNullException>(() => _service.ConfirmAsync(null));
        }

        private static ConfirmRequest Request(string name)
        {
            return new ConfirmRequest(name + ".title", name + ".message", "common.ok", "common.cancel");
        }

        // Runs what a later frame would deliver, then reads the answer.
        private bool Result(Awaitable<bool> answer)
        {
            Action[] pending = _deliveries.ToArray();
            _deliveries.Clear();
            foreach (Action deliver in pending)
            {
                deliver();
            }

            Awaitable<bool>.Awaiter awaiter = answer.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "The answer was not delivered.");
            return awaiter.GetResult();
        }

        private sealed class FakePresenter : IConfirmDialogPresenter
        {
            internal readonly List<ConfirmDialogSession> Shown = new List<ConfirmDialogSession>();
            internal int HideCount;
            internal bool FailNextShow;
            private bool _open;

            internal ConfirmDialogSession Current => Shown.Count > 0 ? Shown[Shown.Count - 1] : null;

            public void Show(ConfirmDialogSession session)
            {
                if (FailNextShow)
                {
                    FailNextShow = false;
                    throw new InvalidOperationException("Popup missing (test).");
                }

                Shown.Add(session);
                _open = true;
            }

            public void Hide()
            {
                // Like Core: hiding a popup that is not open does nothing.
                if (_open)
                {
                    HideCount++;
                    _open = false;
                }
            }

            /// <summary>Core hid the popup (Cancel/Back, HideAllPopups); the popup answers false from OnHidden.</summary>
            internal void CloseExternally()
            {
                _open = false;
                Current.Answer(false);
            }
        }
    }
}
