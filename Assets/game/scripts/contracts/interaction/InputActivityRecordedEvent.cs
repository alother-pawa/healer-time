using Paowa.Core.Messaging;

namespace Game.Contracts.Interaction
{
    /// <summary>Published by <see cref="IInputActivity"/> for every press and release it records, after the state is updated.</summary>
    public readonly struct InputActivityRecordedEvent : IEvent
    {
        /// <summary>Creates the event.</summary>
        public InputActivityRecordedEvent(InputActivityEntry entry)
        {
            Entry = entry;
        }

        /// <summary>The recorded entry (also the newest entry of the history).</summary>
        public InputActivityEntry Entry { get; }
    }
}
