using Paowa.Core.Messaging;

namespace Game.Contracts.Save
{
    /// <summary>
    /// Published by <see cref="ICheckpoint"/> after it saved the game being played (checkpoint, Save button, saving before
    /// leaving), never for the first save of a new game; published only when the slot was written.
    /// </summary>
    public readonly struct GameSavedEvent : IEvent
    {
        /// <summary>Creates the event.</summary>
        public GameSavedEvent(string slotId, bool isCheckpoint)
        {
            SlotId = slotId;
            IsCheckpoint = isCheckpoint;
        }

        /// <summary>Slot that was written.</summary>
        public string SlotId { get; }

        /// <summary>True for <see cref="ICheckpoint.SaveCheckpointAsync"/>, false for <see cref="ICheckpoint.Save"/>.</summary>
        public bool IsCheckpoint { get; }
    }
}
