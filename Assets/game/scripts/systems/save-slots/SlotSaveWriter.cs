using System;
using Game.Contracts.Save;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;
using Paowa.Save;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// Keeps every slot save complete, whoever calls it. On Core's <see cref="SaveSlotWritingEvent"/> (right before any slot
    /// write: <c>SaveAs</c>, <c>Save</c>, <c>AutoSave</c>) the playtime and the location label go into <c>Current</c> (the source
    /// of truth, migrated and backed up with the data) and into the slot metadata as a <see cref="SlotCard"/> (what the panel
    /// lists). On every <see cref="SaveCompletedEvent"/> of a slot the last thumbnail is written once more, so Core's backups
    /// of data and thumbnail always refer to the same save. A checkpoint then replaces it with a fresh capture, unless another
    /// save happened during the capture (the fresh one then waits for the next save) or the session ended. Implements
    /// <see cref="ICheckpoint"/>, which publishes <see cref="GameSavedEvent"/> after each successful save; nothing here throws,
    /// and the event handlers never save, load, delete or start a new game.
    /// </summary>
    internal sealed class SlotSaveWriter : ICheckpoint
    {
        /// <summary>Key of the playtime (double seconds) in the slot data.</summary>
        internal const string PlaytimeKey = "game.playtime.seconds";

        /// <summary>Key of the location label key in the slot data.</summary>
        internal const string LabelKey = "game.checkpoint.label-key";

        /// <summary>Name of the thumbnail attachment.</summary>
        internal const string ThumbnailName = "thumbnail";

        private readonly ISaveSlotService _save;
        private readonly IEventBus _events;
        private readonly PlaytimeTracker _playtime;
        private readonly Func<Awaitable<byte[]>> _captureThumbnail;
        private string _labelKey = string.Empty;
        private byte[] _thumbnail;
        private int _session;
        private int _saveCount;

        /// <param name="events">Bus the save service publishes on; the writer subscribes for the application's lifetime.</param>
        /// <param name="captureThumbnail">
        /// Captures a JPG at the end of the frame, or completes with null (no graphics device, capture failed).
        /// </param>
        internal SlotSaveWriter(ISaveSlotService save, IEventBus events, PlaytimeTracker playtime,
            Func<Awaitable<byte[]>> captureThumbnail)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _playtime = playtime ?? throw new ArgumentNullException(nameof(playtime));
            _captureThumbnail = captureThumbnail ?? throw new ArgumentNullException(nameof(captureThumbnail));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            events.Subscribe<SaveSlotWritingEvent>(_ => WriteCard());
            events.Subscribe<SaveCompletedEvent>(OnSaveCompleted);
            events.Subscribe<SaveSlotLoadedEvent>(evt => OnSlotLoaded(evt.SlotId));
            events.Subscribe<SaveSlotDeletedEvent>(evt =>
            {
                if (evt.WasCurrent)
                {
                    // A pending checkpoint thumbnail must not recreate the deleted slot's files.
                    _session++;
                }
            });
        }

        /// <summary>Location label key written with the next save.</summary>
        internal string CurrentLabelKey => _labelKey;

        /// <summary>Thumbnail rewritten with the next save; null when the game has none yet.</summary>
        internal byte[] CurrentThumbnail => _thumbnail;

        /// <summary>
        /// Empties the session for a new game (Core's <c>NewGame</c>): playtime zero, <paramref name="labelKey"/> as location,
        /// no thumbnail. Nothing is written until <see cref="SaveAs"/>.
        /// </summary>
        internal void StartNewGame(string labelKey)
        {
            _save.NewGame();
            _session++;
            _playtime.Restore(0d);
            _labelKey = labelKey ?? string.Empty;
            _thumbnail = null;
        }

        /// <summary>Writes <c>Current</c> to <paramref name="slotId"/> (the card goes with it); false on failure.</summary>
        internal bool SaveAs(string slotId)
        {
            try
            {
                return _save.SaveAs(slotId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        public bool Save()
        {
            string slotId = _save.CurrentSlotId;
            if (slotId == null || !SaveAs(slotId))
            {
                return false;
            }

            _events.Publish(new GameSavedEvent(slotId, false));
            return true;
        }

        public async Awaitable<bool> SaveCheckpointAsync(string labelKey)
        {
            _labelKey = labelKey ?? string.Empty;
            int session = _session;
            string slotId;
            try
            {
                if (!_save.AutoSave(out slotId))
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }

            _events.Publish(new GameSavedEvent(slotId, true));

            // The save already rewrote the last thumbnail (OnSaveCompleted), so the pair is complete even without a capture.
            int saves = _saveCount;
            byte[] captured = null;
            try
            {
                captured = await _captureThumbnail();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] The checkpoint thumbnail could not be captured: {exception.Message}");
            }

            // A new game, a load or a delete since the save: the thumbnail belongs to a session that is gone.
            if (session != _session || captured == null || captured.Length == 0)
            {
                return true;
            }

            _thumbnail = captured;
            // Another save during the capture: writing now would pair this image with that save's backup; the next save
            // writes it instead.
            if (saves == _saveCount)
            {
                AttachThumbnail(slotId, captured);
            }

            return true;
        }

        /// <summary>A slot was loaded: playtime and label come back from its data, the thumbnail from its attachment.</summary>
        private void OnSlotLoaded(string slotId)
        {
            _session++;
            ISaveStore data = _save.Current;
            _playtime.Restore(data.Get(PlaytimeKey, 0d));
            _labelKey = data.Get(LabelKey, string.Empty) ?? string.Empty;
            _thumbnail = null;
            try
            {
                if (_save.TryReadSlotAttachment(slotId, ThumbnailName, out byte[] jpg) && jpg != null && jpg.Length > 0)
                {
                    _thumbnail = jpg;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] The thumbnail of save slot '{slotId}' could not be read: {exception.Message}");
            }
        }

        private void OnSaveCompleted(SaveCompletedEvent evt)
        {
            if (evt.SlotId == null)
            {
                return;
            }

            _saveCount++;
            AttachThumbnail(evt.SlotId, _thumbnail);
        }

        // Runs inside the save package, right before the slot is built: only state goes in, nothing is saved from here.
        private void WriteCard()
        {
            try
            {
                ISaveStore data = _save.Current;
                data.Set(PlaytimeKey, _playtime.Seconds);
                data.Set(LabelKey, _labelKey);
                _save.SetSlotSummary(_labelKey.Length > 0 ? _labelKey : null);
                _save.SetSlotMetadata(new SlotCard(_playtime.Seconds, _labelKey).ToJson());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void AttachThumbnail(string slotId, byte[] jpg)
        {
            if (jpg == null || jpg.Length == 0)
            {
                return;
            }

            try
            {
                if (_save.SlotExists(slotId))
                {
                    _save.WriteSlotAttachment(slotId, ThumbnailName, jpg);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] The thumbnail of save slot '{slotId}' could not be written: {exception.Message}");
            }
        }
    }
}
