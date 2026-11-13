using System;

namespace Game.Systems.Gameplay
{
    /// <summary>A board position: row 0 is the bottom row (gems fall towards it), column 0 the left column.</summary>
    internal readonly struct Cell : IEquatable<Cell>
    {
        internal Cell(int row, int column)
        {
            Row = row;
            Column = column;
        }

        internal int Row { get; }

        internal int Column { get; }

        /// <summary>True when the two cells share an edge.</summary>
        internal bool IsAdjacent(Cell other)
        {
            return Math.Abs(Row - other.Row) + Math.Abs(Column - other.Column) == 1;
        }

        public bool Equals(Cell other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is Cell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Row * 397 ^ Column;
        }

        public override string ToString()
        {
            return $"({Row},{Column})";
        }
    }
}
