using Game.Contracts.Roster;
using UnityEngine;

namespace Game.Systems.Roster
{
    /// <summary>
    /// The playable characters in menu order, loaded from <c>Resources/game/config/RosterConfig</c>; the first of each list
    /// is the default choice. The builder writes the lists on every run.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Roster", fileName = "RosterConfig")]
    internal sealed class RosterConfig : ScriptableObject
    {
        internal const string ResourcePath = "game/config/RosterConfig";

        [SerializeField] private HeroDefinition[] _heroes = new HeroDefinition[0];
        [SerializeField] private HealerDefinition[] _healers = new HealerDefinition[0];

        internal HeroDefinition[] Heroes => _heroes;

        internal HealerDefinition[] Healers => _healers;
    }
}
