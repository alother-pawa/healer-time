using System.Collections.Generic;
using Paowa.Settings;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Index arithmetic of the "◄ value ►" stepper. Left/right and the arrows clamp at the ends (the arrow at an end is
    /// disabled, so the player sees the end); Submit or a click on the value wraps around. A value that is not among the
    /// options (a resized window, a provider that changed) has no position: stepping right picks the first option, left the
    /// last.
    /// </summary>
    internal static class ChoiceStepper
    {
        /// <summary>Index after stepping <paramref name="delta"/> from <paramref name="current"/> (-1 = unknown); -1 when there are no options.</summary>
        internal static int Step(int current, int count, int delta, bool wrap)
        {
            if (count <= 0)
            {
                return -1;
            }

            if (current < 0 || current >= count)
            {
                return delta >= 0 ? 0 : count - 1;
            }

            int next = current + delta;
            if (wrap)
            {
                return ((next % count) + count) % count;
            }

            return next < 0 ? 0 : next >= count ? count - 1 : next;
        }

        /// <summary>True when a clamped step of <paramref name="delta"/> changes the value (drives the arrows' state).</summary>
        internal static bool CanStep(int current, int count, int delta)
        {
            if (count <= 0)
            {
                return false;
            }

            if (current < 0 || current >= count)
            {
                return true;
            }

            return delta < 0 ? current > 0 : current < count - 1;
        }

        /// <summary>Index of the option whose value is <paramref name="value"/>; -1 when none.</summary>
        internal static int IndexOf(IReadOnlyList<SettingOption> options, string value)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && options[i].Value == value)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
