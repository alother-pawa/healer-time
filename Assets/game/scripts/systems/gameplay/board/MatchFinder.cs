using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Finds every group to clear on a board: horizontal and vertical runs of three or more gems of one kind; runs of the same
    /// kind that share a cell become one group. Groups come out in a fixed order (by their lowest, then leftmost cell), so
    /// the same board always gives the same result.
    /// </summary>
    internal static class MatchFinder
    {
        internal const int MinRun = 3;

        internal static List<GemGroup> Find(Board board)
        {
            var runs = new List<List<Cell>>();
            CollectRuns(board, runs, horizontal: true);
            CollectRuns(board, runs, horizontal: false);
            if (runs.Count == 0)
            {
                return new List<GemGroup>();
            }

            // Union runs of one kind that share a cell.
            var parent = new int[runs.Count];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            var owner = new Dictionary<Cell, int>();
            for (int i = 0; i < runs.Count; i++)
            {
                foreach (Cell cell in runs[i])
                {
                    if (owner.TryGetValue(cell, out int other))
                    {
                        Union(parent, i, other);
                    }
                    else
                    {
                        owner[cell] = i;
                    }
                }
            }

            var groups = new SortedDictionary<int, (HashSet<Cell> cells, List<Cell> ordered, int longest)>();
            for (int i = 0; i < runs.Count; i++)
            {
                int root = Find(parent, i);
                if (!groups.TryGetValue(root, out var group))
                {
                    group = (new HashSet<Cell>(), new List<Cell>(), 0);
                }

                foreach (Cell cell in runs[i])
                {
                    if (group.cells.Add(cell))
                    {
                        group.ordered.Add(cell);
                    }
                }

                group.longest = System.Math.Max(group.longest, runs[i].Count);
                groups[root] = group;
            }

            var matches = new List<GemGroup>(groups.Count);
            foreach (var group in groups.Values)
            {
                group.ordered.Sort(CompareCells);
                matches.Add(new GemGroup(board[group.ordered[0]], group.ordered, group.longest));
            }

            matches.Sort((a, b) => CompareCells(a.Cells[0], b.Cells[0]));
            return matches;
        }

        /// <summary>True when the gem at <paramref name="cell"/> is part of a run of three or more.</summary>
        internal static bool IsInRun(Board board, Cell cell)
        {
            int kind = board[cell];
            if (kind == Board.Empty)
            {
                return false;
            }

            return Span(board, cell, kind, 0, 1) + Span(board, cell, kind, 0, -1) + 1 >= MinRun
                   || Span(board, cell, kind, 1, 0) + Span(board, cell, kind, -1, 0) + 1 >= MinRun;
        }

        internal static int CompareCells(Cell a, Cell b)
        {
            return a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column);
        }

        private static int Span(Board board, Cell from, int kind, int rowStep, int columnStep)
        {
            int count = 0;
            var cell = new Cell(from.Row + rowStep, from.Column + columnStep);
            while (board.Contains(cell) && board[cell] == kind)
            {
                count++;
                cell = new Cell(cell.Row + rowStep, cell.Column + columnStep);
            }

            return count;
        }

        private static void CollectRuns(Board board, List<List<Cell>> runs, bool horizontal)
        {
            int lines = horizontal ? board.Rows : board.Columns;
            int length = horizontal ? board.Columns : board.Rows;
            for (int line = 0; line < lines; line++)
            {
                int start = 0;
                while (start < length)
                {
                    Cell first = horizontal ? new Cell(line, start) : new Cell(start, line);
                    int kind = board[first];
                    int end = start + 1;
                    while (end < length && kind != Board.Empty && board[horizontal ? new Cell(line, end) : new Cell(end, line)] == kind)
                    {
                        end++;
                    }

                    if (kind != Board.Empty && end - start >= MinRun)
                    {
                        var run = new List<Cell>(end - start);
                        for (int i = start; i < end; i++)
                        {
                            run.Add(horizontal ? new Cell(line, i) : new Cell(i, line));
                        }

                        runs.Add(run);
                    }

                    start = end;
                }
            }
        }

        private static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        private static void Union(int[] parent, int a, int b)
        {
            int rootA = Find(parent, a);
            int rootB = Find(parent, b);
            if (rootA != rootB)
            {
                parent[System.Math.Max(rootA, rootB)] = System.Math.Min(rootA, rootB);
            }
        }
    }
}
