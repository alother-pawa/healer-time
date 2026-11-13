using System;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// How much each gem kind is worth after a board action: every cleared gem counts once, more in later combo steps
    /// (<see cref="BattleTuning.ComboBonus"/>) and in groups larger than three (<see cref="BattleTuning.LargeMatchBonus"/>).
    /// </summary>
    internal static class GemPayout
    {
        /// <summary>Power per gem kind (index = kind).</summary>
        internal static float[] Powers(BoardResolution resolution, int kinds, BattleTuning tuning)
        {
            var powers = new float[kinds];
            foreach (CascadeStep step in resolution.Steps)
            {
                float combo = 1f + tuning.ComboBonus * step.Index;
                foreach (GemGroup group in step.Cleared)
                {
                    float size = 1f + tuning.LargeMatchBonus * Math.Max(0, group.Count - MatchFinder.MinRun);
                    powers[group.Kind] += group.Count * combo * size;
                }
            }

            return powers;
        }

        /// <summary>Score of the cleared gems: every gem, times its combo step's bonus.</summary>
        internal static int Score(BoardResolution resolution, BattleTuning tuning)
        {
            float score = 0f;
            foreach (CascadeStep step in resolution.Steps)
            {
                float combo = 1f + tuning.ComboBonus * step.Index;
                foreach (GemGroup group in step.Cleared)
                {
                    score += group.Count * tuning.ScorePerGem * combo;
                }
            }

            return (int)Math.Round(score);
        }
    }
}
