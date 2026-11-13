using Paowa.Core.Messaging;

namespace Game.Contracts.Roster
{
    /// <summary>Published when the selected hero or healer changes.</summary>
    public readonly struct RosterSelectionChangedEvent : IEvent
    {
        public RosterSelectionChangedEvent(HeroDefinition hero, HealerDefinition healer)
        {
            Hero = hero;
            Healer = healer;
        }

        public HeroDefinition Hero { get; }

        public HealerDefinition Healer { get; }
    }
}
