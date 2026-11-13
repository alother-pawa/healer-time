using System;
using System.Collections.Generic;
using Game.Systems.Gameplay;
using NUnit.Framework;
using Paowa.Pause;

namespace Game.Tests.Editor
{
    /// <summary>
    /// When the pause menu opens and closes: the pause press opens it with a Menu pause taken first; focus and controller
    /// loss open it at the next tick (other reasons do not); every way of hiding releases the pause (button, the popup closed
    /// from outside, the resume press); one press never closes and reopens it (no resume in the frame it opened, no reopen in
    /// the frame it closed); a view that throws or does not open gives the pause back; reopening after a failed leave; nothing
    /// opens outside gameplay; leaving gameplay closes it and turns automatic pauses off.
    /// </summary>
    public sealed class PauseControllerTests
    {
        private FakePause _pause;
        private FakeView _view;
        private PauseController _controller;
        private int _frame;

        [SetUp]
        public void SetUp()
        {
            _pause = new FakePause();
            _view = new FakeView { Pause = _pause };
            _frame = 1;
            _controller = new PauseController(_pause, _view, () => _frame);
            _view.Controller = _controller;
            _controller.Enter();
        }

        [Test]
        public void EnteringGameplayAllowsAutomaticPauses()
        {
            Assert.That(_pause.AutoPauseEnabled, Is.True);
        }

        [Test]
        public void ThePausePressOpensTheMenuWithAMenuPauseTakenFirst()
        {
            Assert.That(_controller.OnPausePressed(), Is.True);

            Assert.That(_controller.IsMenuOpen, Is.True);
            Assert.That(_view.Log, Is.EqualTo(new[] { "show" }));
            Assert.That(_view.PausedWhenShown, Is.True, "The pause is in place before the view runs.");
            Assert.That(_pause.IsPausedBy(PauseReason.Menu), Is.True);
        }

        [Test]
        public void ResumeHidesTheMenuAndReleasesThePause()
        {
            _controller.OnPausePressed();
            _frame++;

            _controller.Resume();

            Assert.That(_controller.IsMenuOpen, Is.False);
            Assert.That(_view.Log, Is.EqualTo(new[] { "show", "hide" }));
            Assert.That(_pause.IsPaused, Is.False);
        }

        [Test]
        public void ThePopupClosedFromOutsideReleasesThePause()
        {
            _controller.OnPausePressed();
            _frame++;

            _controller.OnMenuHidden(); // Cancel/Back through the popup handler.
            _controller.OnMenuHidden();

            Assert.That(_controller.IsMenuOpen, Is.False);
            Assert.That(_pause.IsPaused, Is.False);
            Assert.That(_pause.Released, Is.EqualTo(1), "Released once.");
        }

        [Test]
        public void ThePressThatOpenedTheMenuCannotCloseIt()
        {
            _controller.OnPausePressed();

            Assert.That(_controller.OnResumePressed(), Is.False, "Same frame.");
            Assert.That(_controller.IsMenuOpen, Is.True);

            _frame++;
            Assert.That(_controller.OnResumePressed(), Is.True);
            Assert.That(_controller.IsMenuOpen, Is.False);
        }

        [Test]
        public void ThePressThatClosedTheMenuCannotReopenIt()
        {
            _controller.OnPausePressed();
            _frame++;
            _controller.OnResumePressed();

            Assert.That(_controller.OnPausePressed(), Is.False, "Same frame as the resume.");
            Assert.That(_controller.IsMenuOpen, Is.False);

            _frame++;
            Assert.That(_controller.OnPausePressed(), Is.True);
        }

        [Test]
        public void ThePausePressDoesNothingWhileTheMenuIsOpen()
        {
            _controller.OnPausePressed();
            _frame++;

            Assert.That(_controller.OnPausePressed(), Is.False);
            Assert.That(_view.Log, Is.EqualTo(new[] { "show" }));
            Assert.That(_pause.Handles, Is.EqualTo(1));
        }

        [TestCase(PauseReason.FocusLost)]
        [TestCase(PauseReason.ControllerLost)]
        [TestCase(PauseReason.Overlay)]
        public void AutomaticPauseOpensTheMenuAtTheNextTick(PauseReason reason)
        {
            _controller.OnPauseChanged(reason, true);

            Assert.That(_controller.IsMenuOpen, Is.False, "Not from inside the pause service's event.");
            _controller.Tick();
            Assert.That(_controller.IsMenuOpen, Is.True);
            Assert.That(_pause.IsPausedBy(PauseReason.Menu), Is.True);

            _controller.Tick();
            Assert.That(_view.Log, Is.EqualTo(new[] { "show" }), "Opened once.");
        }

        [Test]
        public void OtherReasonsAndEndingReasonsDoNotOpenTheMenu()
        {
            _controller.OnPauseChanged(PauseReason.Menu, true);
            _controller.OnPauseChanged(PauseReason.Custom, true);
            _controller.OnPauseChanged(PauseReason.FocusLost, false);
            _controller.Tick();

            Assert.That(_controller.IsMenuOpen, Is.False);
        }

        [Test]
        public void AFocusLossWhileTheMenuIsOpenKeepsOneMenu()
        {
            _controller.OnPausePressed();
            _controller.OnPauseChanged(PauseReason.FocusLost, true);
            _controller.Tick();

            Assert.That(_view.Log, Is.EqualTo(new[] { "show" }));
        }

        [Test]
        public void NothingOpensOutsideGameplay()
        {
            _controller.Exit();

            Assert.That(_controller.OnPausePressed(), Is.False);
            _controller.OnPauseChanged(PauseReason.FocusLost, true);
            _controller.Tick();
            Assert.That(_controller.IsMenuOpen, Is.False);
            Assert.That(_pause.AutoPauseEnabled, Is.False);
        }

        [Test]
        public void LeavingGameplayClosesTheMenuAndReleasesThePause()
        {
            _controller.OnPausePressed();

            _controller.Exit();

            Assert.That(_controller.IsMenuOpen, Is.False);
            Assert.That(_view.Log, Is.EqualTo(new[] { "show", "hide" }));
            Assert.That(_pause.IsPaused, Is.False);
            Assert.That(_pause.AutoPauseEnabled, Is.False);
        }

        [Test]
        public void ARequestedAutomaticOpenIsDroppedWhenLeaving()
        {
            _controller.OnPauseChanged(PauseReason.ControllerLost, true);
            _controller.Exit();
            _controller.Enter();
            _controller.Tick();

            Assert.That(_controller.IsMenuOpen, Is.False);
        }

        [Test]
        public void WithoutThePauseServiceTheMenuStillOpensAndCloses()
        {
            var view = new FakeView();
            var controller = new PauseController(null, view, () => _frame);
            view.Controller = controller;
            controller.Enter();

            Assert.That(controller.OnPausePressed(), Is.True);
            _frame++;
            controller.Resume();
            Assert.That(view.Log, Is.EqualTo(new[] { "show", "hide" }));
        }

        [Test]
        public void AViewThatDoesNotOpenGivesThePauseBack()
        {
            _view.Opens = false;

            Assert.That(_controller.OnPausePressed(), Is.True);

            Assert.That(_controller.IsMenuOpen, Is.False);
            Assert.That(_pause.IsPaused, Is.False, "Never paused without a menu to resume from.");
        }

        [Test]
        public void AViewThatThrowsGivesThePauseBack()
        {
            _view.Throws = true;

            Assert.Throws<InvalidOperationException>(() => _controller.OnPausePressed());

            Assert.That(_controller.IsMenuOpen, Is.False);
            Assert.That(_pause.IsPaused, Is.False);
        }

        [Test]
        public void ReopenIgnoresTheSameFrameGuardButOnlyInGameplay()
        {
            _controller.OnPausePressed();
            _controller.Resume();

            _controller.Reopen(); // Same frame: leaving failed right after the menu closed.
            Assert.That(_controller.IsMenuOpen, Is.True);

            _controller.Exit();
            _controller.Reopen();
            Assert.That(_controller.IsMenuOpen, Is.False);
        }

        private sealed class FakeView : IPauseMenuView
        {
            internal readonly List<string> Log = new List<string>();
            internal PauseController Controller;
            internal FakePause Pause;
            internal bool PausedWhenShown;
            internal bool Opens = true;
            internal bool Throws;

            public bool Show()
            {
                Log.Add("show");
                PausedWhenShown = Pause != null && Pause.IsPausedBy(PauseReason.Menu);
                if (Throws)
                {
                    throw new InvalidOperationException("view failed");
                }

                return Opens;
            }

            // Like the popup: hiding reports back to the controller (inside the hide call).
            public void Hide()
            {
                Log.Add("hide");
                if (Controller != null)
                {
                    Controller.OnMenuHidden();
                }
            }
        }

        private sealed class FakePause : IPauseService
        {
            private readonly List<PauseReason> _reasons = new List<PauseReason>();

            internal int Released { get; private set; }

            internal int Handles => _reasons.Count;

            public bool IsPaused => _reasons.Count > 0;

            public bool AutoPauseEnabled { get; set; }

            public bool PauseOnFocusLoss { get; set; } = true;

            public bool PauseOnControllerLost { get; set; } = true;

            public bool IsPausedBy(PauseReason reason)
            {
                return _reasons.Contains(reason);
            }

            public void SetOverlayOpen(bool isOpen)
            {
            }

            public IDisposable Pause(PauseReason reason)
            {
                _reasons.Add(reason);
                return new Handle(() =>
                {
                    _reasons.Remove(reason);
                    Released++;
                });
            }

            private sealed class Handle : IDisposable
            {
                private Action _release;

                internal Handle(Action release)
                {
                    _release = release;
                }

                public void Dispose()
                {
                    Action release = _release;
                    _release = null;
                    if (release != null)
                    {
                        release();
                    }
                }
            }
        }
    }
}
