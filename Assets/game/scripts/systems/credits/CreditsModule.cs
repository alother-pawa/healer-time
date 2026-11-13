using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.Credits
{
    /// <summary>Registers the credits popup from <see cref="CreditsConfig"/> and <see cref="ICreditsMenu"/>.</summary>
    internal sealed class CreditsModule : ICoreModule
    {
        private readonly string[] _dependsOn = { UIModuleIds.UI };

        public string Id => GameModuleIds.Credits;

        public int Priority => GameModulePriorities.Credits;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            var config = Resources.Load<CreditsConfig>(CreditsConfig.ResourcePath);
            if (config == null || config.Popup == null)
            {
                throw new InvalidOperationException(
                    $"CreditsConfig with the credits popup is missing at Resources/{CreditsConfig.ResourcePath}. Run Game › Build All.");
            }

            var ui = CoreServiceLocator.Get<IUIManager>();
            ui.RegisterView(config.Popup.ViewId, config.Popup.gameObject);
            CoreServiceLocator.Register<ICreditsMenu>(new CreditsMenu(ui, config.Popup.ViewId));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new CreditsModule());
        }
    }
}
