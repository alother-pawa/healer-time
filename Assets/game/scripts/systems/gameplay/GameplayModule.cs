using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Registers the pause menu popup of <see cref="GameplayConfig"/> with the UI manager, so the gameplay screen can show
    /// it over the persistent UI scene. Everything else of gameplay lives in the scene (<see cref="GameplayScreen"/>).
    /// </summary>
    internal sealed class GameplayModule : ICoreModule
    {
        private readonly string[] _dependsOn = { UIModuleIds.UI };

        public string Id => GameModuleIds.Gameplay;

        public int Priority => GameModulePriorities.Gameplay;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            var config = Resources.Load<GameplayConfig>(GameplayConfig.ResourcePath);
            if (config == null || config.PausePopup == null || config.MenuScene == null)
            {
                throw new InvalidOperationException(
                    $"GameplayConfig with the pause popup and the menu scene is missing at Resources/{GameplayConfig.ResourcePath}. "
                    + "Run Game › Build All.");
            }

            CoreServiceLocator.Get<IUIManager>().RegisterView(config.PausePopup.ViewId, config.PausePopup.gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new GameplayModule());
        }
    }
}
