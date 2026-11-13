using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// A group of gems cleared together: a run of three or more of one kind, merged with every crossing run of the same kind
    /// (L, T and + shapes count as one group), or the gems of one kind caught by a blast.
    /// </summary>
    internal sealed class GemGroup
    {
        internal GemGroup(int kind, IReadOnlyList<Cell> cells, int longestRun)
        {
            Kind = kind;
            Cells = cells;
            LongestRun = longestRun;
        }

        internal int Kind { get; }

        internal IReadOnlyList<Cell> Cells { get; }

        internal int Count => Cells.Count;

        /// <summary>Longest straight run inside the group; 0 for a blast.</summary>
        internal int LongestRun { get; }
    }
}
