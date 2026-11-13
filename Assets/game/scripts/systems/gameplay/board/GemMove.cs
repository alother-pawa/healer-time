namespace Game.Systems.Gameplay
{
    /// <summary>
    /// A gem going from one place to another: a fall, a shuffle move, or a new gem dropping in. A new gem has
    /// <see cref="IsNew"/> set and <see cref="From"/> above the board (row ≥ rows), stacked in the order it drops.
    /// </summary>
    internal readonly struct GemMove
    {
        internal GemMove(Cell from, Cell to, int kind, bool isNew)
        {
            From = from;
            To = to;
            Kind = kind;
            IsNew = isNew;
        }

        internal Cell From { get; }

        internal Cell To { get; }

        internal int Kind { get; }

        internal bool IsNew { get; }
    }
}
