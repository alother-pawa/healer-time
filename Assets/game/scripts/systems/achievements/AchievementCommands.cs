using System.Text;
using Paowa.Core.Foundation.Services;
using Paowa.DevConsole;
using Paowa.Platform;
using UnityEngine.Scripting;

namespace Game.Systems.Achievements
{
    /// <summary>Dev console commands of the achievements (development builds and the Editor only, like the console).</summary>
    internal static class AchievementCommands
    {
        [Preserve]
        [DevCommand("achievements.list", "Lists the achievements of the catalog and whether each is unlocked")]
        private static string List()
        {
            if (!CoreServiceLocator.TryGet(out IPlatformService platform))
            {
                return "The platform module is not running.";
            }

            AchievementCatalog catalog = platform.Achievements;
            if (catalog == null)
            {
                return "No achievement catalog is assigned in PlatformSettings.";
            }

            var text = new StringBuilder($"Platform: {platform.PlatformId}");
            foreach (AchievementDefinition definition in catalog.Achievements)
            {
                if (definition != null && definition.Id.Length > 0)
                {
                    text.Append('\n').Append(platform.IsAchievementUnlocked(definition.Id) ? "[x] " : "[ ] ").Append(definition.Id);
                }
            }

            return text.ToString();
        }

        [Preserve]
        [DevCommand("achievements.unlock", "Unlocks an achievement: achievements.unlock <id>")]
        private static string Unlock(string[] args)
        {
            if (!CoreServiceLocator.TryGet(out IPlatformService platform))
            {
                return "The platform module is not running.";
            }

            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                return "Usage: achievements.unlock <id> (see achievements.list)";
            }

            return platform.UnlockAchievement(args[0]) ? $"Unlocked {args[0]}." : $"Could not unlock {args[0]}.";
        }

        [Preserve]
        [DevCommand("achievements.reset", "Locks every achievement again and resets the stats")]
        private static string Reset()
        {
            if (!CoreServiceLocator.TryGet(out IPlatformService platform))
            {
                return "The platform module is not running.";
            }

            return platform.ResetAll() ? "Every achievement is locked again." : "The platform refused the reset.";
        }
    }
}
