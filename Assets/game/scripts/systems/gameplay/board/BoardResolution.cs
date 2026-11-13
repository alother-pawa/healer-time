using System.Collections.Generic;

namespace Game.Systems.Gameplay
{
    /// <summary>What one action did to the board, in order: an optional swap, the cascade steps, then an optional shuffle.</summary>
    internal sealed class BoardResolution
    {
        private static readonly IReadOnlyList<CascadeStep> NoSteps = new CascadeStep[0];
        private static readonly IReadOnlyList<GemMove> NoMoves = new GemMove[0];

        internal BoardResolution(bool valid, bool swapped, Cell a, Cell b, IReadOnlyList<CascadeStep> steps,
            IReadOnlyList<GemMove> shuffle, bool regenerated)
        {
            Valid = valid;
            Swapped = swapped;
            A = a;
            B = b;
            Steps = steps ?? NoSteps;
            Shuffle = shuffle ?? NoMoves;
            Regenerated = regenerated;
        }

        /// <summary>False when the action changed nothing (a swap without a match, a target outside the board).</summary>
        internal bool Valid { get; }

        /// <summary>True when <see cref="A"/> and <see cref="B"/> were swapped first.</summary>
        internal bool Swapped { get; }

        internal Cell A { get; }

        internal Cell B { get; }

        internal IReadOnlyList<CascadeStep> Steps { get; }

        /// <summary>Where every gem went when the board had no move left and was shuffled (empty otherwise).</summary>
        internal IReadOnlyList<GemMove> Shuffle { get; }

        /// <summary>True when shuffling could not find a playable layout and the board was filled with new gems.</summary>
        internal bool Regenerated { get; }

        /// <summary>True when the board was shuffled or regenerated after the steps.</summary>
        internal bool Reshuffled => Shuffle.Count > 0 || Regenerated;

        internal static BoardResolution Invalid(Cell a, Cell b)
        {
            return new BoardResolution(false, false, a, b, null, null, false);
        }
    }
}
