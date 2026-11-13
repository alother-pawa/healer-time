using System;
using System.Collections.Generic;
using Game.Contracts.Interaction;
using Game.Contracts.Modules;
using Paowa.Core.Foundation.Bootstrap;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Foundation.Ticking;
using Paowa.Core.Messaging;
using Paowa.Input;
using UnityEngine;

namespace Game.Systems.InputActivity
{
    /// <summary>
    /// Registers <see cref="IInputActivity"/> over the actions of <see cref="InputActivityConfig"/> and samples them every
    /// frame. An action the input actions asset lacks is left out at startup; one whose kind does not match its value type
    /// is dropped by the sampler at its first failed read, with one error instead of an exception every frame. Without the input package's module or frame driver the activity stays empty.
    /// Game-agnostic (no art, no game ids): a candidate for a package once it has settled (compare com.paowa.input.tracking).
    /// </summary>
    internal sealed class InputActivityModule : ICoreModule
    {
        public string Id => GameModuleIds.InputActivity;

        public int Priority => GameModulePriorities.InputActivity;

        public bool IsRequired => false;

        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        public void Initialize()
        {
            var config = Resources.Load<InputActivityConfig>(InputActivityConfig.ResourcePath);
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"InputActivityConfig is missing at Resources/{InputActivityConfig.ResourcePath}. Run Game › Build All.");
            }

            CoreServiceLocator.TryGet(out IInputService input);
            CoreServiceLocator.TryGet(out IEventBus events);
            var tracker = new InputActivityTracker(Usable(config.Actions, input), config.HistoryCapacity, config.VectorThreshold, events);
            CoreServiceLocator.Register<IInputActivity>(tracker);

            if (input == null)
            {
                Debug.LogWarning("[Game] the input package's module is not running; the input activity stays empty.");
                return;
            }

            if (CoreServiceLocator.TryGet(out IFrameDriver frames))
            {
                frames.Register(new InputActivitySampler(tracker, input));
            }
            else
            {
                Debug.LogWarning("[Game] Core's frame driver is not running; the input activity is not sampled.");
            }
        }

        private static List<TrackedAction> Usable(IReadOnlyList<TrackedAction> actions, IInputService input)
        {
            var usable = new List<TrackedAction>();
            foreach (TrackedAction action in actions)
            {
                if (action == null || string.IsNullOrEmpty(action.Action))
                {
                    continue;
                }

                if (input != null && !CanRead(input, action))
                {
                    continue;
                }

                usable.Add(action);
            }

            return usable;
        }

        // An action missing from the asset is left out at startup. A kind that does not match the value type only fails once
        // a control drives the action, so the sampler drops it at its first failed read.
        private static bool CanRead(IInputService input, TrackedAction action)
        {
            if (input.HasAction(action.Action))
            {
                return true;
            }

            Debug.LogError($"[Game] Input activity: action '{action.Action}' is not in the input actions asset; it is not tracked.");
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            CoreBootstrapper.Contribute(new InputActivityModule());
        }
    }
}
