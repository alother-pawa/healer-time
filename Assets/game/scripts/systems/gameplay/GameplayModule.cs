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
    /// Registers the gameplay popups of <see cref="GameplayConfig"/> (pause menu, revive, result) with the UI manager, so the
    /// gameplay scene can show them over the persistent UI scene. Everything else of gameplay lives in the scene.
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
            if (config == null || config.PausePopup == null || config.RevivePopup == null || config.ResultPopup == null
                || config.MenuScene == null || config.Balance == null)
            {
                throw new InvalidOperationException(
                    $"GameplayConfig with its popups, menu scene and balance is missing at Resources/{GameplayConfig.ResourcePath}. "
                    + "Run Game › Build All.");
            }

            var ui = CoreServiceLocator.Get<IUIManager>();
            ui.RegisterView(config.PausePopup.ViewId, config.PausePopup.gameObject);
            ui.RegisterView(config.RevivePopup.ViewId, config.RevivePopup.gameObject);
            ui.RegisterView(config.ResultPopup.ViewId, config.ResultPopup.gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new GameplayModule());
        }
    }
}
