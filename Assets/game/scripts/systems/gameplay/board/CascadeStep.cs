using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One wave of a resolution: the groups cleared together, then the gems that fell and the new gems that dropped in to
    /// fill the holes. <see cref="Index"/> 0 is the move itself (or the blast); every later step is a combo.
    /// </summary>
    internal sealed class CascadeStep
    {
        internal CascadeStep(int index, IReadOnlyList<GemGroup> cleared, IReadOnlyList<GemMove> moves)
        {
            Index = index;
            Cleared = cleared;
            Moves = moves;
        }

        internal int Index { get; }

        internal IReadOnlyList<GemGroup> Cleared { get; }

        /// <summary>Falls of existing gems and drops of new ones.</summary>
        internal IReadOnlyList<GemMove> Moves { get; }
    }
}
