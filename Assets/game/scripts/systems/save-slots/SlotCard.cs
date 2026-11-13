using System;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// What a slot card shows without loading the slot: playtime and the location label key. Stored as JSON in the slot's
    /// metadata (Core's <c>SetSlotMetadata</c>, listed as <c>SaveSlotInfo.Extra</c>); the save time comes from Core. Fields
    /// added later read as their defaults from older cards.
    /// </summary>
    [Serializable]
    internal sealed class SlotCard
    {
        [SerializeField] private double _playtimeSeconds;
        [SerializeField] private string _labelKey;

        /// <summary>Used by JsonUtility.</summary>
        public SlotCard()
        {
        }

        internal SlotCard(double playtimeSeconds, string labelKey)
        {
            _playtimeSeconds = playtimeSeconds;
            _labelKey = labelKey;
        }

        /// <summary>Seconds played when the slot was saved.</summary>
        internal double PlaytimeSeconds => _playtimeSeconds;

        /// <summary>Localization key of the chapter or location; empty when none was set.</summary>
        internal string LabelKey => _labelKey ?? string.Empty;

        internal string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Reads a card written by <see cref="ToJson"/>; false (and an empty card) for null, empty or unparsable text, so a
        /// hand-edited metadata file never breaks the panel.
        /// </summary>
        internal static bool TryParse(string json, out SlotCard card)
        {
            card = new SlotCard();
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, card);
            }
            catch (Exception)
            {
                // JsonUtility reports malformed text as an ArgumentException; any failure reads as "no card".
                card = new SlotCard();
                return false;
            }

            if (double.IsNaN(card._playtimeSeconds) || double.IsInfinity(card._playtimeSeconds) || card._playtimeSeconds < 0d)
            {
                card._playtimeSeconds = 0d;
            }

            return true;
        }
    }
}
