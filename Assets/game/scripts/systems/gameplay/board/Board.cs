using System;
using System.Text;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The grid of gems: each cell holds a gem kind (index into the game's gem list) or <see cref="Empty"/>. Pure data;
    /// <see cref="Match3Engine"/> changes it.
    /// </summary>
    internal sealed class Board
    {
        /// <summary>A cell without a gem (only between clearing and refilling).</summary>
        internal const int Empty = -1;

        private readonly int[,] _gems;

        internal Board(int rows, int columns)
        {
            if (rows < 3 || columns < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), "A board needs at least 3 rows and 3 columns.");
            }

            Rows = rows;
            Columns = columns;
            _gems = new int[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    _gems[row, column] = Empty;
                }
            }
        }

        internal int Rows { get; }

        internal int Columns { get; }

        internal int this[Cell cell]
        {
            get => _gems[cell.Row, cell.Column];
            set => _gems[cell.Row, cell.Column] = value;
        }

        internal int this[int row, int column]
        {
            get => _gems[row, column];
            set => _gems[row, column] = value;
        }

        internal bool Contains(Cell cell)
        {
            return cell.Row >= 0 && cell.Row < Rows && cell.Column >= 0 && cell.Column < Columns;
        }

        internal Board Clone()
        {
            var copy = new Board(Rows, Columns);
            Array.Copy(_gems, copy._gems, _gems.Length);
            return copy;
        }

        /// <summary>Rows top to bottom, one character per gem ('.' for empty); for tests and logs.</summary>
        public override string ToString()
        {
            var text = new StringBuilder();
            for (int row = Rows - 1; row >= 0; row--)
            {
                for (int column = 0; column < Columns; column++)
                {
                    int gem = _gems[row, column];
                    text.Append(gem == Empty ? '.' : (char)('0' + gem));
                }

                if (row > 0)
                {
                    text.Append('\n');
                }
            }

            return text.ToString();
        }

        /// <summary>A board from rows written top to bottom, one digit per gem ('.' for empty); for tests.</summary>
        internal static Board Parse(params string[] rowsTopToBottom)
        {
            var board = new Board(rowsTopToBottom.Length, rowsTopToBottom[0].Length);
            for (int i = 0; i < rowsTopToBottom.Length; i++)
            {
                int row = rowsTopToBottom.Length - 1 - i;
                for (int column = 0; column < board.Columns; column++)
                {
                    char c = rowsTopToBottom[i][column];
                    board[row, column] = c == '.' ? Empty : c - '0';
                }
            }

            return board;
        }
    }
}
