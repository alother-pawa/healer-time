using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;
using Paowa.UI;
using Paowa.UI.Navigation;
using UnityEngine;

namespace Game.Systems.AnimationSettings
{
    /// <summary>
    /// Registers <see cref="IAnimationSettings"/>, kept in Core's global store (for the session only without a save
    /// package). The switch also drives the UI manager's view transitions (<see cref="IUIManager.TransitionsEnabled"/>)
    /// and the UI navigation visuals (<see cref="IUIFocusService.AnimateTransitions"/>: the selection frame and list
    /// scrolling jump instead of gliding when animations are off).
    /// </summary>
    internal sealed class AnimationSettingsModule : ICoreModule
    {
        public string Id => GameModuleIds.AnimationSettings;

        public int Priority => GameModulePriorities.AnimationSettings;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public void Initialize()
        {
            CoreServiceLocator.TryGet(out IGlobalStore store);
            var events = CoreServiceLocator.Get<IEventBus>();
            var service = new AnimationSettingsService(store, events);
            CoreServiceLocator.Register<IAnimationSettings>(service);

            if (CoreServiceLocator.TryGet(out IUIManager ui))
            {
                ui.TransitionsEnabled = service.Enabled;
                events.Subscribe<AnimationSettingsChangedEvent>(evt => ui.TransitionsEnabled = evt.Enabled);
            }

            if (CoreServiceLocator.TryGet(out IUIFocusService focus))
            {
                focus.AnimateTransitions = service.Enabled;

                // Lives for the application, like the module.
                events.Subscribe<AnimationSettingsChangedEvent>(evt => focus.AnimateTransitions = evt.Enabled);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new AnimationSettingsModule());
        }
    }
}
