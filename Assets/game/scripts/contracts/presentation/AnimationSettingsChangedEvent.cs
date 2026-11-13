using Paowa.Core.Messaging;

namespace Game.Contracts.Presentation
{
    /// <summary>Published when <see cref="IAnimationSettings.Enabled"/> changes.</summary>
    public readonly struct AnimationSettingsChangedEvent : IEvent
    {
        /// <summary>Creates the event.</summary>
        public AnimationSettingsChangedEvent(bool enabled)
        {
            Enabled = enabled;
        }

        /// <summary>New value of <see cref="IAnimationSettings.Enabled"/>.</summary>
        public bool Enabled { get; }
    }
}
