using System;
using Game.Contracts.Presentation;
using Game.Contracts.Save;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Saving and leaving from the pause menu (pure logic; one action at a time). Quit to main menu / Quit to desktop: ask
    /// (Cancel is the safe choice) → stop playtime → save the current slot through <see cref="ICheckpoint.Save"/> → only then
    /// leave. A failed save keeps the player in the game with a notice (the progress is still in memory); a game without a
    /// slot (its files were deleted elsewhere) asks "Leave without saving?" and never leaves silently. Whenever the player
    /// stays, playtime counts again (it still does not advance while paused). Without the confirmation dialog the quit
    /// question counts as yes (the player pressed the button) and the unsaved question as no (with the no-slot notice).
    /// </summary>
    internal sealed class GameplayExitFlow
    {
        private readonly ICheckpoint _checkpoint;
        private readonly IPlaytime _playtime;
        private readonly Func<string> _currentSlot;
        private readonly Func<ConfirmRequest, Awaitable<bool>> _confirm;
        private readonly Action<string> _notify;
        private readonly GameplayExitKeys _keys;

        /// <param name="checkpoint">Saves the slot; null: nothing can be saved (leaving asks as without a slot).</param>
        /// <param name="playtime">Stopped before saving, started again when staying; may be null.</param>
        /// <param name="currentSlot">Current slot id or null.</param>
        /// <param name="confirm">Asks a question; null when there is no dialog.</param>
        /// <param name="notify">Shows a notice from a localization key (toast); may be null.</param>
        /// <param name="keys">Texts of the questions and notices.</param>
        internal GameplayExitFlow(ICheckpoint checkpoint, IPlaytime playtime, Func<string> currentSlot,
            Func<ConfirmRequest, Awaitable<bool>> confirm, Action<string> notify, GameplayExitKeys keys)
        {
            _checkpoint = checkpoint;
            _playtime = playtime;
            _currentSlot = currentSlot;
            _confirm = confirm;
            _notify = notify;
            _keys = keys;
        }

        /// <summary>True while a quit is being asked or carried out.</summary>
        internal bool IsBusy { get; private set; }

        /// <summary>Save button: saves the current slot (no new thumbnail) and tells the player how it went.</summary>
        internal bool SaveNow()
        {
            if (IsBusy)
            {
                return false;
            }

            if (_checkpoint == null || _currentSlot() == null)
            {
                Notify(_keys.NoSlot);
                return false;
            }

            bool saved = _checkpoint.Save();
            Notify(saved ? _keys.Saved : _keys.SaveFailed);
            return saved;
        }

        /// <summary>
        /// Quit to main menu: asks, saves, then runs <paramref name="leave"/> (hide the menu, switch scene), which completes
        /// true when the switch started. Completes true when the game left.
        /// </summary>
        internal Awaitable<bool> QuitToMenuAsync(Func<Awaitable<bool>> leave)
        {
            return RunAsync(new ConfirmRequest(_keys.QuitToMenuTitle, _keys.QuitToMenuMessage, _keys.QuitToMenuConfirm, _keys.Cancel),
                leave);
        }

        /// <summary>Quit to desktop: asks, saves, then runs <paramref name="quit"/>. Completes true when it quit.</summary>
        internal Awaitable<bool> QuitToDesktopAsync(Action quit)
        {
            return RunAsync(new ConfirmRequest(_keys.QuitToDesktopTitle, _keys.QuitToDesktopMessage, _keys.QuitToDesktopConfirm, _keys.Cancel),
                () =>
                {
                    quit();
                    return Completed(true);
                });
        }

        private async Awaitable<bool> RunAsync(ConfirmRequest question, Func<Awaitable<bool>> leave)
        {
            if (IsBusy)
            {
                return false;
            }

            IsBusy = true;
            bool left = false;
            bool stopped = false;
            try
            {
                if (_confirm != null && !await _confirm(question))
                {
                    return false;
                }

                if (_playtime != null)
                {
                    _playtime.StopCounting();
                }

                stopped = true;
                if (!await SaveOrConfirmLeavingAsync())
                {
                    return false;
                }

                left = await leave();
                return left;
            }
            finally
            {
                IsBusy = false;
                if (stopped && !left && _playtime != null)
                {
                    _playtime.StartCounting();
                }
            }
        }

        // True when the progress is saved, or the game has no slot and the player chose to leave anyway.
        private async Awaitable<bool> SaveOrConfirmLeavingAsync()
        {
            if (_checkpoint != null && _currentSlot() != null)
            {
                if (_checkpoint.Save())
                {
                    return true;
                }

                Notify(_keys.SaveFailed);
                return false;
            }

            if (_confirm == null)
            {
                // Without a dialog nobody can agree to lose the progress: stay, and say why.
                Notify(_keys.NoSlot);
                return false;
            }

            return await _confirm(new ConfirmRequest(_keys.UnsavedTitle, _keys.UnsavedMessage, _keys.UnsavedConfirm, _keys.Cancel));
        }

        private void Notify(string key)
        {
            if (_notify != null && !string.IsNullOrEmpty(key))
            {
                _notify(key);
            }
        }

        private static Awaitable<bool> Completed(bool value)
        {
            var source = new AwaitableCompletionSource<bool>();
            source.SetResult(value);
            return source.Awaitable;
        }
    }
}
