using System;
using Paowa.Save;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// Everything one slot card shows and allows, derived from Core's slot listing (<c>TryGetSlotInfo</c>, no slot data
    /// loaded) and the outcome of the last failed Continue. Pure data: the card view draws it and the panel links navigation
    /// from it, so the same state always gives the same card.
    /// </summary>
    internal sealed class SlotCardModel
    {
        private const int SecondsPerMinute = 60;
        private const int MinutesPerHour = 60;

        private SlotCardModel(string slotId, int number, SlotCardState state, SlotCard card, DateTime lastSavedUtc,
            SaveLoadStatus loadFailure)
        {
            SlotId = slotId;
            Number = number;
            State = state;
            Card = card;
            LastSavedUtc = lastSavedUtc;
            LoadFailure = loadFailure;
        }

        /// <summary>Core slot id.</summary>
        internal string SlotId { get; }

        /// <summary>1-based position shown to the player ("Slot 2").</summary>
        internal int Number { get; }

        internal SlotCardState State { get; }

        /// <summary>Card metadata (an empty card when the slot has none or it is unreadable).</summary>
        internal SlotCard Card { get; }

        /// <summary>Last save time, UTC (default for an empty slot).</summary>
        internal DateTime LastSavedUtc { get; }

        /// <summary>Why the last Continue failed, for <see cref="SlotCardState.LoadFailed"/>; <c>Loaded</c> otherwise.</summary>
        internal SaveLoadStatus LoadFailure { get; }

        internal bool CanContinue => State == SlotCardState.Saved || State == SlotCardState.Unlabeled;

        /// <summary>New game on an empty slot, or over a healthy one after a confirmation.</summary>
        internal bool CanNewGame => State == SlotCardState.Empty || State == SlotCardState.Saved;

        /// <summary>Every used slot can be deleted (a newer version's slot after a stronger confirmation).</summary>
        internal bool CanDelete => State != SlotCardState.Empty;

        /// <summary>True when the slot has files (New game asks before overwriting).</summary>
        internal bool IsUsed => State != SlotCardState.Empty;

        /// <summary>
        /// Maps a slot to its card. <paramref name="exists"/> and <paramref name="info"/> come from <c>TryGetSlotInfo</c>;
        /// <paramref name="lastFailure"/> is the status of the last failed Continue of this slot (<c>Loaded</c> when none).
        /// Order: no files → Empty; no data copy → Damaged; data from a newer version (listed, or reported by the last load) →
        /// NeedsNewerVersion; any other failed load → LoadFailed; unreadable metadata → Unlabeled; otherwise Saved.
        /// </summary>
        internal static SlotCardModel Describe(string slotId, int number, bool exists, SaveSlotInfo info, int gameDataVersion,
            SaveLoadStatus lastFailure)
        {
            if (!exists)
            {
                return new SlotCardModel(slotId, number, SlotCardState.Empty, new SlotCard(), default, SaveLoadStatus.Loaded);
            }

            SlotCard.TryParse(info.Extra, out SlotCard card);
            SlotCardState state;
            if (!info.HasData)
            {
                state = SlotCardState.Damaged;
            }
            else if (lastFailure == SaveLoadStatus.NewerDataVersion || info.DataVersion > gameDataVersion)
            {
                state = SlotCardState.NeedsNewerVersion;
            }
            else if (lastFailure != SaveLoadStatus.Loaded)
            {
                state = SlotCardState.LoadFailed;
            }
            else if (info.IsCorrupted)
            {
                state = SlotCardState.Unlabeled;
            }
            else
            {
                state = SlotCardState.Saved;
            }

            return new SlotCardModel(slotId, number, state, card, info.LastSavedUtc,
                state == SlotCardState.LoadFailed ? lastFailure : SaveLoadStatus.Loaded);
        }

        /// <summary>Playtime as hours:minutes ("0:05", "12:40", "130:00"); whole minutes, rounded down.</summary>
        internal static string FormatPlaytime(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0d)
            {
                seconds = 0d;
            }

            long minutes = (long)Math.Min(Math.Floor(seconds / SecondsPerMinute), long.MaxValue / 2);
            return $"{minutes / MinutesPerHour}:{minutes % MinutesPerHour:00}";
        }
    }
}
