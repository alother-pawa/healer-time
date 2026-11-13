using System;
using System.Collections.Generic;
using Game.Contracts.Interaction;
using Game.Contracts.Modules;
using Game.Contracts.Navigation;
using Game.Contracts.Presentation;
using Game.Contracts.Save;
using Game.Foundation.Localization;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using Paowa.Localization;
using Paowa.Platform;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.Achievements
{
    /// <summary>
    /// Registers the achievements popup from <see cref="AchievementsConfig"/> and <see cref="IAchievementsMenu"/>, and
    /// connects the platform service to the game for the application's lifetime:
    /// <list type="bullet">
    /// <item>the demo's rules (<see cref="AchievementRules"/>) unlock achievements from gameplay presses and saves
    /// (<see cref="GameSavedEvent"/>);</item>
    /// <item>offline (<see cref="PlatformId.None"/>) every unlock shows a toast, since no platform shows its own popup;</item>
    /// <item>each scene switch sets the rich presence <c>status</c> of the destination (cleared without an entry), sent
    /// again in the new language when the language changes.</item>
    /// </list>
    /// </summary>
    internal sealed class AchievementsModule : ICoreModule
    {
        private const string StatusKey = "status";
        private const string ToastKey = "achievements.toast";

        private readonly string[] _dependsOn = { UIModuleIds.UI, PlatformModuleIds.Platform };
        private readonly AchievementRules _rules = new AchievementRules();
        private readonly List<string> _earned = new List<string>();
        private IPlatformService _platform;
        private AchievementsConfig _config;
        private string _presenceKey = string.Empty;

        public string Id => GameModuleIds.Achievements;

        public int Priority => GameModulePriorities.Achievements;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            _config = Resources.Load<AchievementsConfig>(AchievementsConfig.ResourcePath);
            if (_config == null || _config.Popup == null)
            {
                throw new InvalidOperationException(
                    $"AchievementsConfig with the achievements popup is missing at Resources/{AchievementsConfig.ResourcePath}. Run Game › Build All.");
            }

            var ui = CoreServiceLocator.Get<IUIManager>();
            ui.RegisterView(_config.Popup.ViewId, _config.Popup.gameObject);
            CoreServiceLocator.Register<IAchievementsMenu>(new AchievementsMenu(ui, _config.Popup.ViewId));

            _platform = CoreServiceLocator.Get<IPlatformService>();
            var events = CoreServiceLocator.Get<IEventBus>();

            // Live for the application, like the module.
            events.Subscribe<InputActivityRecordedEvent>(OnInputRecorded);
            events.Subscribe<GameSavedEvent>(_ => Unlock(_rules.OnGameSaved));
            events.Subscribe<PlatformAchievementUnlockedEvent>(OnUnlocked);
            events.Subscribe<SceneSwitchStartingEvent>(evt => SetPresence(_config.StatusKeyOf(evt.Scene)));
            events.Subscribe<LanguageChangedEvent>(_ => SetPresence(_presenceKey));
        }

        private void OnInputRecorded(InputActivityRecordedEvent evt)
        {
            if (evt.Entry.Phase != InputActivityPhase.Pressed)
            {
                return;
            }

            IReadOnlyList<string> tracked = CoreServiceLocator.TryGet(out IInputActivity activity) ? activity.Actions : null;
            Unlock(earned => _rules.OnActionPressed(evt.Entry.Action, tracked, earned));
        }

        private void Unlock(Action<List<string>> rule)
        {
            _earned.Clear();
            rule(_earned);
            foreach (string id in _earned)
            {
                if (!_platform.IsAchievementUnlocked(id))
                {
                    _platform.UnlockAchievement(id);
                }
            }

            _earned.Clear();
        }

        private void OnUnlocked(PlatformAchievementUnlockedEvent evt)
        {
            if (evt.Platform != PlatformId.None || !CoreServiceLocator.TryGet(out IToastService toasts))
            {
                return;
            }

            AchievementCatalog catalog = _platform.Achievements;
            string name = catalog != null && catalog.TryGet(evt.Id, out AchievementDefinition definition)
                ? Loc.Get(definition.NameKey)
                : evt.Id;
            toasts.Show(Loc.Format(ToastKey, name));
        }

        private void SetPresence(string key)
        {
            _presenceKey = key ?? string.Empty;
            _platform.SetRichPresence(StatusKey, _presenceKey.Length > 0 ? Loc.Get(_presenceKey) : null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new AchievementsModule());
        }
    }
}
