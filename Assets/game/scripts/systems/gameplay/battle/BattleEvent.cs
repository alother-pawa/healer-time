namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One thing that happened in battle, with the values right after it (<see cref="After"/>), so a view can replay the
    /// events one by one and show each step's numbers, then end exactly on the battle's state.
    /// </summary>
    internal readonly struct BattleEvent
    {
        internal BattleEvent(BattleEventKind kind, float amount, float absorbed, int value, bool boss, int gem, BattleSnapshot after)
        {
            Kind = kind;
            Amount = amount;
            Absorbed = absorbed;
            Value = value;
            Boss = boss;
            Gem = gem;
            After = after;
        }

        internal BattleEventKind Kind { get; }

        internal float Amount { get; }

        internal float Absorbed { get; }

        internal int Value { get; }

        internal bool Boss { get; }

        /// <summary>The gem kind whose match caused the event, or -1 (time, items, revive).</summary>
        internal int Gem { get; }

        internal BattleSnapshot After { get; }
    }
}
