using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>What a matched gem kind does to the battle, for one move's total amount of that kind.</summary>
    internal interface IGemEffect
    {
        /// <param name="battle">The battle to change.</param>
        /// <param name="gem">The gem kind (for the events).</param>
        /// <param name="amount">Matched power of the kind times the gem's power.</param>
        /// <param name="events">Receives what happened.</param>
        void Apply(Battle battle, int gem, float amount, List<BattleEvent> events);
    }
}
