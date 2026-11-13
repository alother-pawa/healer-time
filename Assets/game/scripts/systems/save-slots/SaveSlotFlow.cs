using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Paowa.Save;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// What the slot panel does, without any UI: describes each card from Core's listing (<see cref="SlotCardModel"/>) and
    /// runs Continue, New game and Delete with their confirmations. One action at a time. The order of writes never leaves a
    /// half-made slot: New game over a used slot deletes it first and stops when that fails (the old game stays whole); a
    /// first save that fails deletes what it wrote and empties the session. A failed Continue is remembered per slot (the
    /// card then offers Delete only: Core already tried the slot's <c>.tmp</c> and <c>.bak</c> copies, and restoring after a
    /// failed migration would let the next save overwrite the newer data) and unbinds the session from that slot, so nothing
    /// written later can overwrite it. Deleting a slot of a newer game version asks with a stronger message. <see cref="Changed"/> asks the panel to redraw; the save package events do too.
    /// </summary>
    internal sealed class SaveSlotFlow
    {
        private readonly ISaveSlotService _save;
        private readonly SlotSaveWriter _writer;
        private readonly IReadOnlyList<string> _slotIds;
        private readonly int _dataVersion;
        private readonly string _newGameLabelKey;
        private readonly Func<ConfirmRequest, Awaitable<bool>> _confirm;
        private readonly SaveSlotDialogKeys _keys;
        private readonly Action _enterGameplay;
        private readonly Dictionary<string, SaveLoadStatus> _failures = new Dictionary<string, SaveLoadStatus>();

        /// <param name="confirm">Shows a confirmation or a single-button notice and completes with the answer.</param>
        /// <param name="enterGameplay">Called once a new game was created or a slot loaded.</param>
        internal SaveSlotFlow(ISaveSlotService save, SlotSaveWriter writer, IReadOnlyList<string> slotIds, int dataVersion,
            string newGameLabelKey, Func<ConfirmRequest, Awaitable<bool>> confirm, SaveSlotDialogKeys keys, Action enterGameplay)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _slotIds = slotIds ?? throw new ArgumentNullException(nameof(slotIds));
            _dataVersion = dataVersion;
            _newGameLabelKey = newGameLabelKey ?? string.Empty;
            _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _enterGameplay = enterGameplay ?? throw new ArgumentNullException(nameof(enterGameplay));
        }

        /// <summary>A card's state changed through an action (also after a failed one).</summary>
        internal event Action Changed;

        internal int Count => _slotIds.Count;

        /// <summary>True while an action runs (including its confirmation or notice).</summary>
        internal bool IsBusy { get; private set; }

        /// <summary>The card of slot <paramref name="index"/>, read from Core now.</summary>
        internal SlotCardModel Describe(int index)
        {
            string slotId = _slotIds[index];
            bool exists;
            SaveSlotInfo info;
            try
            {
                exists = _save.TryGetSlotInfo(slotId, out info);
            }
            catch (Exception exception)
            {
                // Listed as files without data: the card offers Delete only.
                Debug.LogWarning($"[Game] Save slot '{slotId}' could not be listed: {exception.Message}");
                exists = true;
                info = default;
            }

            _failures.TryGetValue(slotId, out SaveLoadStatus failure);
            return SlotCardModel.Describe(slotId, index + 1, exists, info, _dataVersion, failure);
        }

        /// <summary>
        /// The card to start on: the most recently saved slot that can continue, else the first empty one, else the first
        /// with any action; -1 when no card has an action.
        /// </summary>
        internal static int PreferredIndex(IReadOnlyList<SlotCardModel> cards)
        {
            int best = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].CanContinue && (best < 0 || cards[i].LastSavedUtc > cards[best].LastSavedUtc))
                {
                    best = i;
                }
            }

            for (int i = 0; best < 0 && i < cards.Count; i++)
            {
                if (cards[i].State == SlotCardState.Empty)
                {
                    best = i;
                }
            }

            for (int i = 0; best < 0 && i < cards.Count; i++)
            {
                if (cards[i].CanContinue || cards[i].CanNewGame || cards[i].CanDelete)
                {
                    best = i;
                }
            }

            return best;
        }

        internal async Awaitable ContinueAsync(int index)
        {
            if (!TryBegin())
            {
                return;
            }

            try
            {
                SlotCardModel card = Describe(index);
                if (!card.CanContinue)
                {
                    return;
                }

                bool loaded;
                SaveLoadResult result;
                try
                {
                    loaded = _save.LoadSlot(card.SlotId, out result);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Game] Save slot '{card.SlotId}' could not be loaded: {exception.Message}");
                    loaded = false;
                    result = new SaveLoadResult(SaveLoadStatus.Unreadable, exception.Message);
                }

                if (loaded)
                {
                    _failures.Remove(card.SlotId);
                    _enterGameplay();
                    return;
                }

                SaveLoadStatus status = result.Status == SaveLoadStatus.Loaded ? SaveLoadStatus.Unreadable : result.Status;
                _failures[card.SlotId] = status;
                if (_save.CurrentSlotId == card.SlotId)
                {
                    // Core binds the session to a slot whose unreadable file it moved aside; a later save must not write there.
                    _writer.StartNewGame(string.Empty);
                }

                Changed?.Invoke();
                await Notify(LoadFailureMessage(status));
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal async Awaitable NewGameAsync(int index)
        {
            if (!TryBegin())
            {
                return;
            }

            try
            {
                SlotCardModel card = Describe(index);
                if (!card.CanNewGame)
                {
                    return;
                }

                if (card.IsUsed)
                {
                    if (!await _confirm(new ConfirmRequest(_keys.OverwriteTitle, _keys.OverwriteMessage, _keys.OverwriteConfirm,
                            _keys.Cancel, card.Number)))
                    {
                        return;
                    }

                    // Deleting first drops the old game's thumbnail and backups; when it fails the old game stays whole.
                    if (!DeleteFiles(card.SlotId))
                    {
                        Changed?.Invoke();
                        await Notify(_keys.DeleteFailedMessage);
                        return;
                    }

                    _failures.Remove(card.SlotId);
                }

                _writer.StartNewGame(_newGameLabelKey);
                if (!_writer.SaveAs(card.SlotId))
                {
                    // A failed first save can leave data without metadata: remove it and leave nothing bound to the slot.
                    DeleteFiles(card.SlotId);
                    _writer.StartNewGame(string.Empty);
                    Changed?.Invoke();
                    await Notify(_keys.CreateFailedMessage);
                    return;
                }

                _enterGameplay();
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal async Awaitable DeleteAsync(int index)
        {
            if (!TryBegin())
            {
                return;
            }

            try
            {
                SlotCardModel card = Describe(index);
                string message = card.State == SlotCardState.NeedsNewerVersion ? _keys.DeleteNewerMessage : _keys.DeleteMessage;
                if (!card.CanDelete || !await _confirm(new ConfirmRequest(_keys.DeleteTitle, message, _keys.DeleteConfirm,
                        _keys.Cancel, card.Number)))
                {
                    return;
                }

                bool deleted = DeleteFiles(card.SlotId);
                if (deleted)
                {
                    _failures.Remove(card.SlotId);
                }

                Changed?.Invoke();
                if (!deleted)
                {
                    await Notify(_keys.DeleteFailedMessage);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool TryBegin()
        {
            if (IsBusy)
            {
                return false;
            }

            IsBusy = true;
            return true;
        }

        // True when the slot has no files afterwards (also when it had none).
        private bool DeleteFiles(string slotId)
        {
            try
            {
                _save.DeleteSlot(slotId);
                return !_save.TryGetSlotInfo(slotId, out _);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] Save slot '{slotId}' could not be deleted: {exception.Message}");
                return false;
            }
        }

        private string LoadFailureMessage(SaveLoadStatus status)
        {
            switch (status)
            {
                case SaveLoadStatus.NewerDataVersion:
                    return _keys.NewerVersionMessage;
                case SaveLoadStatus.MigrationFailed:
                    return _keys.MigrationFailedMessage;
                default:
                    return _keys.UnreadableMessage;
            }
        }

        private Awaitable<bool> Notify(string messageKey)
        {
            return _confirm(new ConfirmRequest(_keys.ErrorTitle, messageKey, _keys.ErrorConfirm, null));
        }
    }
}
