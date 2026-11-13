using System;
using Game.Contracts.Navigation;
using Paowa.Core.Flow.Scenes;
using Paowa.Core.Messaging;
using UnityEngine;

namespace Game.Systems.SceneFlow
{
    /// <summary>
    /// Default <see cref="IGameNavigator"/> over <see cref="ISceneService"/>. Each switch is announced with
    /// <see cref="SceneSwitchStartingEvent"/> before the loading screen covers, so it can present itself for the destination.
    /// </summary>
    internal sealed class GameNavigator : IGameNavigator
    {
        private readonly ISceneService _scenes;
        private readonly IEventBus _eventBus;

        internal GameNavigator(ISceneService scenes, IEventBus eventBus)
        {
            _scenes = scenes;
            _eventBus = eventBus;
        }

        public bool IsNavigating => _scenes.IsLoading;

        public Awaitable LoadPersistentAsync(SceneKey scene)
        {
            return _scenes.LoadPersistentSceneAsync(NameOf(scene));
        }

        public async Awaitable<bool> GoToAsync(SceneKey scene)
        {
            string sceneName = NameOf(scene);
            if (_scenes.IsLoading)
            {
                return false;
            }

            _eventBus.Publish(new SceneSwitchStartingEvent(scene));
            await _scenes.SwitchSceneAsync(sceneName);
            return true;
        }

        private static string NameOf(SceneKey scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (string.IsNullOrEmpty(scene.SceneName))
            {
                throw new ArgumentException($"Scene key '{scene.name}' has no scene name.", nameof(scene));
            }

            return scene.SceneName;
        }
    }
}
