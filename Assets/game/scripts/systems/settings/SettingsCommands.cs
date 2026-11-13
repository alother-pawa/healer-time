using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Contracts.Settings;
using Paowa.Core.Foundation.Services;
using Paowa.DevConsole;
using Paowa.Settings;
using UnityEngine.Scripting;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Dev console commands of the settings (development builds and the Editor only, like the console). Values go through
    /// the settings service, so they are saved and the Settings popup shows them.
    /// </summary>
    internal static class SettingsCommands
    {
        // Above this a value is read as a percentage ("110" = 1.1).
        private const float PercentThreshold = 5f;

        [Preserve]
        [DevCommand("ui.scale", "Sets the interface size: ui.scale <1.1 | 110>")]
        private static string UIScale(string[] args)
        {
            if (!CoreServiceLocator.TryGet(out ISettingsService settings)
                || !settings.TryGetDefinition(GameSettingIds.UIScale, out SettingDefinition definition))
            {
                return "The interface size setting is not available.";
            }

            if (args.Length == 0 || !float.TryParse(args[0].TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return $"Interface size: {settings.GetFloat(GameSettingIds.UIScale).ToString("0.00", CultureInfo.InvariantCulture)}. "
                    + "Usage: ui.scale <1.1 | 110>";
            }

            settings.SetFloat(GameSettingIds.UIScale, value > PercentThreshold ? value / 100f : value);
            if (definition.ApplyMode == SettingApplyMode.Deferred)
            {
                settings.ApplyTab(definition.TabId);
            }

            return $"Interface size: {settings.GetFloat(GameSettingIds.UIScale).ToString("0.00", CultureInfo.InvariantCulture)}";
        }

        [Preserve]
        [DevCommand("lang", "Switches the language: lang <code>")]
        private static string Language(string[] args)
        {
            if (!CoreServiceLocator.TryGet(out ISettingsService settings) || !settings.TryGetDefinition(SettingIds.Language, out _))
            {
                return "The language setting is not available.";
            }

            var options = new List<SettingOption>();
            settings.GetOptions(SettingIds.Language, options);
            var codes = new StringBuilder();
            foreach (SettingOption option in options)
            {
                if (args.Length > 0 && option.Value == args[0])
                {
                    settings.SetString(SettingIds.Language, option.Value);
                    return $"Language: {option.Value}";
                }

                codes.Append(codes.Length > 0 ? ", " : string.Empty).Append(option.Value);
            }

            return args.Length == 0 ? $"Languages: {codes}. Usage: lang <code>" : $"Unknown language '{args[0]}'. Languages: {codes}";
        }
    }
}
