using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Session;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Save;
using UnityEngine;

namespace Game.Systems.FirstLaunch
{
    /// <summary>Registers <see cref="IFirstLaunch"/> over the global save store; the screen itself lives in its scene.</summary>
    internal sealed class FirstLaunchModule : ICoreModule
    {
        private readonly string[] _dependsOn = { SaveModuleIds.Save };

        public string Id => GameModuleIds.FirstLaunch;

        public int Priority => GameModulePriorities.FirstLaunch;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => _dependsOn;

        public void Initialize()
        {
            ISaveService save = CoreServiceLocator.TryGet(out ISaveService found) ? found : null;
            CoreServiceLocator.Register<IFirstLaunch>(
                new FirstLaunchState(save != null ? save.Global : null, save != null ? save.FlushGlobal : (System.Func<bool>)null));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new FirstLaunchModule());
        }
    }
}
