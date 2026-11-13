using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Desktop;
using UnityEngine;

namespace Game.Systems.RenderSettings
{
    /// <summary>
    /// Applies the display service's render scale and brightness on Universal RP (<see cref="UrpDisplayAdapter"/>): the
    /// saved values at startup, then every change. The assembly compiles only when the URP package is installed (version
    /// define <c>GAME_URP</c>); without it the values are still saved by the display service and do nothing. No service.
    /// </summary>
    internal sealed class RenderSettingsModule : ICoreModule
    {
        public string Id => GameModuleIds.RenderSettings;

        public int Priority => GameModulePriorities.RenderSettings;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public void Initialize()
        {
            if (!CoreServiceLocator.TryGet(out IDisplayService display))
            {
                return;
            }

            var adapter = new UrpDisplayAdapter(display);
            adapter.ApplyAll();

            IDisposable binding = CoreServiceLocator.Get<IEventBus>().Subscribe<DisplaySettingsChangedEvent>(adapter.OnDisplaySettingsChanged);

            // Undone when the application (or Play Mode) ends, so with domain reload off a later session starts clean.
            void OnQuitting()
            {
                Application.quitting -= OnQuitting;
                binding.Dispose();
                adapter.Restore();
            }

            Application.quitting += OnQuitting;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new RenderSettingsModule());
        }
    }
}
