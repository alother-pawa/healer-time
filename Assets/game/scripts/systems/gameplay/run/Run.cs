using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One run of the game (pure logic): the board and the battle together. Board actions are resolved on the board first,
    /// then their matched gems are applied to the battle, so both hold their final state when a method returns.
    /// </summary>
    internal sealed class Run
    {
        internal Run(Match3Engine engine, Battle battle)
        {
            Engine = engine;
            Battle = battle;
        }

        internal Match3Engine Engine { get; }

        internal Battle Battle { get; }

        internal Board Board => Engine.Board;

        internal bool IsOver => Battle.IsOver;

        internal RunStep Start()
        {
            var events = new List<BattleEvent>();
            Battle.Start(events);
            return new RunStep(null, events);
        }

        internal void Tick(float seconds, List<BattleEvent> events)
        {
            Battle.Tick(seconds, events);
        }

        internal RunStep Swap(Cell a, Cell b)
        {
            return IsOver ? Nothing() : Apply(Engine.TrySwap(a, b));
        }

        internal RunStep Blast(Cell center, int radius)
        {
            return IsOver ? Nothing() : Apply(Engine.Blast(center, radius));
        }

        internal RunStep Shuffle()
        {
            return IsOver ? Nothing() : new RunStep(Engine.ShuffleNow(), new List<BattleEvent>());
        }

        private RunStep Apply(BoardResolution resolution)
        {
            var events = new List<BattleEvent>();
            if (resolution.Valid)
            {
                Battle.ApplyBoard(resolution, events);
            }

            return new RunStep(resolution, events);
        }

        private static RunStep Nothing()
        {
            return new RunStep(null, new List<BattleEvent>());
        }
    }
}
