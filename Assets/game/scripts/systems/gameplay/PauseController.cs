using System;
using Paowa.Pause;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// When the pause menu opens and closes during gameplay (pure logic over <see cref="IPauseService"/> and an
    /// <see cref="IPauseMenuView"/>). While gameplay runs, automatic pauses are allowed (<see cref="Enter"/>/<see cref="Exit"/>).
    /// The menu holds a <see cref="PauseReason.Menu"/> handle, taken before the view shows and released whenever the view
    /// hides (Resume button, Cancel/Back through the popup handler, the resume press, or any other path), so the pause never
    /// outlives the menu. Opening:
    /// <list type="bullet">
    /// <item>the gameplay pause press (<see cref="OnPausePressed"/>), ignored while the menu is open and in the frame it
    /// closed, so one press never closes and reopens it;</item>
    /// <item>a focus-lost or controller-lost pause (<see cref="OnPauseChanged"/>), opened at the next <see cref="Tick"/> so
    /// the pause service is never re-entered from its own event.</item>
    /// </list>
    /// The resume press (<see cref="OnResumePressed"/>) is ignored in the frame the menu opened, so the press that opened it
    /// cannot close it. If the view throws or does not open, the pause is released at once.
    /// </summary>
    internal sealed class PauseController
    {
        private readonly IPauseService _pause;
        private readonly IPauseMenuView _view;
        private readonly Func<int> _frame;
        private IDisposable _menuPause;
        private bool _entered;
        private bool _autoOpenRequested;
        private int _openedFrame = int.MinValue;
        private int _closedFrame = int.MinValue;

        /// <param name="pause">Desktop pause service; null when the module is off (the menu still opens, nothing is paused).</param>
        /// <param name="view">The menu.</param>
        /// <param name="frame">Current frame number (<c>Time.frameCount</c>).</param>
        internal PauseController(IPauseService pause, IPauseMenuView view, Func<int> frame)
        {
            _pause = pause;
            _view = view;
            _frame = frame;
        }

        /// <summary>True from opening until the view reported it hidden.</summary>
        internal bool IsMenuOpen { get; private set; }

        /// <summary>Gameplay started: automatic pauses are allowed.</summary>
        internal void Enter()
        {
            _entered = true;
            if (_pause != null)
            {
                _pause.AutoPauseEnabled = true;
            }
        }

        /// <summary>Gameplay ends (leaving, scene unloading): automatic pauses stop and an open menu closes.</summary>
        internal void Exit()
        {
            _entered = false;
            _autoOpenRequested = false;
            if (_pause != null)
            {
                _pause.AutoPauseEnabled = false;
            }

            if (IsMenuOpen)
            {
                _view.Hide();
                OnMenuHidden();
            }
        }

        /// <summary>The gameplay pause action was pressed this frame; true when the menu opened.</summary>
        internal bool OnPausePressed()
        {
            if (!_entered || IsMenuOpen || _frame() == _closedFrame)
            {
                return false;
            }

            Open();
            return true;
        }

        /// <summary>The pause action of the menu (e.g. Start) was pressed this frame; true when the menu closed.</summary>
        internal bool OnResumePressed()
        {
            if (!IsMenuOpen || _frame() == _openedFrame)
            {
                return false;
            }

            Resume();
            return true;
        }

        /// <summary>A pause reason started or ended; focus loss, controller loss and the platform overlay ask for the menu.</summary>
        internal void OnPauseChanged(PauseReason reason, bool reasonActive)
        {
            if (_entered && reasonActive && (reason == PauseReason.FocusLost || reason == PauseReason.ControllerLost
                || reason == PauseReason.Overlay))
            {
                _autoOpenRequested = true;
            }
        }

        /// <summary>Once per frame: opens the menu an automatic pause asked for.</summary>
        internal void Tick()
        {
            if (!_autoOpenRequested)
            {
                return;
            }

            _autoOpenRequested = false;
            if (_entered && !IsMenuOpen)
            {
                Open();
            }
        }

        /// <summary>Resume (button): hides the menu, which releases the pause.</summary>
        internal void Resume()
        {
            if (!IsMenuOpen)
            {
                return;
            }

            _view.Hide();
            OnMenuHidden();
        }

        /// <summary>The view is hidden, by whatever path; releases the menu's pause. Calling it again does nothing.</summary>
        internal void OnMenuHidden()
        {
            if (!IsMenuOpen)
            {
                return;
            }

            IsMenuOpen = false;
            _closedFrame = _frame();
            IDisposable menuPause = _menuPause;
            _menuPause = null;
            if (menuPause != null)
            {
                menuPause.Dispose();
            }
        }

        /// <summary>
        /// Opens the menu again after leaving failed (the scene switch did not start), ignoring the same-frame guard. Does
        /// nothing outside gameplay or while the menu is open.
        /// </summary>
        internal void Reopen()
        {
            if (_entered && !IsMenuOpen)
            {
                Open();
            }
        }

        private void Open()
        {
            // State first: the pause and the open flag are in place before the view runs its own code. A view that throws or
            // does not open gives the pause back, so the game is never paused without a menu to resume from.
            _menuPause = _pause != null ? _pause.Pause(PauseReason.Menu) : null;
            IsMenuOpen = true;
            _openedFrame = _frame();
            bool shown = false;
            try
            {
                shown = _view.Show();
            }
            finally
            {
                if (!shown)
                {
                    OnMenuHidden();
                }
            }
        }
    }
}
