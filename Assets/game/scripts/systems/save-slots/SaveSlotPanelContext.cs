using System;
using Paowa.Save;

namespace Game.Systems.SaveSlots
{
    /// <summary>What the slot panel needs from the module, passed as the popup's payload.</summary>
    internal sealed class SaveSlotPanelContext
    {
        internal SaveSlotPanelContext(ISaveSlotService save, SlotSaveWriter writer, SaveSlotsData data, Action enterGameplay)
        {
            Save = save ?? throw new ArgumentNullException(nameof(save));
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Data = data ?? throw new ArgumentNullException(nameof(data));
            EnterGameplay = enterGameplay ?? throw new ArgumentNullException(nameof(enterGameplay));
        }

        internal ISaveSlotService Save { get; }

        internal SlotSaveWriter Writer { get; }

        internal SaveSlotsData Data { get; }

        /// <summary>Hides the panel and switches to the gameplay scene.</summary>
        internal Action EnterGameplay { get; }
    }
}
