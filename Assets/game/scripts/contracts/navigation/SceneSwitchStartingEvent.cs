using Paowa.Core.Messaging;

namespace Game.Contracts.Navigation
{
    /// <summary>
    /// Published by <see cref="IGameNavigator.GoToAsync"/> right before the scene switch starts (before the loading screen
    /// covers), so the transition can present itself for the destination.
    /// </summary>
    public readonly struct SceneSwitchStartingEvent : IEvent
    {
        /// <summary>Creates the event.</summary>
        public SceneSwitchStartingEvent(SceneKey scene)
        {
            Scene = scene;
        }

        /// <summary>Destination of the switch.</summary>
        public SceneKey Scene { get; }
    }
}
