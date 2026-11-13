using Game.Contracts.Navigation;
using Paowa.Core.Foundation.Services;
using UnityEngine;

namespace Game.Systems.SceneFlow
{
    /// <summary>
    /// The Boot scene's only behaviour: loads the persistent UI scene, then switches to the first content scene. Core has
    /// finished bootstrapping before any scene loads, so services are ready here. Without a first scene, Boot stays the
    /// active content scene once the UI scene is loaded. Boot's own audio listener covers the frames before the UI scene
    /// (which owns the game's listener) is loaded and is switched off once it is, so there is always exactly one.
    /// REFERENCES: <c>_persistentUiScene</c> is the shared UI scene; <c>_firstScene</c> is where the game starts (optional);
    /// <c>_bootListener</c> is Boot's temporary audio listener (optional).
    /// </summary>
    internal sealed class BootSceneEntry : MonoBehaviour
    {
        [SerializeField] private SceneKey _persistentUiScene;
        [SerializeField] private SceneKey _firstScene;
        [SerializeField] private AudioListener _bootListener;

        private async Awaitable Start()
        {
            var navigator = CoreServiceLocator.Get<IGameNavigator>();
            await navigator.LoadPersistentAsync(_persistentUiScene);
            if (_bootListener != null)
            {
                _bootListener.enabled = false;
            }

            if (_firstScene != null)
            {
                await navigator.GoToAsync(_firstScene);
            }
        }
    }
}
