using System;
using Paowa.Desktop;
using UnityEngine;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Logic of the "Keep these display settings? (N)" popup, fed by the display service's events. A pending change shows
    /// the popup; the change's resolution (confirmed, reverted, timed out, superseded) hides it, but only for the change
    /// shown. A newer <c>Apply</c> resolves the older change at once (Superseded: the popup closes) and publishes the newer
    /// one as pending once its mode settled (the popup opens again with a fresh countdown). Should a pending change arrive
    /// while the popup is open it just becomes <see cref="Current"/> (the popup reads it every frame), and a stale resolution
    /// never closes it. Closing the popup any other
    /// way (Cancel/Back, every popup hidden) reverts an unresolved change: the previous mode is the safe choice. The
    /// countdown is <see cref="IPendingDisplayChange.SecondsRemaining"/> rounded up, so it shows the full duration first and
    /// 1 during the last second.
    /// </summary>
    internal sealed class DisplayConfirmPresenter
    {
        private readonly IDisplayConfirmView _view;
        private bool _shown;

        internal DisplayConfirmPresenter(IDisplayConfirmView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>The change waiting for the player; null when none.</summary>
        internal IPendingDisplayChange Current { get; private set; }

        /// <summary>True while the view is shown.</summary>
        internal bool IsShown => _shown;

        /// <summary>Whole seconds left before the automatic revert, rounded up; 0 when nothing is pending.</summary>
        internal int SecondsLeft => Current == null ? 0 : Mathf.CeilToInt(Mathf.Max(0f, Current.SecondsRemaining));

        /// <summary>A changed mode is visible and waits for confirmation (<c>DisplayChangePendingEvent</c>).</summary>
        internal void OnPending(IPendingDisplayChange change)
        {
            if (change == null || change.IsResolved)
            {
                return;
            }

            Current = change;
            if (!_shown)
            {
                _shown = true;
                _view.Show(this);
            }
        }

        /// <summary>A change was resolved (<c>DisplayChangeResolvedEvent</c>).</summary>
        internal void OnResolved(IPendingDisplayChange change)
        {
            if (change == null || change != Current)
            {
                return;
            }

            Current = null;
            if (_shown)
            {
                _shown = false;
                _view.Hide();
            }
        }

        /// <summary>Keep: confirms and saves the shown mode.</summary>
        internal void Keep()
        {
            IPendingDisplayChange change = Current;
            change?.Confirm();
        }

        /// <summary>Revert: restores the previous mode.</summary>
        internal void Revert()
        {
            IPendingDisplayChange change = Current;
            change?.Revert();
        }

        /// <summary>The popup was hidden (by <see cref="OnResolved"/> or from outside); an unresolved change reverts.</summary>
        internal void OnViewClosed()
        {
            _shown = false;
            IPendingDisplayChange change = Current;
            if (change != null && !change.IsResolved)
            {
                change.Revert();
            }
        }
    }
}
