using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One run of the game (pure logic): the board and the battle together. A board action resolves the board at once; its
    /// cascade steps then reach the battle one by one as each pops (<see cref="ApplySteps"/>), while the battle keeps
    /// running. <see cref="ApplyPending"/> applies whatever is left (the replay ended, was skipped or animations are off), and
    /// the next board action does it first, so no step is ever lost or applied twice.
    /// </summary>
    internal sealed class Run
    {
        private BoardResolution _pending;
        private int _applied;

        internal Run(Match3Engine engine, Battle battle)
        {
            Engine = engine;
            Battle = battle;
        }

        internal Match3Engine Engine { get; }

        internal Battle Battle { get; }

        internal Board Board => Engine.Board;

        internal bool IsOver => Battle.IsOver;

        /// <summary>True while cascade steps of the last board action have not reached the battle yet.</summary>
        internal bool HasPendingSteps => _pending != null && _applied < _pending.Steps.Count;

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

        /// <summary>Applies the pending cascade steps up to and including step <paramref name="index"/> to the battle.</summary>
        internal void ApplySteps(int index, List<BattleEvent> events)
        {
            while (HasPendingSteps && _pending.Steps[_applied].Index <= index)
            {
                Battle.ApplyStep(_pending.Steps[_applied++], events);
            }
        }

        /// <summary>Applies every pending cascade step to the battle.</summary>
        internal void ApplyPending(List<BattleEvent> events)
        {
            ApplySteps(int.MaxValue, events);
        }

        // The board is resolved now; its steps wait for ApplySteps. Steps of the previous action left pending apply first.
        private RunStep Apply(BoardResolution resolution)
        {
            var events = new List<BattleEvent>();
            if (resolution.Valid)
            {
                ApplyPending(events);
                _pending = resolution;
                _applied = 0;
            }

            return new RunStep(resolution, events);
        }

        private static RunStep Nothing()
        {
            return new RunStep(null, new List<BattleEvent>());
        }
    }
}
