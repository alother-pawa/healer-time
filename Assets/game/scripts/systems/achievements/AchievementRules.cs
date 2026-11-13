using System.Collections.Generic;

namespace Game.Systems.Achievements
{
    /// <summary>
    /// The demo's unlock rules (pure logic): which achievement a press or a save of the game in play earns. Pressed actions are counted for the
    /// session only; the platform service remembers what is unlocked, so a rule may report an id again and unlocking it
    /// twice is harmless.
    /// </summary>
    internal sealed class AchievementRules
    {
        private readonly HashSet<string> _pressed = new HashSet<string>();

        /// <summary>A tracked gameplay action was pressed; adds the ids it earns to <paramref name="earned"/>.</summary>
        internal void OnActionPressed(string action, IReadOnlyList<string> trackedActions, List<string> earned)
        {
            if (string.IsNullOrEmpty(action))
            {
                return;
            }

            earned.Add(AchievementIds.FirstAction);
            _pressed.Add(action);
            if (trackedActions == null || trackedActions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < trackedActions.Count; i++)
            {
                if (!_pressed.Contains(trackedActions[i]))
                {
                    return;
                }
            }

            earned.Add(AchievementIds.AllActions);
        }

        /// <summary>The game in play was saved (checkpoint, Save button, saving before leaving).</summary>
        internal void OnGameSaved(List<string> earned)
        {
            earned.Add(AchievementIds.FirstSave);
        }
    }
}
