using System.Globalization;
using System.Text;
using Game.Contracts.Interaction;
using Paowa.Core.Foundation.Services;
using Paowa.DevConsole;
using UnityEngine.Scripting;

namespace Game.Systems.InputActivity
{
    /// <summary>Dev console commands of the input activity (development builds and the Editor only, like the console).</summary>
    internal static class InputActivityCommands
    {
        private const int DefaultLines = 10;

        [Preserve]
        [DevCommand("input.history", "Prints the last input presses/releases: input.history [count]")]
        private static string History(string[] args)
        {
            if (!CoreServiceLocator.TryGet(out IInputActivity activity))
            {
                return "The input activity module is not running.";
            }

            int lines = args.Length > 0 && int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : DefaultLines;
            int count = System.Math.Min(System.Math.Max(lines, 0), activity.HistoryCount);
            if (count == 0)
            {
                return "No input recorded yet.";
            }

            var text = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    text.Append('\n');
                }

                InputActivityOverlay.AppendEntry(text, activity.GetHistory(i));
            }

            return text.ToString();
        }

        [Preserve]
        [DevCommand("input.clear", "Clears the input history, press counts and hold times")]
        private static string Clear()
        {
            if (!CoreServiceLocator.TryGet(out IInputActivity activity))
            {
                return "The input activity module is not running.";
            }

            activity.Clear();
            return "Input history cleared.";
        }
    }
}
