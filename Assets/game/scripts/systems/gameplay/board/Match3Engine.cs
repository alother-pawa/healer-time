using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The match-3 rules over a <see cref="Board"/> (pure logic, no time): filling without ready-made matches, swapping two
    /// neighbours (kept only when it makes a match), clearing groups, letting gems fall and dropping new ones in, repeating
    /// while new matches form (combos), and shuffling when no move is left. Each action returns a
    /// <see cref="BoardResolution"/> describing what happened in order; the board already holds the final state when it
    /// returns, so a view can show the result at once or replay the steps.
    /// </summary>
    internal sealed class Match3Engine
    {
        private const int MaxSteps = 200;
        private const int ShuffleAttempts = 60;
        private const int FillAttempts = 60;

        private readonly GemSpawner _spawner;

        internal Match3Engine(int rows, int columns, GemSpawner spawner)
        {
            _spawner = spawner;
            Board = new Board(rows, columns);
            FillPlayable();
        }

        /// <summary>An engine over an existing board (tests); the board is used as given.</summary>
        internal Match3Engine(Board board, GemSpawner spawner)
        {
            _spawner = spawner;
            Board = board;
        }

        internal Board Board { get; }

        /// <summary>Swaps two neighbours; a swap that makes no match is undone and the resolution is invalid.</summary>
        internal BoardResolution TrySwap(Cell a, Cell b)
        {
            if (!Board.Contains(a) || !Board.Contains(b) || !a.IsAdjacent(b) || !MoveFinder.Makes(Board, a, b))
            {
                return BoardResolution.Invalid(a, b);
            }

            MoveFinder.Swap(Board, a, b);
            var steps = new List<CascadeStep>();
            Cascade(MatchFinder.Find(Board), steps);
            return Finish(true, a, b, steps);
        }

        /// <summary>
        /// Clears every gem within <paramref name="radius"/> cells of <paramref name="center"/> (a square; radius 1 is 3×3),
        /// grouped by kind, then lets the board settle like a move.
        /// </summary>
        internal BoardResolution Blast(Cell center, int radius)
        {
            if (!Board.Contains(center))
            {
                return BoardResolution.Invalid(center, center);
            }

            var byKind = new SortedDictionary<int, List<Cell>>();
            for (int row = center.Row - radius; row <= center.Row + radius; row++)
            {
                for (int column = center.Column - radius; column <= center.Column + radius; column++)
                {
                    var cell = new Cell(row, column);
                    if (!Board.Contains(cell) || Board[cell] == Board.Empty)
                    {
                        continue;
                    }

                    if (!byKind.TryGetValue(Board[cell], out List<Cell> cells))
                    {
                        byKind[Board[cell]] = cells = new List<Cell>();
                    }

                    cells.Add(cell);
                }
            }

            var groups = new List<GemGroup>();
            foreach (KeyValuePair<int, List<Cell>> kind in byKind)
            {
                groups.Add(new GemGroup(kind.Key, kind.Value, 0));
            }

            var steps = new List<CascadeStep>();
            Cascade(groups, steps);
            return Finish(false, center, center, steps);
        }

        /// <summary>Shuffles the board even when a move exists (the shuffle item).</summary>
        internal BoardResolution ShuffleNow()
        {
            List<GemMove> moves = Shuffle(out bool regenerated);
            return new BoardResolution(true, false, default, default, null, moves, regenerated);
        }

        // Clears the first groups, then repeats fall → refill → find until nothing matches.
        private void Cascade(List<GemGroup> groups, List<CascadeStep> steps)
        {
            while (groups.Count > 0 && steps.Count < MaxSteps)
            {
                foreach (GemGroup group in groups)
                {
                    foreach (Cell cell in group.Cells)
                    {
                        Board[cell] = Board.Empty;
                    }
                }

                steps.Add(new CascadeStep(steps.Count, groups, Collapse()));
                groups = MatchFinder.Find(Board);
            }
        }

        private BoardResolution Finish(bool swapped, Cell a, Cell b, List<CascadeStep> steps)
        {
            List<GemMove> shuffle = null;
            bool regenerated = false;
            if (!MoveFinder.HasMove(Board))
            {
                shuffle = Shuffle(out regenerated);
            }

            return new BoardResolution(true, swapped, a, b, steps, shuffle, regenerated);
        }

        // Every column: gems fall to the lowest empty cells keeping their order, then new gems drop in from above.
        private List<GemMove> Collapse()
        {
            var moves = new List<GemMove>();
            for (int column = 0; column < Board.Columns; column++)
            {
                int target = 0;
                for (int row = 0; row < Board.Rows; row++)
                {
                    int gem = Board[row, column];
                    if (gem == Board.Empty)
                    {
                        continue;
                    }

                    if (row != target)
                    {
                        Board[target, column] = gem;
                        Board[row, column] = Board.Empty;
                        moves.Add(new GemMove(new Cell(row, column), new Cell(target, column), gem, false));
                    }

                    target++;
                }

                for (int row = target, above = Board.Rows; row < Board.Rows; row++, above++)
                {
                    int gem = _spawner.Next();
                    Board[row, column] = gem;
                    moves.Add(new GemMove(new Cell(above, column), new Cell(row, column), gem, true));
                }
            }

            return moves;
        }

        // A permutation of the current gems with no ready match and at least one move; new gems when none is found.
        private List<GemMove> Shuffle(out bool regenerated)
        {
            var cells = new List<Cell>(Board.Rows * Board.Columns);
            for (int row = 0; row < Board.Rows; row++)
            {
                for (int column = 0; column < Board.Columns; column++)
                {
                    cells.Add(new Cell(row, column));
                }
            }

            Board original = Board.Clone();
            var order = new List<Cell>(cells);
            for (int attempt = 0; attempt < ShuffleAttempts; attempt++)
            {
                for (int i = order.Count - 1; i > 0; i--)
                {
                    int j = _spawner.Random.Next(i + 1);
                    Cell swap = order[i];
                    order[i] = order[j];
                    order[j] = swap;
                }

                for (int i = 0; i < cells.Count; i++)
                {
                    Board[order[i]] = original[cells[i]];
                }

                if (MatchFinder.Find(Board).Count == 0 && MoveFinder.HasMove(Board))
                {
                    var moves = new List<GemMove>(cells.Count);
                    for (int i = 0; i < cells.Count; i++)
                    {
                        if (!cells[i].Equals(order[i]))
                        {
                            moves.Add(new GemMove(cells[i], order[i], original[cells[i]], false));
                        }
                    }

                    regenerated = false;
                    return moves;
                }
            }

            FillPlayable();
            regenerated = true;
            return new List<GemMove>();
        }

        // New gems everywhere, never three in a row, until the board has a move.
        private void FillPlayable()
        {
            for (int attempt = 0; attempt < FillAttempts; attempt++)
            {
                for (int row = 0; row < Board.Rows; row++)
                {
                    for (int column = 0; column < Board.Columns; column++)
                    {
                        int left = column >= 2 && Board[row, column - 1] == Board[row, column - 2] ? Board[row, column - 1] : Board.Empty;
                        int below = row >= 2 && Board[row - 1, column] == Board[row - 2, column] ? Board[row - 1, column] : Board.Empty;
                        Board[row, column] = _spawner.NextExcept(left, below);
                    }
                }

                if (MoveFinder.HasMove(Board))
                {
                    return;
                }
            }
        }
    }
}
