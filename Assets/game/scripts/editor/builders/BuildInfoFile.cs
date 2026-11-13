using System;
using System.Globalization;
using System.Text;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Text of the build stamp Core's <c>BuildInfo</c> reads (<c>Resources/Paowa/BuildInfo</c>): <c>key=value</c> lines
    /// <c>build</c>, <c>commit</c>, <c>date</c> (ISO 8601 UTC). Pure functions, tested without Unity.
    /// </summary>
    internal static class BuildInfoFile
    {
        /// <summary>The file's text; an empty build number or commit leaves its line out (Core shows what is known).</summary>
        internal static string Compose(string buildNumber, string commit, DateTime utc)
        {
            var text = new StringBuilder("# Written by Game.Editor.Builders.BuildPipelineMenu before the player build; removed after it.\n");
            if (!string.IsNullOrWhiteSpace(buildNumber))
            {
                text.Append("build=").Append(OneLine(buildNumber)).Append('\n');
            }

            if (!string.IsNullOrWhiteSpace(commit))
            {
                text.Append("commit=").Append(OneLine(commit)).Append('\n');
            }

            text.Append("date=").Append(utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)).Append('\n');
            return text.ToString();
        }

        /// <summary>
        /// The build number: <paramref name="environmentValue"/> (CI's <c>BUILD_NUMBER</c>) when set, else the local counter
        /// after <paramref name="counterText"/> (the counter file's content; missing or unreadable counts as 0). Returns the
        /// number and the counter text to write back (null when the counter is not used).
        /// </summary>
        internal static string ResolveBuildNumber(string environmentValue, string counterText, out string nextCounterText)
        {
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                nextCounterText = null;
                return OneLine(environmentValue);
            }

            int next = NextCounter(counterText);
            nextCounterText = next.ToString(CultureInfo.InvariantCulture);
            return nextCounterText;
        }

        /// <summary>The counter after <paramref name="counterText"/>: its number + 1, or 1 when missing, invalid or negative.</summary>
        internal static int NextCounter(string counterText)
        {
            if (string.IsNullOrWhiteSpace(counterText)
                || !int.TryParse(counterText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int current) || current < 0
                || current == int.MaxValue)
            {
                return 1;
            }

            return current + 1;
        }

        // Values never break the line format.
        private static string OneLine(string value)
        {
            return value.Trim().Replace("\r", string.Empty).Replace("\n", " ");
        }
    }
}
