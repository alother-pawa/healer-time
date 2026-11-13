using System;
using System.Collections.Generic;
using System.Linq;
using Game.Contracts.Roster;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;

namespace Game.Systems.Roster
{
    /// <summary>
    /// Default <see cref="IRoster"/>: the configured lists, with the chosen ids kept in Core's global store (for the session
    /// only without one). A saved id that no longer exists falls back to the first character of its list.
    /// </summary>
    internal sealed class RosterService : IRoster
    {
        internal const string HeroKey = "game.roster.hero";
        internal const string HealerKey = "game.roster.healer";

        private readonly HeroDefinition[] _heroes;
        private readonly HealerDefinition[] _healers;
        private readonly ISaveStore _store;
        private readonly IEventBus _events;

        internal RosterService(IEnumerable<HeroDefinition> heroes, IEnumerable<HealerDefinition> healers, ISaveStore store, IEventBus events)
        {
            _heroes = heroes.Where(hero => hero != null).ToArray();
            _healers = healers.Where(healer => healer != null).ToArray();
            if (_heroes.Length == 0 || _healers.Length == 0)
            {
                throw new ArgumentException("The roster needs at least one hero and one healer.");
            }

            _store = store;
            _events = events;
            SelectedHero = Find(_heroes, store != null ? store.Get(HeroKey, string.Empty) : string.Empty);
            SelectedHealer = Find(_healers, store != null ? store.Get(HealerKey, string.Empty) : string.Empty);
        }

        public IReadOnlyList<HeroDefinition> Heroes => _heroes;

        public IReadOnlyList<HealerDefinition> Healers => _healers;

        public HeroDefinition SelectedHero { get; private set; }

        public HealerDefinition SelectedHealer { get; private set; }

        public void Select(HeroDefinition hero)
        {
            if (hero == null || hero == SelectedHero || Array.IndexOf(_heroes, hero) < 0)
            {
                return;
            }

            SelectedHero = hero;
            _store?.Set(HeroKey, hero.Id);
            Publish();
        }

        public void Select(HealerDefinition healer)
        {
            if (healer == null || healer == SelectedHealer || Array.IndexOf(_healers, healer) < 0)
            {
                return;
            }

            SelectedHealer = healer;
            _store?.Set(HealerKey, healer.Id);
            Publish();
        }

        private void Publish()
        {
            _events?.Publish(new RosterSelectionChangedEvent(SelectedHero, SelectedHealer));
        }

        private static T Find<T>(T[] list, string id) where T : CharacterDefinition
        {
            foreach (T character in list)
            {
                if (character.Id == id)
                {
                    return character;
                }
            }

            return list[0];
        }
    }
}
