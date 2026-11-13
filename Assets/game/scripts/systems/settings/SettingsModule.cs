using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Presentation;
using Game.Contracts.Settings;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using Paowa.Settings;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.Settings
{
    /// <summary>
    /// Registers the settings popup and the "Keep these display settings?" popup of the <see cref="SettingsConfig"/>,
    /// <see cref="ISettingsMenu"/> and <see cref="IGameSettings"/>, and gives the settings package's service the game's
    /// appliers: the animation switch (backed by <see cref="IAnimationSettings"/>) and the game's own values (UI scale,
    /// camera shake, sensitivity...). Appliers receive the saved values as they register, before the first scene loads.
    /// </summary>
    internal sealed class SettingsModule : ICoreModule
    {
        private readonly string[] _dependsOn = { UIModuleIds.UI };

        public string Id => GameModuleIds.Settings;

        public int Priority => GameModulePriorities.Settings;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            var config = Resources.Load<SettingsConfig>(SettingsConfig.ResourcePath);
            if (config == null || config.Popup == null || config.DisplayConfirmPopup == null)
            {
                throw new InvalidOperationException(
                    $"SettingsConfig with both popups is missing at Resources/{SettingsConfig.ResourcePath}. Run Game › Build All.");
            }

            var ui = CoreServiceLocator.Get<IUIManager>();
            var events = CoreServiceLocator.Get<IEventBus>();
            ui.RegisterView(config.Popup.ViewId, config.Popup.gameObject);
            CoreServiceLocator.Register<ISettingsMenu>(new SettingsMenu(ui, config.Popup.ViewId));

            var gameSettings = new GameSettings(events);
            CoreServiceLocator.Register<IGameSettings>(gameSettings);
            if (CoreServiceLocator.TryGet(out ISettingsService settings))
            {
                settings.RegisterApplier(gameSettings);
                if (CoreServiceLocator.TryGet(out IAnimationSettings animations))
                {
                    settings.RegisterApplier(new AnimationSettingApplier(animations, settings, events));
                }
            }

            if (CoreServiceLocator.TryGet(out IDisplayService _))
            {
                ui.RegisterView(config.DisplayConfirmPopup.ViewId, config.DisplayConfirmPopup.gameObject);
                var presenter = new DisplayConfirmPresenter(new PopupDisplayConfirmView(ui, config.DisplayConfirmPopup.ViewId));

                // Lives for the application, like the module.
                events.Subscribe<DisplayChangePendingEvent>(evt => presenter.OnPending(evt.Change));
                events.Subscribe<DisplayChangeResolvedEvent>(evt => presenter.OnResolved(evt.Change));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new SettingsModule());
        }
    }
}
