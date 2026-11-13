using Paowa.Core.Messaging;

namespace Game.Contracts.Settings
{
    /// <summary>
    /// Published when the committed text size multiplier changes (<see cref="IGameSettings.TextScale"/>); scaled texts
    /// resize from it.
    /// </summary>
    public readonly struct TextScaleChangedEvent : IEvent
    {
        /// <summary>Creates the event.</summary>
        public TextScaleChangedEvent(float scale)
        {
            Scale = scale;
        }

        /// <summary>The new multiplier, 1 = the size the builders set.</summary>
        public float Scale { get; }
    }
}
