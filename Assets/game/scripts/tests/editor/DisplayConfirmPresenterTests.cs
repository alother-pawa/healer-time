using Game.Systems.Settings;
using NUnit.Framework;
using Paowa.Desktop;

namespace Game.Tests.Editor
{
    /// <summary>
    /// "Keep these display settings? (N)" logic with a fake change and view: a pending change shows the popup once, the
    /// countdown is the remaining time rounded up, only the shown change's resolution hides it (a newer Apply closes the
    /// popup through the older change's Superseded, then reopens it when the newer change is pending), Keep confirms, Revert reverts, and closing the popup otherwise reverts an unresolved
    /// change but never a resolved one.
    /// </summary>
    public sealed class DisplayConfirmPresenterTests
    {
        private FakeView _view;
        private DisplayConfirmPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _presenter = new DisplayConfirmPresenter(_view);
        }

        [Test]
        public void APendingChangeShowsThePopupOnce()
        {
            var change = new FakeChange(15f);

            _presenter.OnPending(change);
            _presenter.OnPending(change);

            Assert.That(_view.ShowCount, Is.EqualTo(1));
            Assert.That(_presenter.Current, Is.SameAs(change));
            Assert.That(_presenter.IsShown, Is.True);
        }

        [Test]
        public void TheCountdownRoundsUp()
        {
            var change = new FakeChange(15f);
            _presenter.OnPending(change);

            Assert.That(_presenter.SecondsLeft, Is.EqualTo(15));
            change.Remaining = 14.2f;
            Assert.That(_presenter.SecondsLeft, Is.EqualTo(15));
            change.Remaining = 0.3f;
            Assert.That(_presenter.SecondsLeft, Is.EqualTo(1));
            change.Remaining = -0.1f;
            Assert.That(_presenter.SecondsLeft, Is.Zero);
        }

        [Test]
        public void ResolvingTheShownChangeHidesThePopup()
        {
            var change = new FakeChange(15f);
            _presenter.OnPending(change);

            change.Resolve(DisplayChangeState.TimedOut);
            _presenter.OnResolved(change);

            Assert.That(_view.HideCount, Is.EqualTo(1));
            Assert.That(_presenter.Current, Is.Null);
            Assert.That(_presenter.SecondsLeft, Is.Zero);
        }

        [Test]
        public void ANewerChangeClosesThenReopensThePopup()
        {
            var older = new FakeChange(15f);
            var newer = new FakeChange(15f);
            _presenter.OnPending(older);

            // The display service's order: Apply resolves the older change at once (Superseded), the newer one is pending
            // only after the new mode settled.
            older.Resolve(DisplayChangeState.Superseded);
            _presenter.OnResolved(older);
            Assert.That(_view.HideCount, Is.EqualTo(1));
            Assert.That(_presenter.IsShown, Is.False);
            _presenter.OnViewClosed();
            Assert.That(older.RevertCount, Is.Zero, "A resolved change is never reverted by the closing popup.");

            _presenter.OnPending(newer);

            Assert.That(_view.ShowCount, Is.EqualTo(2));
            Assert.That(_presenter.Current, Is.SameAs(newer));
            Assert.That(_presenter.SecondsLeft, Is.EqualTo(15), "The countdown starts again for the newer change.");
        }

        [Test]
        public void AStaleResolutionDoesNotCloseTheShownChange()
        {
            var older = new FakeChange(15f);
            var newer = new FakeChange(15f);
            _presenter.OnPending(older);
            _presenter.OnPending(newer);

            older.Resolve(DisplayChangeState.Superseded);
            _presenter.OnResolved(older);

            Assert.That(_view.HideCount, Is.Zero, "Only the shown change closes the popup, whatever the event order.");
            Assert.That(_presenter.Current, Is.SameAs(newer));
        }

        [Test]
        public void KeepConfirmsAndRevertReverts()
        {
            var change = new FakeChange(15f);
            _presenter.OnPending(change);

            _presenter.Keep();
            Assert.That(change.ConfirmCount, Is.EqualTo(1));

            var other = new FakeChange(15f);
            _presenter.OnPending(other);
            _presenter.Revert();
            Assert.That(other.RevertCount, Is.EqualTo(1));
        }

        [Test]
        public void ClosingThePopupRevertsAnUnresolvedChange()
        {
            var change = new FakeChange(15f);
            _presenter.OnPending(change);

            _presenter.OnViewClosed();

            Assert.That(change.RevertCount, Is.EqualTo(1));
            Assert.That(_presenter.IsShown, Is.False);
        }

        [Test]
        public void ClosingAfterTheChangeResolvedRevertsNothing()
        {
            var change = new FakeChange(15f);
            _presenter.OnPending(change);
            change.Resolve(DisplayChangeState.Confirmed);
            _presenter.OnResolved(change);

            _presenter.OnViewClosed();

            Assert.That(change.RevertCount, Is.Zero);
        }

        [Test]
        public void AnAlreadyResolvedChangeShowsNothing()
        {
            var change = new FakeChange(0f);
            change.Resolve(DisplayChangeState.Reverted);

            _presenter.OnPending(change);

            Assert.That(_view.ShowCount, Is.Zero);
            Assert.That(_presenter.Current, Is.Null);
        }

        private sealed class FakeView : IDisplayConfirmView
        {
            internal int ShowCount;
            internal int HideCount;

            public void Show(DisplayConfirmPresenter presenter)
            {
                ShowCount++;
            }

            public void Hide()
            {
                HideCount++;
            }
        }

        private sealed class FakeChange : IPendingDisplayChange
        {
            internal float Remaining;
            internal int ConfirmCount;
            internal int RevertCount;

            internal FakeChange(float remaining)
            {
                Remaining = remaining;
                State = DisplayChangeState.AwaitingConfirmation;
            }

            public DisplayMode Target => default;

            public DisplayMode Previous => default;

            public DisplayChangeState State { get; private set; }

            public bool IsResolved => State != DisplayChangeState.Applying && State != DisplayChangeState.AwaitingConfirmation;

            public float SecondsRemaining => IsResolved ? 0f : Remaining;

            public void Confirm()
            {
                ConfirmCount++;
            }

            public void Revert()
            {
                RevertCount++;
            }

            internal void Resolve(DisplayChangeState state)
            {
                State = state;
            }
        }
    }
}
