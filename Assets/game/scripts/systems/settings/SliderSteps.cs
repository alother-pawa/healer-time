using UnityEngine;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Maps a slider definition's range onto whole slider positions, so one left/right press (or one drag notch) is one step
    /// of the definition. A stepped range has one position per step from the minimum; when the range is not a whole number
    /// of steps the last position is the maximum itself (a shorter last step), so both ends are always reachable. A
    /// continuous definition (step 0) gets <see cref="ContinuousSteps"/> even positions.
    /// </summary>
    internal static class SliderSteps
    {
        /// <summary>Positions of a continuous slider (5% of the range per press).</summary>
        internal const int ContinuousSteps = 20;

        // Float division of a range by its step (1 / 0.05) lands a hair off the whole number.
        private const float WholeTolerance = 1e-3f;

        /// <summary>Number of steps between the ends (positions are 0..count).</summary>
        internal static int Count(float min, float max, float step)
        {
            if (max <= min)
            {
                return 1;
            }

            if (step <= 0f)
            {
                return ContinuousSteps;
            }

            float exact = (max - min) / step;
            int whole = Mathf.RoundToInt(exact);
            return Mathf.Max(1, Mathf.Abs(exact - whole) < WholeTolerance ? whole : Mathf.CeilToInt(exact));
        }

        /// <summary>Value at position <paramref name="index"/>; the last position is the maximum.</summary>
        internal static float ToValue(int index, float min, float max, float step, int count)
        {
            if (count <= 0 || index <= 0)
            {
                return min;
            }

            if (index >= count)
            {
                return max;
            }

            return step > 0f ? Mathf.Min(max, min + index * step) : Mathf.Lerp(min, max, (float)index / count);
        }

        /// <summary>Nearest position of <paramref name="value"/>.</summary>
        internal static int ToIndex(float value, float min, float max, float step, int count)
        {
            if (max <= min || count <= 0)
            {
                return 0;
            }

            if (step <= 0f)
            {
                return Mathf.Clamp(Mathf.RoundToInt((value - min) / (max - min) * count), 0, count);
            }

            if (value >= max - step * WholeTolerance)
            {
                return count;
            }

            return Mathf.Clamp(Mathf.RoundToInt((value - min) / step), 0, count);
        }
    }
}
