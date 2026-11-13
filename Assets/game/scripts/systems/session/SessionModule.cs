using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Session;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using UnityEngine;

namespace Game.Systems.Session
{
    /// <summary>Registers <see cref="ISessionContext"/>.</summary>
    internal sealed class SessionModule : ICoreModule
    {
        public string Id => GameModuleIds.Session;

        public int Priority => GameModulePriorities.Session;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public void Initialize()
        {
            CoreServiceLocator.Register<ISessionContext>(new SessionContext());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new SessionModule());
        }
    }
}
