namespace Game.Systems.Gameplay
{
    /// <summary>Whether a board still has a swap that makes a match, and which one (for a hint).</summary>
    internal static class MoveFinder
    {
        internal static bool HasMove(Board board)
        {
            return TryFind(board, out _, out _);
        }

        /// <summary>The first swap (scanning from the bottom-left) that makes a match.</summary>
        internal static bool TryFind(Board board, out Cell a, out Cell b)
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int column = 0; column < board.Columns; column++)
                {
                    a = new Cell(row, column);
                    if (column + 1 < board.Columns && Makes(board, a, b = new Cell(row, column + 1)))
                    {
                        return true;
                    }

                    if (row + 1 < board.Rows && Makes(board, a, b = new Cell(row + 1, column)))
                    {
                        return true;
                    }
                }
            }

            a = default;
            b = default;
            return false;
        }

        /// <summary>True when swapping the two adjacent cells puts either gem in a run (the board is restored).</summary>
        internal static bool Makes(Board board, Cell a, Cell b)
        {
            if (board[a] == board[b] || board[a] == Board.Empty || board[b] == Board.Empty)
            {
                return false;
            }

            Swap(board, a, b);
            bool makes = MatchFinder.IsInRun(board, a) || MatchFinder.IsInRun(board, b);
            Swap(board, a, b);
            return makes;
        }

        internal static void Swap(Board board, Cell a, Cell b)
        {
            int gem = board[a];
            board[a] = board[b];
            board[b] = gem;
        }
    }
}
