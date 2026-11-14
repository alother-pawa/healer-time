using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The result of one player action in a run: what the board did (its steps reach the battle as they pop, see
    /// <see cref="Run.ApplySteps"/>) and what the battle did at once (an item's heal or armor).
    /// </summary>
    internal sealed class RunStep
    {
        internal RunStep(BoardResolution board, List<BattleEvent> events)
        {
            Board = board;
            Events = events;
        }

        /// <summary>The board's changes; null when the action did not touch the board (a potion).</summary>
        internal BoardResolution Board { get; }

        internal List<BattleEvent> Events { get; }

        /// <summary>False when nothing happened (a swap without a match, an item that could not be used).</summary>
        internal bool Done => Board != null ? Board.Valid : Events.Count > 0;
    }
}
