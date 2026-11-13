namespace Game.Systems.SaveSlots
{
    /// <summary>What a slot card shows, decided from the slot's files and the last load attempt.</summary>
    internal enum SlotCardState
    {
        /// <summary>No files: New game.</summary>
        Empty = 0,

        /// <summary>Healthy slot: thumbnail, label, playtime and save time; Continue, New game (overwrites), Delete.</summary>
        Saved = 1,

        /// <summary>Metadata unreadable but data present: Continue (the next save writes the card again) and Delete.</summary>
        Unlabeled = 2,

        /// <summary>Metadata without any data copy: Delete only.</summary>
        Damaged = 3,

        /// <summary>
        /// Written by a newer game data version: never loaded or overwritten by this build; Delete only, after a stronger
        /// confirmation.
        /// </summary>
        NeedsNewerVersion = 4,

        /// <summary>
        /// The last Continue failed: unreadable even from Core's <c>.tmp</c>/<c>.bak</c> copies, or a migration step failed.
        /// Delete only: restoring a backup would loop on the same damage, or overwrite newer data at the next save.
        /// </summary>
        LoadFailed = 5
    }
}
