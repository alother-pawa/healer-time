using System.Collections.Generic;

namespace Game.Contracts.Roster
{
    /// <summary>
    /// The playable heroes and healers and the player's choice of each (saved per install). Every character is free to
    /// pick; choosing one applies at once and publishes <see cref="RosterSelectionChangedEvent"/>.
    /// </summary>
    public interface IRoster
    {
        IReadOnlyList<HeroDefinition> Heroes { get; }

        IReadOnlyList<HealerDefinition> Healers { get; }

        HeroDefinition SelectedHero { get; }

        HealerDefinition SelectedHealer { get; }

        void Select(HeroDefinition hero);

        void Select(HealerDefinition healer);
    }
}
