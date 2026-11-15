using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Presentation;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Logging;
using Paowa.Core.Foundation.Services;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.UIShell
{
    /// <summary>
    /// Registers <see cref="IToastService"/>, <see cref="IConfirmDialog"/> and <see cref="IHowToPlay"/> (with the popups of
    /// the <see cref="UIShellConfig"/>); the UI scene's behaviours (root, loading screen, toast host) attach to Core at runtime.
    /// </summary>
    internal sealed class UIShellModule : ICoreModule
    {
        private readonly string[] _dependsOn = { UIModuleIds.UI, CoreModuleIds.Scenes };

        public string Id => GameModuleIds.UIShell;

        public int Priority => GameModulePriorities.UIShell;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            var logger = CoreServiceLocator.Get<ICoreLogger>();
            CoreServiceLocator.Register<IToastService>(new ToastService(logger));

            var config = Resources.Load<UIShellConfig>(UIShellConfig.ResourcePath);
            if (config == null || config.ConfirmDialog == null || config.HowToPlay == null)
            {
                throw new InvalidOperationException(
                    $"UIShellConfig with its confirmation and How to play popups is missing at Resources/{UIShellConfig.ResourcePath}. Run Game › Build All.");
            }

            var ui = CoreServiceLocator.Get<IUIManager>();
            ui.RegisterView(config.ConfirmDialog.ViewId, config.ConfirmDialog.gameObject);
            CoreServiceLocator.Register<IConfirmDialog>(
                new ConfirmDialogService(new PopupConfirmPresenter(ui, config.ConfirmDialog.ViewId), logger));
            ui.RegisterView(config.HowToPlay.ViewId, config.HowToPlay.gameObject);
            CoreServiceLocator.Register<IHowToPlay>(new HowToPlayMenu(ui, config.HowToPlay.ViewId));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new UIShellModule());
        }
    }
}
