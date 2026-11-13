using System;
using System.Collections.Generic;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;
using Paowa.Save;

namespace Game.Tests.Editor
{
    /// <summary>
    /// In-memory <see cref="ISaveSlotService"/> following Core 0.6.2's rules, for cases real files cannot set up (a failing
    /// delete or first save, a given load outcome): slot listing with corruption flags, load outcomes per slot (an unreadable
    /// file moved aside binds the session to the slot with empty data), the writing hook published before every slot write
    /// (a save requested from it throws), save/load/delete/restore events, deletes that publish nothing for a slot without
    /// files, attachments only on existing slots, autosave into the current slot, and a restore that only swaps copies (it
    /// never makes damaged data readable). Every call that changes something is logged in order; failures can be switched on
    /// per operation.
    /// </summary>
    internal sealed class FakeSaveSlotService : ISaveSlotService
    {
        internal readonly List<string> Calls = new List<string>();
        internal readonly Dictionary<string, Slot> Slots = new Dictionary<string, Slot>();
        internal readonly List<ISaveMigrator> Migrators = new List<ISaveMigrator>();
        internal bool FailSaveAs;
        internal bool FailSaveAsLeavesData;
        internal bool FailDelete;
        internal bool FailRestore;
        internal string LastMetadata;
        internal string LastSummary;

        private readonly FakeStore _current = new FakeStore();
        private readonly IEventBus _events;
        private bool _announcing;
        private DateTime _clock = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

        internal sealed class Slot
        {
            internal bool HasData = true;
            internal bool MetadataCorrupted;
            internal bool HasBackup;
            internal int DataVersion;
            internal string Extra;
            internal string Summary;
            internal DateTime SavedUtc;
            internal SaveLoadStatus LoadStatus = SaveLoadStatus.Loaded;
            internal Dictionary<string, object> Data = new Dictionary<string, object>();
            internal readonly Dictionary<string, byte[]> Attachments = new Dictionary<string, byte[]>();
        }

        internal FakeSaveSlotService(IEventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public ISaveStore Global { get; } = new FakeStore();

        public string PendingSlotSummary => LastSummary;

        public string PendingSlotMetadata => LastMetadata;

        public ISaveStore Current => _current;

        public string CurrentSlotId { get; private set; }

        public bool HasUnsavedChanges => _current.IsDirty;

        public int DataVersion { get; set; }

        public AutoSaveTarget AutoSaveTarget { get; set; }

        internal Slot Add(string slotId, DateTime savedUtc, string extra = null)
        {
            var slot = new Slot { SavedUtc = savedUtc, Extra = extra, DataVersion = DataVersion };
            Slots[slotId] = slot;
            return slot;
        }

        /// <summary>Binds the session to a slot as if it had been loaded earlier.</summary>
        internal void BindCurrent(string slotId)
        {
            CurrentSlotId = slotId;
        }

        public bool FlushGlobal()
        {
            return true;
        }

        public IReadOnlyList<SaveSlotInfo> GetSlots()
        {
            var list = new List<SaveSlotInfo>();
            foreach (string id in Slots.Keys)
            {
                TryGetSlotInfo(id, out SaveSlotInfo info);
                list.Add(info);
            }

            return list;
        }

        public bool SlotExists(string slotId)
        {
            return Slots.TryGetValue(slotId, out Slot slot) && slot.HasData;
        }

        public void NewGame()
        {
            Calls.Add("new");
            _current.Clear();
            _current.IsDirty = false;
            CurrentSlotId = null;
            LastSummary = null;
            LastMetadata = null;
        }

        public bool LoadSlot(string slotId)
        {
            return LoadSlot(slotId, out _);
        }

        public bool LoadSlot(string slotId, out SaveLoadResult result)
        {
            if (!SlotExists(slotId))
            {
                throw new InvalidOperationException($"Save slot '{slotId}' does not exist.");
            }

            Calls.Add("load:" + slotId);
            Slot slot = Slots[slotId];
            if (slot.LoadStatus == SaveLoadStatus.Unreadable)
            {
                // The unparsable file was moved aside: Core binds the session to the slot with empty data.
                result = new SaveLoadResult(SaveLoadStatus.Unreadable, "unreadable (test)");
                _current.Clear();
                CurrentSlotId = slotId;
                return false;
            }

            if (slot.LoadStatus != SaveLoadStatus.Loaded)
            {
                result = new SaveLoadResult(slot.LoadStatus, "not loaded (test)");
                return false;
            }

            _current.Replace(slot.Data);
            CurrentSlotId = slotId;
            LastMetadata = slot.Extra;
            LastSummary = slot.Summary;
            result = new SaveLoadResult(SaveLoadStatus.Loaded, null);
            _events.Publish(new SaveSlotLoadedEvent(slotId));
            return true;
        }

        public bool SaveAs(string slotId)
        {
            Announce(slotId, false);
            Calls.Add("save:" + slotId);
            if (FailSaveAs)
            {
                if (FailSaveAsLeavesData)
                {
                    // Data written, metadata not: listed as corrupted.
                    Slots[slotId] = new Slot { MetadataCorrupted = true, SavedUtc = Tick() };
                }

                return false;
            }

            Write(slotId);
            CurrentSlotId = slotId;
            _current.IsDirty = false;
            _events.Publish(new SaveCompletedEvent(slotId, false));
            return true;
        }

        public bool Save()
        {
            if (CurrentSlotId == null)
            {
                throw new InvalidOperationException("No current slot.");
            }

            return SaveAs(CurrentSlotId);
        }

        public bool AutoSave()
        {
            return AutoSave(out _);
        }

        public bool AutoSave(out string slotId)
        {
            slotId = AutoSaveTarget == AutoSaveTarget.CurrentSlot && CurrentSlotId != null && SlotExists(CurrentSlotId)
                ? CurrentSlotId
                : SaveSlots.AutoSave;
            Announce(slotId, true);
            Calls.Add("autosave:" + slotId);
            Write(slotId);
            _events.Publish(new SaveCompletedEvent(slotId, true));
            return true;
        }

        public bool DeleteSlot(string slotId)
        {
            if (!Slots.ContainsKey(slotId))
            {
                return false;
            }

            Calls.Add("delete:" + slotId);
            if (FailDelete)
            {
                return false;
            }

            Slots.Remove(slotId);
            bool wasCurrent = CurrentSlotId == slotId;
            if (wasCurrent)
            {
                CurrentSlotId = null;
            }

            _events.Publish(new SaveSlotDeletedEvent(slotId, wasCurrent));
            return true;
        }

        public void SetSlotSummary(string summary)
        {
            LastSummary = summary;
        }

        public void SetSlotMetadata(string json)
        {
            Calls.Add("metadata");
            LastMetadata = json;
        }

        public bool TryGetSlotInfo(string slotId, out SaveSlotInfo info)
        {
            if (!SaveSlots.IsValidId(slotId))
            {
                throw new ArgumentException("Invalid slot id.", nameof(slotId));
            }

            if (!Slots.TryGetValue(slotId, out Slot slot))
            {
                info = default;
                return false;
            }

            string reason = slot.MetadataCorrupted ? "metadata unreadable (test)" : !slot.HasData ? "data missing (test)" : null;
            info = new SaveSlotInfo(slotId, false, null, slot.SavedUtc, slot.MetadataCorrupted ? null : slot.Summary, 1,
                slot.MetadataCorrupted ? null : slot.Extra, slot.MetadataCorrupted ? 0 : slot.DataVersion, slot.HasData,
                slot.HasBackup, reason);
            return true;
        }

        public void RegisterMigrator(ISaveMigrator migrator)
        {
            Migrators.Add(migrator);
        }

        public bool WriteSlotAttachment(string slotId, string name, byte[] data)
        {
            if (!SlotExists(slotId))
            {
                throw new InvalidOperationException($"Save slot '{slotId}' does not exist.");
            }

            Calls.Add($"attach:{slotId}:{name}");
            Slots[slotId].Attachments[name] = data;
            return true;
        }

        public bool TryReadSlotAttachment(string slotId, string name, out byte[] data)
        {
            data = null;
            return Slots.TryGetValue(slotId, out Slot slot) && slot.Attachments.TryGetValue(name, out data);
        }

        public bool RestoreSlotBackup(string slotId)
        {
            if (!Slots.TryGetValue(slotId, out Slot slot) || !slot.HasBackup)
            {
                return false;
            }

            Calls.Add("restore:" + slotId);
            if (FailRestore)
            {
                return false;
            }

            // Swaps copies only: damaged data stays damaged (the copy Core already tried while loading).
            _events.Publish(new SaveSlotRestoredEvent(slotId, slotId == CurrentSlotId));
            return true;
        }

        // Core's writing hook: subscribers update Current and the pending card; a save requested from them throws.
        private void Announce(string slotId, bool isAutoSave)
        {
            if (_announcing)
            {
                throw new InvalidOperationException("A slot save was requested from a SaveSlotWritingEvent subscriber.");
            }

            _announcing = true;
            try
            {
                _events.Publish(new SaveSlotWritingEvent(slotId, isAutoSave));
            }
            finally
            {
                _announcing = false;
            }
        }

        private void Write(string slotId)
        {
            bool existed = Slots.TryGetValue(slotId, out Slot previous);
            var slot = new Slot
            {
                SavedUtc = Tick(),
                Extra = LastMetadata,
                Summary = LastSummary,
                DataVersion = DataVersion,
                HasBackup = existed,
                Data = _current.Snapshot(),
            };
            if (existed)
            {
                foreach (KeyValuePair<string, byte[]> attachment in previous.Attachments)
                {
                    slot.Attachments[attachment.Key] = attachment.Value;
                }
            }

            Slots[slotId] = slot;
        }

        private DateTime Tick()
        {
            _clock = _clock.AddMinutes(1);
            return _clock;
        }

        /// <summary>Key/value store holding values as objects; a read with another type gives the default.</summary>
        internal sealed class FakeStore : ISaveStore
        {
            private Dictionary<string, object> _values = new Dictionary<string, object>();

            public bool IsDirty { get; internal set; }

            public bool Has(string key)
            {
                return _values.ContainsKey(key);
            }

            public T Get<T>(string key, T defaultValue = default)
            {
                return TryGet(key, out T value) ? value : defaultValue;
            }

            public bool TryGet<T>(string key, out T value)
            {
                if (_values.TryGetValue(key, out object stored) && stored is T typed)
                {
                    value = typed;
                    return true;
                }

                value = default;
                return false;
            }

            public void Set<T>(string key, T value)
            {
                _values[key] = value;
                IsDirty = true;
            }

            public bool Delete(string key)
            {
                IsDirty = true;
                return _values.Remove(key);
            }

            public void Clear()
            {
                _values.Clear();
                IsDirty = true;
            }

            internal Dictionary<string, object> Snapshot()
            {
                return new Dictionary<string, object>(_values);
            }

            internal void Replace(Dictionary<string, object> values)
            {
                _values = new Dictionary<string, object>(values);
                IsDirty = false;
            }
        }
    }
}
