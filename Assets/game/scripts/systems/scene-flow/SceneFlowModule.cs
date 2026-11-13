using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Navigation;
using Paowa.Core.Flow.Scenes;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using UnityEngine;

namespace Game.Systems.SceneFlow
{
    /// <summary>Registers <see cref="IGameNavigator"/>.</summary>
    internal sealed class SceneFlowModule : ICoreModule
    {
        private readonly string[] _dependsOn = { CoreModuleIds.Scenes };

        public string Id => GameModuleIds.SceneFlow;

        public int Priority => GameModulePriorities.SceneFlow;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            CoreServiceLocator.Register<IGameNavigator>(
                new GameNavigator(CoreServiceLocator.Get<ISceneService>(), CoreServiceLocator.Get<IEventBus>()));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new SceneFlowModule());
        }
    }
}
