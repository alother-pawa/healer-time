using Paowa.Core.Messaging;

namespace Game.Contracts.Settings
{
    /// <summary>Published when the committed UI scale changes (<see cref="IGameSettings.UIScale"/>).</summary>
    public readonly struct UIScaleChangedEvent : IEvent
    {
        /// <summary>Creates the event.</summary>
        public UIScaleChangedEvent(float scale)
        {
            Scale = scale;
        }

        /// <summary>New scale, 1 = reference size.</summary>
        public float Scale { get; }
    }
}
