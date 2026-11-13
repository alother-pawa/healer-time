using System.Collections.Generic;
using Game.Contracts.Save;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// The game's save settings: slot ids shown as cards, game data version and its migration steps, the location label of
    /// a new game and the thumbnail size. Owned by the game (the builder creates it once and never overwrites it), so a data
    /// version bump and its migrator survive a rebuild.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Save/Save Slots Data", fileName = "SaveSlotsData")]
    internal sealed class SaveSlotsData : ScriptableObject
    {
        [Tooltip("Manual slot ids, one card each, left to right: 1-32 characters of a-z, 0-9, _ or -; never 'autosave'. "
                 + "The panel layout fits three cards.")]
        [SerializeField] private string[] _slotIds = { "slot-1", "slot-2", "slot-3" };
        [Tooltip("Game data version written with every slot save. Raise it when the data shape changes and add the step "
                 + "from the previous version to Migrators.")]
        [Min(0)] [SerializeField] private int _dataVersion;
        [Tooltip("Migration steps, one per version below Data Version (any order).")]
        [SerializeField] private List<SaveMigrator> _migrators = new List<SaveMigrator>();
        [Tooltip("Localization key of the location shown on the card of a new game until its first checkpoint.")]
        [SerializeField] private string _newGameLabelKey;
        [Min(16)] [SerializeField] private int _thumbnailWidth = 320;
        [Min(9)] [SerializeField] private int _thumbnailHeight = 180;
        [Range(1, 100)] [SerializeField] private int _thumbnailQuality = 75;

        internal IReadOnlyList<string> SlotIds => _slotIds;

        internal int DataVersion => _dataVersion;

        internal IReadOnlyList<SaveMigrator> Migrators => _migrators;

        internal string NewGameLabelKey => _newGameLabelKey ?? string.Empty;

        internal int ThumbnailWidth => _thumbnailWidth;

        internal int ThumbnailHeight => _thumbnailHeight;

        internal int ThumbnailQuality => _thumbnailQuality;
    }
}
