using System;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// How much each gem kind is worth when a cascade step pops: every cleared gem counts once, more in later combo steps
    /// (<see cref="BattleTuning.ComboBonus"/>) and in groups larger than three (<see cref="BattleTuning.LargeMatchBonus"/>).
    /// </summary>
    internal static class GemPayout
    {
        /// <summary>Power per gem kind (index = kind) of one step.</summary>
        internal static float[] Powers(CascadeStep step, int kinds, BattleTuning tuning)
        {
            var powers = new float[kinds];
            float combo = 1f + tuning.ComboBonus * step.Index;
            foreach (GemGroup group in step.Cleared)
            {
                float size = 1f + tuning.LargeMatchBonus * Math.Max(0, group.Count - MatchFinder.MinRun);
                powers[group.Kind] += group.Count * combo * size;
            }

            return powers;
        }

        /// <summary>Score of one step's cleared gems: every gem, times the step's combo bonus.</summary>
        internal static int Score(CascadeStep step, BattleTuning tuning)
        {
            float combo = 1f + tuning.ComboBonus * step.Index;
            float score = 0f;
            foreach (GemGroup group in step.Cleared)
            {
                score += group.Count * tuning.ScorePerGem * combo;
            }

            return (int)Math.Round(score);
        }
    }
}
