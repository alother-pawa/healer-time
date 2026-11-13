using UnityEngine;

namespace Game.Contracts.Save
{
    /// <summary>
    /// Saves the game being played into its slot. Every save first writes the slot card (playtime and the location label
    /// shown on the save slot panel), so gameplay saves through this service instead of calling the save service directly.
    /// Failures never throw: the methods return false and the progress stays in memory.
    /// </summary>
    public interface ICheckpoint
    {
        /// <summary>
        /// Checkpoint: stores <paramref name="labelKey"/> (localization key of the chapter or location shown on the slot
        /// card) and saves the current slot at once, then captures a thumbnail at the end of this frame and keeps it with the
        /// slot. Await it before showing any pause or menu UI, so the thumbnail shows the game. Completes true when the
        /// progress was saved (a missing thumbnail does not fail it). Without a current slot the progress goes to Core's
        /// shared autosave slot, which the panel does not list.
        /// </summary>
        Awaitable<bool> SaveCheckpointAsync(string labelKey);

        /// <summary>
        /// Saves the current slot with the last location label and no new thumbnail (leaving gameplay, quitting). False when
        /// there is no current slot or writing failed.
        /// </summary>
        bool Save();
    }
}
