using System;
using System.Collections.Generic;
using Game.Contracts.Modules;
using Game.Contracts.Roster;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;
using UnityEngine;

namespace Game.Systems.Roster
{
    /// <summary>Registers <see cref="IRoster"/> from <see cref="RosterConfig"/>.</summary>
    internal sealed class RosterModule : ICoreModule
    {
        public string Id => GameModuleIds.Roster;

        public int Priority => GameModulePriorities.Roster;

        public bool IsRequired => true;

        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public void Initialize()
        {
            var config = Resources.Load<RosterConfig>(RosterConfig.ResourcePath);
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"RosterConfig is missing at Resources/{RosterConfig.ResourcePath}. Run Game › Build All.");
            }

            CoreServiceLocator.TryGet(out IGlobalStore store);
            CoreServiceLocator.TryGet(out IEventBus events);
            CoreServiceLocator.Register<IRoster>(new RosterService(config.Heroes, config.Healers, store, events));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new RosterModule());
        }
    }
}
